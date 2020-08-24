using DurableFileProcessing.Interfaces;
using Microsoft.Azure.Storage;

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
