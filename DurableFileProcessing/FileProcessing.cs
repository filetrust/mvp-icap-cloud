using Dynamitey.DynamicObjects;
using Flurl;
using Flurl.Http;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace DurableFileProcessing
{
    [StorageAccount("FileProcessingStorage")]
    public static class FileProcessing
    {
        [FunctionName("FileProcessing")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            [Blob("original-store")] CloudBlobContainer container,
            ILogger log)
        {
            var blobName = context.GetInput<string>();

            string blobSas = BlobUtilities.GetSharedAccessSignature(container, blobName, context.CurrentUtcDateTime.AddHours(24), SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write);
            var configurationSettings = await context.CallActivityAsync<ConfigurationSettings>("FileProcessing_GetConfigurationSettings", null);
            
            log.LogInformation($"FileProcessing SAS Token: {blobSas}");

            var fileId = context.InstanceId.ToString();

            var filetype = await context.CallActivityAsync<string>("FileProcessing_GetFileType", (configurationSettings, blobSas));

            if (filetype == "error")
            {
                await context.CallActivityAsync("FileProcessing_SignalTransactionOutcome", (configurationSettings, blobName, new RebuildOutcome { Outcome = ProcessingOutcome.Error, RebuiltFileSas = String.Empty }));
            }
            else if (filetype == "unmanaged")
            {
                await context.CallActivityAsync("FileProcessing_SignalTransactionOutcome", (configurationSettings, blobName, new RebuildOutcome { Outcome = ProcessingOutcome.Unknown, RebuiltFileSas = String.Empty }));
            }
            else
            {
                log.LogInformation($"FileProcessing {filetype}");
                var fileProcessingStorage = CloudStorageAccount.Parse(configurationSettings.FileProcessingStorage);
                var rebuildUrl = Url.Combine(fileProcessingStorage.BlobEndpoint.AbsoluteUri, "rebuild-store");
                log.LogInformation($"FileProcessing using  {rebuildUrl}");

                var rebuildContainer = new CloudBlobContainer(new Uri(rebuildUrl), fileProcessingStorage.Credentials);
                var sourceSas = BlobUtilities.GetSharedAccessSignature(container, blobName, context.CurrentUtcDateTime.AddHours(24), SharedAccessBlobPermissions.Read);

                // Specify the hash value as the rebuilt filename
                var rebuiltWritesSas = BlobUtilities.GetSharedAccessSignature(rebuildContainer, fileId, context.CurrentUtcDateTime.AddHours(24), SharedAccessBlobPermissions.Write);
                var rebuildOutcome = await context.CallActivityAsync<ProcessingOutcome>("FileProcessing_RebuildFile", (configurationSettings, sourceSas, rebuiltWritesSas, filetype));

                if (rebuildOutcome == ProcessingOutcome.Rebuilt)
                {
                    var rebuiltReadSas = BlobUtilities.GetSharedAccessSignature(rebuildContainer, fileId, context.CurrentUtcDateTime.AddHours(24), SharedAccessBlobPermissions.Read);
                    log.LogInformation($"FileProcessing Rebuild {rebuiltReadSas}");

                    await context.CallActivityAsync("FileProcessing_SignalTransactionOutcome", (configurationSettings, blobName, new RebuildOutcome { Outcome = ProcessingOutcome.Rebuilt, RebuiltFileSas = rebuiltReadSas }));
                }
                else
                {
                    log.LogInformation($"FileProcessing Rebuild failure");
                    await context.CallActivityAsync("FileProcessing_SignalTransactionOutcome", (configurationSettings, blobName, new RebuildOutcome { Outcome = ProcessingOutcome.Failed, RebuiltFileSas = String.Empty }));
                }
            }
        }

        [FunctionName("FileProcessing_GetConfigurationSettings")]
        public static Task<ConfigurationSettings> GetConfigurationSettings([ActivityTrigger] IDurableActivityContext context)
        {
            var configurationSettings = new ConfigurationSettings
            {
                FileProcessingStorage = Environment.GetEnvironmentVariable("FileProcessingStorage", EnvironmentVariableTarget.Process),
                TransactionOutcomeQueueName = Environment.GetEnvironmentVariable("TransactionOutcomeQueueName", EnvironmentVariableTarget.Process),
                FiletypeDetectionUrl = Environment.GetEnvironmentVariable("FiletypeDetectionUrl", EnvironmentVariableTarget.Process),
                ServiceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString", EnvironmentVariableTarget.Process),
                FiletypeDetectionKey = Environment.GetEnvironmentVariable("FiletypeDetectionKey", EnvironmentVariableTarget.Process),
                RebuildUrl = Environment.GetEnvironmentVariable("RebuildUrl", EnvironmentVariableTarget.Process),
                RebuildKey = Environment.GetEnvironmentVariable("RebuildKey", EnvironmentVariableTarget.Process),
            };

            return Task.FromResult(configurationSettings);
        }

        [FunctionName("FileProcessing_HashGenerator")]
        public static async Task<string> HashGeneratorAsync([ActivityTrigger] string blobSas, ILogger log)
        {
            log.LogInformation($"HashGenerator {blobSas}");
            var rxBlockBlob = new CloudBlockBlob(new Uri(blobSas));

            using (var fileStream = new MemoryStream())
            using (var md5 = MD5.Create())
            {
                await rxBlockBlob.DownloadToStreamAsync(fileStream);

                fileStream.Position = 0;

                return md5.ComputeHash(fileStream).ToString();
            }
        }

        [FunctionName("FileProcessing_StoreHash")]
        public static void StoreHash([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            (string transactionId, string hash) = context.GetInput<(string, string)>();
            log.LogInformation($"StoreHash, transactionId='{transactionId}', hash='{hash}'");
        }

        [FunctionName("FileProcessing_CheckAvailableOutcome")]
        public static ProcessingOutcome CheckAvailableOutcome([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            string hash = context.GetInput<string>();
            var outcome = ProcessingOutcome.Unknown;
            log.LogInformation($"CheckAvailableOutcome, hash='{hash}', Outcome = {outcome}");
            return outcome;
        }

        [FunctionName("FileProcessing_SignalTransactionOutcome")]
        public static async Task SignalTransactionOutcomeAsync([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            (ConfigurationSettings configuration, string fileId, RebuildOutcome outcome) = context.GetInput<(ConfigurationSettings, string, RebuildOutcome)>();
            log.LogInformation($"SignalTransactionOutcome, fileId='{fileId}', outcome='{outcome.Outcome}'");
            log.LogInformation($"SignalTransactionOutcome, ServiceBusConnectionString='{configuration.ServiceBusConnectionString}', TransactionOutcomeQueueName='{configuration.TransactionOutcomeQueueName}'");

            var queueClient = new QueueClient(configuration.ServiceBusConnectionString, configuration.TransactionOutcomeQueueName);
            var message = new Message
            {
                Label = "transaction-outcome"
            };
            message.UserProperties.Add("file-id", fileId);
            message.UserProperties.Add("file-outcome", Enum.GetName(typeof(ProcessingOutcome), outcome.Outcome));
            message.UserProperties.Add("file-rebuild-sas", outcome.RebuiltFileSas);
            await queueClient.SendAsync(message);

            await queueClient.CloseAsync();
        }
        
        [FunctionName("FileProcessing_GetFileType")]
        public static async Task<string> GetFileTypeAsync([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            (ConfigurationSettings configuration, string blobSas) = context.GetInput<(ConfigurationSettings, string)>();
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
        
        [FunctionName("FileProcessing_RebuildFile")]
        public static async Task<ProcessingOutcome> RebuildFileAsync([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            (ConfigurationSettings configuration, string receivedSas, string rebuildSas, string receivedFiletype) = context.GetInput<(ConfigurationSettings, string, string, string)>();
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

        [FunctionName("FileProcessing_BlobTrigger")]
        public static async Task BlobTrigger(
            [BlobTrigger("original-store/{name}")] CloudBlockBlob myBlob, string name,
            [DurableClient] IDurableOrchestrationClient starter, ILogger log)
        {
            string instanceId = await starter.StartNewAsync("FileProcessing", input:name);

            log.LogInformation($"Started orchestration with ID = '{instanceId}', Blob '{name}'.");
        }
    }
}