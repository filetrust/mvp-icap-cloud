using Microsoft.Azure.Storage;

namespace DurableFileProcessing.Interfaces
{
    public interface IAzureStorageAccount
    {
        public CloudStorageAccount GetClient(string connectionString);
    }
}
