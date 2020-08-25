using DurableFileProcessing.Interfaces;
using Microsoft.Azure.Cosmos.Table;
using System;

namespace DurableFileProcessing.Services
{
    class TableStorageCacheClient : ICacheClient<CloudTableClient>
    {
        private Lazy<CloudTableClient> _cloudTableClient;
        
        public TableStorageCacheClient(IConfigurationSettings configurationSettings)
        {
            _cloudTableClient = new Lazy<CloudTableClient>(() =>
            {
                var cloudStorageAccount = CloudStorageAccount.Parse(configurationSettings.CacheConnectionString);
                return cloudStorageAccount.CreateCloudTableClient();
            });
        }

        public CloudTableClient Client => _cloudTableClient.Value;
    }
}
