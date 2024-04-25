namespace Baguettefy.Core.Interfaces
{
    public interface IFirebaseDatabase
    {
        public Dictionary<string, Dictionary<string, object>> CachedCollections { get; set; }

        public event Action<KeyValuePair<string, object>> OnDataPut;

        Task Init(string databaseUrl, string serviceAccount, string localCacheName = "CachedDatabase");

        Task<T> GetAsync<T>(string path) where T : class;

        Task GetAllAsync<T>(string path, Func<string, T, bool> onItemFound) where T : class;

        Task PutAsync<T>(string path, T data);

        Task DeleteAsync(string path);
    }
}