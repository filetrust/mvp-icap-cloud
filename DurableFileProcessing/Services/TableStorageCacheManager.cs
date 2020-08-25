using DurableFileProcessing.Interfaces;
using DurableFileProcessing.Models;
using Microsoft.Azure.Cosmos.Table;
using System.Threading.Tasks;

namespace DurableFileProcessing.Services
{
    public class TableStorageCacheManager : ICacheManager<OutcomeEntity>
    {
        private readonly ICacheClient<CloudTableClient> _cacheClient;
        private readonly IConfigurationSettings _configurationSettings;

        public TableStorageCacheManager(ICacheClient<CloudTableClient> cacheClient, IConfigurationSettings configurationSettings)
        {
            _cacheClient = cacheClient;
            _configurationSettings = configurationSettings;
        }

        public async Task<OutcomeEntity> GetEntityAsync(string partitionKey, string rowKey)
        {
            var table = _cacheClient.Client.GetTableReference(_configurationSettings.CacheConnectionString);

            var retrieveOperation = TableOperation.Retrieve<OutcomeEntity>(partitionKey, rowKey);

            var result = await table.ExecuteAsync(retrieveOperation);

            if (result == null)
            {
                return null;
            }

            return result.Result as OutcomeEntity;
        }

        public async Task InsertEntityAsync(OutcomeEntity entity)
        {
            var table = _cacheClient.Client.GetTableReference(_configurationSettings.CacheConnectionString);

            var insertOperation = TableOperation.InsertOrMerge(entity);

            await table.ExecuteAsync(insertOperation);
        }
    }
}
