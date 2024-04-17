using Baguettefy.Core.Interfaces;
using Firebase.Database;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Baguettefy.Cache
{
    /// <summary>
    /// A Firebase Realtime Database, but it stores all the json objects locally in a file structure.
    /// If an entry doesnt exist, it will grab it from Firebase.
    /// If it does exist, and the last time this entry was accessed was X minutes ago, then it will just use the local version.
    /// </summary>
    public class CachedFirebaseDatabase : IFirebaseDatabase
    {
        private static readonly SemaphoreSlim _SemaphoreSlim = new SemaphoreSlim(1, 1);

        public string BASE_URL = "";
        public string SERVICE_ACCOUNT_JSON = "";

        private FirebaseClient _Client;

        private string _baseDirectory = "CachedDatabase";

        public Dictionary<string, Type> CachedCollections { get; set; } = new();

        private class OfflineObject
        {
            public DateTime LastAccessed;
            public string Key;
            public string Json;
        }

        public event Action<KeyValuePair<string, object>> OnDataPut;

        //https://github.com/step-up-labs/firebase-database-dotnet/issues/221
        //Auth setup using this suggestion.

        private async Task<string> GetAccessToken()
        {
            var credential = GoogleCredential.FromJson(SERVICE_ACCOUNT_JSON)
                .CreateScoped(
                    "https://www.googleapis.com/auth/userinfo.email",
                    "https://www.googleapis.com/auth/firebase.database"
                );
            ITokenAccess c = credential as ITokenAccess;
            return await c.GetAccessTokenForRequestAsync();
        }

        private FirebaseClient CreateClient()
        {
            return new FirebaseClient(BASE_URL,
                new FirebaseOptions
                {
                    AuthTokenAsyncFactory = GetAccessToken,
                    AsAccessToken = true
                });
        }

        public async Task Init(string databaseUrl, string serviceAccount, string localCacheName = "CachedDatabase")
        {
            _baseDirectory = localCacheName;

            BASE_URL = databaseUrl;
            SERVICE_ACCOUNT_JSON = serviceAccount;

            _Client = CreateClient();

            await _SemaphoreSlim.WaitAsync();
            try
            {
                Directory.CreateDirectory($"{_baseDirectory}{Path.DirectorySeparatorChar}");

                //Cache all the known collections, this will clear the offline database if any existed.
                foreach (var cachedCollection in CachedCollections)
                {
                    //Assume that if the folder already exists, its previously been cached, use that.
                    var cachedDirectory = $"{_baseDirectory}{Path.DirectorySeparatorChar}{cachedCollection.Key}";
                    var exists = Directory.Exists(cachedDirectory);
                    if (exists)
                    {
                        Console.WriteLine($"{cachedDirectory} already exists, skipping cache load and using existing.");
                        continue;
                    }
                    Console.WriteLine($"{cachedDirectory} doesn't exist, re-caching..");

                    var json = await _Client.Child(cachedCollection.Key)?.OnceAsJsonAsync();
                    if (json == null)
                    {
                        continue;
                    }

                    Dictionary<string, object>? dataDic;
                    try
                    {
                        JArray obj = JArray.Parse(json);
                        dataDic = new Dictionary<string, object>();
                        foreach (var token in obj)
                        {
                            if (!token.HasValues) continue;

                            var key = token.Path.TrimStart("[".ToCharArray()).TrimEnd("]".ToCharArray());
                            var value = token.ToString();
                            var data = JsonConvert.DeserializeObject(value);

                            if (data == null) continue;
                            dataDic.Add(key, data);
                        }
                    }
                    catch (Exception e)
                    {
                        dataDic = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    }


                    if (dataDic == null) continue;
                    foreach (var dataPair in dataDic)
                    {
                        var path = $"{cachedCollection.Key}{Path.DirectorySeparatorChar}{dataPair.Key}";
                        var data = dataPair.Value;

                        var dataJson = JsonConvert.SerializeObject(data);

                        FileInfo file = new FileInfo($"{_baseDirectory}{Path.DirectorySeparatorChar}{path}.json");
                        var filePath = file.FullName;
                        var directoryName = Path.GetDirectoryName(filePath);
                        Directory.CreateDirectory(directoryName);

                        await File.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(new OfflineObject
                        {
                            Json = dataJson,
                            Key = path,
                            LastAccessed = DateTime.Now
                        }));
                    }
                }
            }
            finally
            {
                _SemaphoreSlim.Release();
            }
        }

        public async Task<T> GetAsync<T>(string path) where T : class
        {
            await _SemaphoreSlim.WaitAsync();
            try
            {
                string? json = null;
                var offlinePath = $"{_baseDirectory}{Path.DirectorySeparatorChar}{path}.json";

                //If the offline file exists, use it
                if (File.Exists(offlinePath))
                {
                    json = await File.ReadAllTextAsync(offlinePath);
                }

                //If it doesnt exist offline, then grab it from online.
                if (string.IsNullOrEmpty(json))
                {
                    json = await _Client.Child(path).OnceAsJsonAsync();
                    var obj = JsonConvert.DeserializeObject<T>(json);
                    if (obj == null) return default;

                    await File.WriteAllTextAsync(offlinePath, json);
                }
                else
                {
                    var offlineEntry = JsonConvert.DeserializeObject<OfflineObject>(json);
                    json = offlineEntry?.Json;
                }

                return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
            }
            catch (Exception e)
            {
                return default;
            }
            finally
            {
                _SemaphoreSlim.Release();
            }
        }

        public async Task GetAllAsync<T>(string path, Func<string, T, bool> onItemFound) where T : class
        {
            await _SemaphoreSlim.WaitAsync();
            try
            {
                var baseDirectoryPath = $"{_baseDirectory}{Path.DirectorySeparatorChar}{path}";

                Directory.CreateDirectory(baseDirectoryPath);

                foreach (var filePath in Directory.EnumerateFileSystemEntries(baseDirectoryPath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    var offlineObj = JsonConvert.DeserializeObject<OfflineObject>(json);
                    if (offlineObj?.Json == null) continue;
                    try
                    {
                        if (onItemFound?.Invoke(offlineObj.Key, JsonConvert.DeserializeObject<T>(offlineObj.Json)) ?? false)
                        {
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to parse json element in GetAllAsync");
                    }
                }
            }
            finally
            {
                _SemaphoreSlim.Release();
            }
        }

        public async Task PutAsync<T>(string path, T data)
        {
            await _SemaphoreSlim.WaitAsync();
            try
            {
                await Internal_PutAsync(path, data);
            }
            finally
            {
                _SemaphoreSlim.Release();
            }
        }

        private async Task Internal_PutAsync<T>(string path, T data)
        {
            var dataJson = JsonConvert.SerializeObject(data);

            JObject? jsonObj = JsonConvert.DeserializeObject<JObject>(dataJson);
            if (jsonObj != null)
            {
                FileInfo file = new FileInfo($"{_baseDirectory}{Path.DirectorySeparatorChar}{path}.json");
                var filePath = file.FullName;
                var directoryName = Path.GetDirectoryName(filePath);
                Directory.CreateDirectory(directoryName);

                await File.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(new OfflineObject
                {
                    Json = dataJson,
                    Key = path,
                    LastAccessed = DateTime.Now
                }));
            }

            await _Client.Child(path).PutAsync(dataJson);

            OnDataPut?.Invoke(new KeyValuePair<string, object>(path, data));
        }

        public async Task DeleteAsync(string path)
        {
            await _SemaphoreSlim.WaitAsync();
            try
            {
                await _Client.Child(path).DeleteAsync();
                File.Delete($"{_baseDirectory}{Path.DirectorySeparatorChar}{path}.json");

                RecursiveDelete(new DirectoryInfo($"{_baseDirectory}{Path.DirectorySeparatorChar}{path}"));
            }
            finally
            {
                _SemaphoreSlim.Release();
            }
        }

        private static void RecursiveDelete(DirectoryInfo baseDir)
        {
            if (!baseDir.Exists)
                return;

            foreach (var dir in baseDir.EnumerateDirectories())
            {
                RecursiveDelete(dir);
            }
            var files = baseDir.GetFiles();
            foreach (var file in files)
            {
                file.IsReadOnly = false;
                file.Delete();
            }
            baseDir.Delete();
        }
    }
}
