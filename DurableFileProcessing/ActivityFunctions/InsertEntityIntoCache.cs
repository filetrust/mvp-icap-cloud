using DurableFileProcessing.Interfaces;
using DurableFileProcessing.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace DurableFileProcessing.ActivityFunctions
{
    public class InsertEntityIntoCache
    {
        private readonly ICacheManager<OutcomeEntity> _cacheManager;

        public InsertEntityIntoCache(ICacheManager<OutcomeEntity> cacheManager)
        {
            _cacheManager = cacheManager;
        }

        [FunctionName("FileProcessing_InsertEntityIntoCache")]
        public async Task Run([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            (string fileHash, string fileStatus, string fileType) = context.GetInput<(string, string, string)>();

            var entry = new OutcomeEntity
            {
                PartitionKey = "durablefileprocessing",
                RowKey = fileHash,
                FileStatus = fileStatus,
                FileType = fileType
            };

            await _cacheManager.InsertEntityAsync(entry);
        }
    }
}