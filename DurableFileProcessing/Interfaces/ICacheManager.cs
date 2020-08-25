using System.Threading.Tasks;

namespace DurableFileProcessing.Interfaces
{
    public interface ICacheManager<T>
    {
        Task<T> GetEntityAsync(string partitionKey, string rowKey);

        Task InsertEntityAsync(T entity);
    }
}
