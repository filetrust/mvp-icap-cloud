using Microsoft.Azure.Storage;
using DurableFileProcessing.Interfaces;

namespace DurableFileProcessing.Services
{
    public class AzureStorageAccount : IStorageAccount<CloudStorageAccount>
    {
        public CloudStorageAccount GetClient(string connectionString)
        {
            return CloudStorageAccount.Parse(connectionString);
        }
    }
}
