using DurableFileProcessing.Interfaces;
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
        private readonly IConfigurationSettings _configurationSettings;

        public GetFileType(IConfigurationSettings configurationSettings)
        {
            _configurationSettings = configurationSettings;
        }

        [FunctionName("FileProcessing_GetFileType")]
        public async Task<string> Run([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            string blobSas = context.GetInput<string>();
            var filetypeDetectionUrl = _configurationSettings.FiletypeDetectionUrl;
            var filetypeDetectionKey = _configurationSettings.FiletypeDetectionKey;

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