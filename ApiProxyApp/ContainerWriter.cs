using Azure.Storage.Blobs;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ApiProxyApp
{
    class ContainerWriter
    {
        private readonly string containerName;
        BlobServiceClient blobServiceClient;

        public ContainerWriter(string connectionString, string containerName)
        {
            blobServiceClient = new BlobServiceClient(connectionString);
            this.containerName = containerName ?? throw new ArgumentNullException(nameof(containerName));
        }

        public async Task Write(string inputFilePath)
        {
            try
            {
                // Create a BlobServiceClient object which will be used to create a container client
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                BlobClient blobClient = containerClient.GetBlobClient(Path.GetFileName(inputFilePath));

                using (FileStream uploadFileStream = File.OpenRead(inputFilePath))
                {
                    await blobClient.UploadAsync(uploadFileStream, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ContainerWriter : {ex.Message}");
            }
        }
    }
}
