using Flurl;
using Flurl.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using Dynamitey.DynamicObjects;
using System.Threading.Tasks;
using DurableFileProcessing.Services;
using DurableFileProcessing.Models;

namespace DurableFileProcessing.ActivityFunctions
{
    public class RebuildFile
    {
        [FunctionName("FileProcessing_RebuildFile")]
        public static async Task<ProcessingOutcome> Run([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            (IConfigurationSettings configuration, string receivedSas, string rebuildSas, string receivedFiletype) = context.GetInput<(IConfigurationSettings, string, string, string)>();
            log.LogInformation($"RebuildFileAsync, receivedSas='{receivedSas}', rebuildSas='{rebuildSas}', receivedFiletype='{receivedFiletype}'");
            var rebuildUrl = configuration.RebuildUrl;
            var rebuildKey = configuration.RebuildKey;
            try
            {
                var response = await rebuildUrl
                                    .SetQueryParam("code", rebuildKey, isEncoded: true)
                                    .PostJsonAsync(new
                                    {
                                        InputGetUrl = receivedSas,
                                        OutputPutUrl = rebuildSas,
                                        OutputPutUrlRequestHeaders = new Dictionary
                                        {
                                                 { "x-ms-blob-type", "BlockBlob" }
                                        }
                                    })
                                    .ReceiveString();
                log.LogInformation($"GetFileType, response='{response}'");

                return ProcessingOutcome.Rebuilt;
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Unable to Rebuild file.");
                return ProcessingOutcome.Error;
            }
        }
    }
}