using DurableFileProcessing.Interfaces;
using DurableFileProcessing.Models;
using Flurl;
using Microsoft.Azure.Storage;
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
        private readonly IStorageAccount<CloudStorageAccount> _storageAccount;
        private readonly IBlobUtilities _blobUtilities;
        private readonly IConfigurationSettings _configurationSettings;

        public FileProcessingOrchestrator(IStorageAccount<CloudStorageAccount> storageAccount, IBlobUtilities blobUtilities, IConfigurationSettings configurationSettings)
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
            ProcessingOutcome fileStatus;

            var blobName = context.GetInput<string>();

            string blobSas = _blobUtilities.GetSharedAccessSignature(container, blobName, context.CurrentUtcDateTime.AddHours(24), SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write);
            
            log.LogInformation($"FileProcessing SAS Token: {blobSas}");

            var hash = await context.CallActivityAsync<string>("FileProcessing_HashGenerator", blobSas);

            var cachedEntry = await context.CallActivityAsync<OutcomeEntity>("FileProcessing_GetEntityFromCache", hash);

            var filetype = cachedEntry?.FileType ?? await context.CallActivityAsync<string>("FileProcessing_GetFileType", blobSas);
            
            if (filetype.ToLower() == "error" || filetype.ToLower() == "unmanaged" || filetype.ToLower() == "unknown")
            {
                fileStatus = filetype == "error" ? ProcessingOutcome.Error : ProcessingOutcome.Unmanaged;
                await context.CallActivityAsync("FileProcessing_SignalTransactionOutcome", (blobName, new RebuildOutcome { Outcome = fileStatus, RebuiltFileSas = String.Empty }));
            }
            else
            {
                log.LogInformation($"FileProcessing {filetype}");
                var fileProcessingStorage = _storageAccount.GetClient(_configurationSettings.FileProcessingStorage);
                
                var rebuildUrl = Url.Combine(fileProcessingStorage.BlobEndpoint.AbsoluteUri, "rebuild-store");
                log.LogInformation($"FileProcessing using  {rebuildUrl}");

                var rebuildContainer = new CloudBlobContainer(new Uri(rebuildUrl), fileProcessingStorage.Credentials);

                if (!Enum.TryParse(cachedEntry?.FileStatus, out ProcessingOutcome rebuildOutcome))
                {
                    // Specify the hash value as the rebuilt filename
                    var rebuiltWritesSas = _blobUtilities.GetSharedAccessSignature(rebuildContainer, hash, context.CurrentUtcDateTime.AddHours(24), SharedAccessBlobPermissions.Write);
                    var sourceSas = _blobUtilities.GetSharedAccessSignature(container, blobName, context.CurrentUtcDateTime.AddHours(24), SharedAccessBlobPermissions.Read);

                    rebuildOutcome = await context.CallActivityAsync<ProcessingOutcome>("FileProcessing_RebuildFile", (sourceSas, rebuiltWritesSas, filetype));
                }

                fileStatus = rebuildOutcome;

                if (rebuildOutcome == ProcessingOutcome.Rebuilt)
                {
                    var rebuiltReadSas = _blobUtilities.GetSharedAccessSignature(rebuildContainer, hash, context.CurrentUtcDateTime.AddHours(24), SharedAccessBlobPermissions.Read);
                    log.LogInformation($"FileProcessing Rebuild {rebuiltReadSas}");

                    await context.CallActivityAsync("FileProcessing_SignalTransactionOutcome", (blobName, new RebuildOutcome { Outcome = fileStatus, RebuiltFileSas = rebuiltReadSas }));
                }
                else
                {
                    log.LogInformation($"FileProcessing Rebuild failure");
                    await context.CallActivityAsync("FileProcessing_SignalTransactionOutcome", (blobName, new RebuildOutcome { Outcome = fileStatus, RebuiltFileSas = String.Empty }));
                }
            }

            if (cachedEntry == null)
            {
                await context.CallActivityAsync("FileProcessing_InsertEntityIntoCache", (hash, fileStatus.ToString(), filetype));
            }
        }
    }
}