namespace Baguettefy.Core.Interfaces
{
    public interface IFirebaseDatabase
    {
        public event Action<KeyValuePair<string, object>> OnDataPut;

        Task Init(TimeSpan syncDatabaseTickTime);

        Task<T> GetAsync<T>(string path) where T : class;

        Task GetAllAsync<T>(string path, Action<string,T> onItemFound) where T : class;

        Task PutAsync<T>(string path, T data);

        Task DeleteAsync(string path);
    }
}
