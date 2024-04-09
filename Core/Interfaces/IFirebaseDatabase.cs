namespace Baguettefy.Core.Interfaces
{
    public interface IFirebaseDatabase
    {
        public Dictionary<string, Type> CachedCollections { get; set; }

        public event Action<KeyValuePair<string, object>> OnDataPut;

        Task Init(string databaseUrl, string serviceAccount, TimeSpan syncDatabaseTickTime, bool cacheLocal = true, string localCacheName = "CachedDatabase/");

        Task<T> GetAsync<T>(string path) where T : class;

        Task GetAllAsync<T>(string path, Func<string,T, bool> onItemFound) where T : class;

        Task PutAsync<T>(string path, T data);

        Task DeleteAsync(string path);
    }
}
