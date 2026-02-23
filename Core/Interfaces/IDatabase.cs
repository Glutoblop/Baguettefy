namespace Baguettefy.Core.Interfaces
{
    public interface IDatabase
    {
        public Dictionary<string, Dictionary<string, object>> CachedCollections { get; set; }

        public event Action<KeyValuePair<string, object>> OnDataPut;

        Task Init(string localCacheName);

        Task<T> GetAsync<T>(string path) where T : class;

        Task GetAllAsync<T>(string path, Func<string, T, bool> onItemFound) where T : class;

        Task PutAsync<T>(string path, T data);

        Task DeleteAsync(string path);
    }
}