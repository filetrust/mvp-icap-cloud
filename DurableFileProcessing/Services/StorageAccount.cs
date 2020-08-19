using Microsoft.Azure.Storage;

namespace DurableFileProcessing.Services
{
    class StorageAccount : IStorageAccount
    {
        public CloudStorageAccount GetClient(string connectionString)
        {
            return CloudStorageAccount.Parse(connectionString);
        }
    }
}
