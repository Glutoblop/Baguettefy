using Baguettefy.Data;
using Baguettefy.Data.Quests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Baguettefy.Cache
{
    public class UpdateDatabase
    {
        private static readonly SemaphoreSlim _SemaphoreSlim = new SemaphoreSlim(1, 1);

        private string _BaseDirectory = "CachedData/";

        static HttpClient client = new HttpClient();

        public Dictionary<string, Type> CachedCollections { get; set; } = new();

        private class OfflineObject
        {
            public DateTime LastAccessed;
            public string Key;
            public string Json;
        }

        public event Action<KeyValuePair<string, object>> OnDataPut;

        public async Task Init(string localCacheName = "CachedDatabase/")
        {
            if (!localCacheName.EndsWith("/")) localCacheName += "/";
            _BaseDirectory = localCacheName;

            DateTime now = DateTime.UtcNow;

            var completeData = await GetAsync<CacheComplete>($"Complete");
            if (completeData?.IsComplete ?? false) return;

            //Categories contain types of quests, loop through these
            for (int category = 2; category < 100; category++)
            {
                var pageMissingCount = 0;
                Console.WriteLine($"#### Search Category {category}...");
                await Task.Delay(100);

                //Loop through the pages in this category
                for (int page = 0; page < 10; page++)
                {

                    var url = $"https://api.dofusdb.fr/quests?$";
                    var skipStartIndex = page * 50;
                    var allQuestsUrl = $"{url}skip={skipStartIndex}&$populate=true&$limit=50&categoryId={category}&lang=en";
                    //allQuestsUrl = $"https://api.dofusdb.fr/quests?$skip=0&$populate=true&$limit=1&categoryId=4&lang=en";

                    HttpResponseMessage response = await client.GetAsync(allQuestsUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        QuestsData? allQuests = JsonConvert.DeserializeObject<QuestsData>(json);

                        if (allQuests?.Quests?.Length > 0)
                        {
                            pageMissingCount = 0;

                            foreach (var quest in allQuests?.Quests)
                            {
                                var data = await GetAsync<Quest>($"Quest/{quest.Id}");
                                if (data == null)
                                {
                                    await PutAsync($"Quest/{quest.Id}", quest);

                                    Console.WriteLine($"{quest.Name.En} added to cached [{quest.Id}]");
                                }
                                else
                                {
                                    Console.WriteLine($"{quest.Name.En} already cached [{quest.Id}]");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"No data found for category:{category} page:{page}");
                            pageMissingCount++;
                            if (pageMissingCount > 2)
                            {
                                Console.WriteLine($"Assuming end of Category, skipping to next.");
                                break;
                            }
                        }
                    }

                    await Task.Delay(350);
                }
            }

            Console.WriteLine($"Completed in :{DateTime.UtcNow - now:g}");

            await PutAsync($"Complete", new CacheComplete() { IsComplete = true });

        }

        public async Task<T> GetAsync<T>(string path) where T : class
        {
            await _SemaphoreSlim.WaitAsync();
            try
            {
                CanProcessPath(path);

                string? json = null;
                var offlinePath = $"{_BaseDirectory}/{path}.json";

                //If the offline file exists, use it
                if (File.Exists(offlinePath))
                {
                    json = await File.ReadAllTextAsync(offlinePath);
                }

                //If it doesnt exist offline, then grab it from online.
                if (!string.IsNullOrEmpty(json))
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
                CanProcessPath(path);

                var baseDirectoryPath = $"{_BaseDirectory}{path}";
                Directory.CreateDirectory(baseDirectoryPath);


                foreach (var filePath in Directory.EnumerateFileSystemEntries(baseDirectoryPath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    var offlineObj = JsonConvert.DeserializeObject<OfflineObject>(json);
                    var finish = onItemFound?.Invoke(offlineObj.Key, JsonConvert.DeserializeObject<T>(offlineObj.Json)) ?? false;
                    if (finish)
                    {
                        break;
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
            CanProcessPath(path);

            var dataJson = JsonConvert.SerializeObject(data);

            JObject? jsonObj = JsonConvert.DeserializeObject<JObject>(dataJson);
            if (jsonObj != null)
            {
                FileInfo file = new FileInfo($"{_BaseDirectory}/{path}.json");
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

            OnDataPut?.Invoke(new KeyValuePair<string, object>(path, data));
        }

        private void CanProcessPath(string path)
        {
            //TODO - Support subobject access and combinding json objects.
            //TODO - With the current implementation if you were to access Accounts/1234/Event directly, it can be accessed and edited
            //TODO - But the object Account/1234 does not know the edit has happened, so will not be updated.
            //TODO - This could be fixed by either:
            //TODO -    Whenever you save an object, you climb up the tree and save the changes in the parent object before returning the value
            //TODO -    Whenever you get an object, you climb down the tree and grab all the subobject values and put them into the parent before you return

            if (path.Split("/").Length > 2 ||
                path.Split("\\").Length > 2)
            {
                // throw new NotSupportedException($"Error accessing: {path}. {nameof(UpdateDatabase)} only supports a flat Firebase structure, you cannot access sub-objects directly.");
            }
        }
    }
}
