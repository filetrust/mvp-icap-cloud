using Microsoft.Azure.Storage;

namespace DurableFileProcessing.Services
{
    public interface IStorageAccount
    {
        public CloudStorageAccount GetClient(string connectionString);
    }
}
