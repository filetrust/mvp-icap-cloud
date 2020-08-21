using DurableFileProcessing.Orchestrators;
using DurableFileProcessing.TriggerFunctions;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace DurableFileProcessing.Tests.TriggerFunctions
{
    public class BlobTriggerTests 
    {
        public class BlobTriggerStartMethod : BlobTriggerTests
        {
            [Test]
            public async Task BlobTrigger_Starts_With_Correct_Input()
            {
                // Arrange
                const string expectedInputName = "TestInput";

                var durableOrchestrationClient = new Mock<IDurableOrchestrationClient>();
                var cloudblockBlob = new Mock<CloudBlockBlob>(new Uri("http://tempuri.org/blob"));
                var logger = new Mock<ILogger>();

                // Act
                await BlobTrigger.BlobTriggerStart(cloudblockBlob.Object, expectedInputName, durableOrchestrationClient.Object, logger.Object);

                // Assert
                durableOrchestrationClient.Verify(s => s.StartNewAsync(
                    It.Is<string>(s => s == nameof(FileProcessingOrchestrator)),
                    It.Is<string>(s => s == string.Empty),
                    It.Is<string>(s => s == expectedInputName)));
            }
        }
    }
}
