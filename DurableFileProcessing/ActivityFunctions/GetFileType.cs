using DurableFileProcessing.Services;
using Flurl.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DurableFileProcessing.ActivityFunctions
{
    public class GetFileType
    {
        [FunctionName("FileProcessing_GetFileType")]
        public static async Task<string> Run([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            (IConfigurationSettings configuration, string blobSas) = context.GetInput<(IConfigurationSettings, string)>();
            var filetypeDetectionUrl = configuration.FiletypeDetectionUrl;
            var filetypeDetectionKey = configuration.FiletypeDetectionKey;

            log.LogInformation($"GetFileType, filetypeDetectionUrl='{filetypeDetectionUrl}'");
            log.LogInformation($"GetFileType, blobSas='{blobSas}'");
            try
            {
                var response = await filetypeDetectionUrl
                                        .WithHeader("x-api-key", filetypeDetectionKey)
                                        .PostJsonAsync(new
                                        {
                                            SasUrl = blobSas
                                        })
                                        .ReceiveJson();

                return response.FileTypeName;
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Unable to detect filetype.");
                return "Error";
            }
        }
    }
}