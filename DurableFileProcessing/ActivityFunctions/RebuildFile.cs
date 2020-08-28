using Flurl;
using Flurl.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using Dynamitey.DynamicObjects;
using System.Threading.Tasks;
using DurableFileProcessing.Models;
using DurableFileProcessing.Interfaces;
using DurableFileProcessing.Services;
using System.Net;

namespace DurableFileProcessing.ActivityFunctions
{
    public class RebuildFile
    {
        private readonly IConfigurationSettings _configurationSettings;

        public RebuildFile(IConfigurationSettings configurationSettings)
        {
            _configurationSettings = configurationSettings;
        }

        [FunctionName("FileProcessing_RebuildFile")]
        public async Task<ProcessingOutcome> Run([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            (string receivedSas, string rebuildSas, string receivedFiletype) = context.GetInput<(string, string, string)>();

            log.LogInformation($"RebuildFileAsync, receivedSas='{receivedSas}', rebuildSas='{rebuildSas}', receivedFiletype='{receivedFiletype}'");

            var rebuildUrl = _configurationSettings.RebuildUrl;
            var rebuildKey = _configurationSettings.RebuildKey;

            try
            {
                var response = await PollyPolicies.ApiRetryPolicy
                    .ExecuteAsync(async () =>
                    {
                        return await rebuildUrl
                        .SetQueryParam("code", rebuildKey, isEncoded: true)
                        .AllowHttpStatus(HttpStatusCode.UnprocessableEntity)
                        .PostJsonAsync(new
                        {
                            InputGetUrl = receivedSas,
                            OutputPutUrl = rebuildSas,
                            OutputPutUrlRequestHeaders = new Dictionary
                            {
                                    { "x-ms-blob-type", "BlockBlob" }
                            }
                        });
                    });

                if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
                {
                    log.LogInformation($"Unable to rebuild file.");
                    return ProcessingOutcome.Failed;
                }

                log.LogInformation($"Successfully Rebuilt File.");
                return ProcessingOutcome.Rebuilt;
            }
            catch (FlurlHttpException fhe)
            {
                log.LogError($"Error when trying to Rebuild file. Status: {fhe.Call.Response.StatusCode}");
                return ProcessingOutcome.Error;
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error when trying to Rebuild file.");
                return ProcessingOutcome.Error;
            }
        }
    }
}