using DurableFileProcessing.Interfaces;
using DurableFileProcessing.Models;
using Flurl;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DurableFileProcessing.Orchestrators
{
    [StorageAccount("FileProcessingStorage")]
    public class FileProcessingOrchestrator
    {
        private readonly IAzureStorageAccount _storageAccount;
        private readonly IBlobUtilities _blobUtilities;
        private readonly IConfigurationSettings _configurationSettings;

        public FileProcessingOrchestrator(IAzureStorageAccount storageAccount, IBlobUtilities blobUtilities, IConfigurationSettings configurationSettings)
        {
            _storageAccount = storageAccount;
            _blobUtilities = blobUtilities;
            _configurationSettings = configurationSettings;
        }

        [FunctionName(nameof(FileProcessingOrchestrator))]
        public async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            [Blob("original-store")] CloudBlobContainer container,
            ILogger log)
        {
            var blobName = context.GetInput<string>();

            string blobSas = _blobUtilities.GetSharedAccessSignature(container, blobName, context.CurrentUtcDateTime.AddHours(24), SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write);
            
            log.LogInformation($"FileProcessing SAS Token: {blobSas}");

            var fileId = context.InstanceId.ToString();

            var filetype = await context.CallActivityAsync<string>("FileProcessing_GetFileType", (blobSas));

            if (filetype == "error")
            {
                await context.CallActivityAsync("FileProcessing_SignalTransactionOutcome", (blobName, new RebuildOutcome { Outcome = ProcessingOutcome.Error, RebuiltFileSas = String.Empty }));
            }
            else if (filetype == "unmanaged")
            {
                await context.CallActivityAsync("FileProcessing_SignalTransactionOutcome", (blobName, new RebuildOutcome { Outcome = ProcessingOutcome.Unknown, RebuiltFileSas = String.Empty }));
            }
            else
            {
                log.LogInformation($"FileProcessing {filetype}");
                var fileProcessingStorage = _storageAccount.GetClient(_configurationSettings.FileProcessingStorage);
                
                var rebuildUrl = Url.Combine(fileProcessingStorage.BlobEndpoint.AbsoluteUri, "rebuild-store");
                log.LogInformation($"FileProcessing using  {rebuildUrl}");

                var rebuildContainer = new CloudBlobContainer(new Uri(rebuildUrl), fileProcessingStorage.Credentials);
                var sourceSas = _blobUtilities.GetSharedAccessSignature(container, blobName, context.CurrentUtcDateTime.AddHours(24), SharedAccessBlobPermissions.Read);

                // Specify the hash value as the rebuilt filename
                var rebuiltWritesSas = _blobUtilities.GetSharedAccessSignature(rebuildContainer, fileId, context.CurrentUtcDateTime.AddHours(24), SharedAccessBlobPermissions.Write);
                var rebuildOutcome = await context.CallActivityAsync<ProcessingOutcome>("FileProcessing_RebuildFile", (sourceSas, rebuiltWritesSas, filetype));

                if (rebuildOutcome == ProcessingOutcome.Rebuilt)
                {
                    var rebuiltReadSas = _blobUtilities.GetSharedAccessSignature(rebuildContainer, fileId, context.CurrentUtcDateTime.AddHours(24), SharedAccessBlobPermissions.Read);
                    log.LogInformation($"FileProcessing Rebuild {rebuiltReadSas}");

                    await context.CallActivityAsync("FileProcessing_SignalTransactionOutcome", (blobName, new RebuildOutcome { Outcome = ProcessingOutcome.Rebuilt, RebuiltFileSas = rebuiltReadSas }));
                }
                else
                {
                    log.LogInformation($"FileProcessing Rebuild failure");
                    await context.CallActivityAsync("FileProcessing_SignalTransactionOutcome", (blobName, new RebuildOutcome { Outcome = ProcessingOutcome.Failed, RebuiltFileSas = String.Empty }));
                }
            }
        }
    }
}