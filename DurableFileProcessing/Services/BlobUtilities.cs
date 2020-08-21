using System;
using DurableFileProcessing.Interfaces;
using Microsoft.Azure.Storage.Blob;

namespace DurableFileProcessing.Services
{
    public class BlobUtilities : IBlobUtilities
    {
        public string GetSharedAccessSignature(CloudBlobContainer container, DateTimeOffset expiryTime, SharedAccessBlobPermissions accessPermissions)
        {
            SharedAccessBlobPolicy adHocPolicy = new SharedAccessBlobPolicy()
            {
                // When the start time for the SAS is omitted, the start time is assumed to be the time when the storage service receives the request.
                // Omitting the start time for a SAS that is effective immediately helps to avoid clock skew.
                SharedAccessExpiryTime = expiryTime,
                Permissions = accessPermissions 
            };

            var sasContainerToken = container.GetSharedAccessSignature(adHocPolicy);

            return container.Uri + sasContainerToken;
        }

        public string GetSharedAccessSignature(CloudBlobContainer container, string blobName, DateTimeOffset expiryTime, SharedAccessBlobPermissions accessPermissions)
        {
            SharedAccessBlobPolicy adHocPolicy = new SharedAccessBlobPolicy()
            {
                // When the start time for the SAS is omitted, the start time is assumed to be the time when the storage service receives the request.
                // Omitting the start time for a SAS that is effective immediately helps to avoid clock skew.
                SharedAccessExpiryTime = expiryTime,
                Permissions = accessPermissions
            };
            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);
            var sasBlobToken = blob.GetSharedAccessSignature(adHocPolicy);

            return blob.Uri + sasBlobToken;
        }
    }
}
