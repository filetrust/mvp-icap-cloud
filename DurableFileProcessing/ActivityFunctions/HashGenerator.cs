﻿using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DurableFileProcessing.ActivityFunctions
{
    public class HashGenerator
    {
        [FunctionName("FileProcessing_HashGenerator")]
        public static async Task<string> Run([ActivityTrigger] string blobSas, ILogger log)
        {
            log.LogInformation($"HashGenerator {blobSas}");
            var rxBlockBlob = new CloudBlockBlob(new Uri(blobSas));

            using (var fileStream = new MemoryStream())
            using (var md5 = MD5.Create())
            {
                await rxBlockBlob.DownloadToStreamAsync(fileStream);

                fileStream.Position = 0;

                var hash = md5.ComputeHash(fileStream);

                var stringBuilder = new StringBuilder();

                foreach (byte b in hash)
                    stringBuilder.AppendFormat("{0:X2}", b);

                return stringBuilder.ToString();
            }
        }
    }
}