using Microsoft.Azure.Storage;
using DurableFileProcessing.Interfaces;

namespace DurableFileProcessing.Services
{
    public class AzureStorageAccount : IAzureStorageAccount
    {
        public CloudStorageAccount GetClient(string connectionString)
        {
            return CloudStorageAccount.Parse(connectionString);
        }
    }
}
