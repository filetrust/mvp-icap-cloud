using DurableFileProcessing.Interfaces;
using DurableFileProcessing.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace DurableFileProcessing.ActivityFunctions
{
    public class GetEntityFromCache
    {
        private readonly ICacheManager<OutcomeEntity> _cacheManager;

        public GetEntityFromCache(ICacheManager<OutcomeEntity> cacheManager)
        {
            _cacheManager = cacheManager;
        }

        [FunctionName("FileProcessing_GetEntityFromCache")]
        public async Task<OutcomeEntity> Run([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            string fileHash = context.GetInput<string>();

            var entity = await _cacheManager.GetEntityAsync("durablefileprocessing", fileHash);

            if(entity == null)
            {
                log.LogDebug($"No entity for hash: {fileHash} found in cache.");
            }

            log.LogDebug($"Entity for hash: {fileHash} found in cache. File Status: {entity.FileStatus}, File Type: {entity.FileType}, TimeStamp: {entity.Timestamp}");
            return entity;
        }
    }
}