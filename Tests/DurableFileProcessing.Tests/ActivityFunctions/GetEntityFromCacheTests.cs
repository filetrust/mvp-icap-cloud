using DurableFileProcessing.ActivityFunctions;
using DurableFileProcessing.Interfaces;
using DurableFileProcessing.Models;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace DurableFileProcessing.Tests.ActivityFunctions
{
    public class GetEntityFromCacheTests
    {
        public class RunMethod : GetEntityFromCacheTests
        {
            [Test]
            public async Task Entity_Is_Returned_When_Called()
            {
                // Arrange
                const string fileHash = "IAMHASH";
                const string partitionKey = "durablefileprocessing";

                var expected = new OutcomeEntity
                {
                    FileStatus = "rebuilt",
                    FileType = "Pdf",
                    RowKey = fileHash,
                    PartitionKey = "durablefileprocessing"
                };

                var mockCacheManager = new Mock<ICacheManager<OutcomeEntity>>();
                var mockContext = new Mock<IDurableActivityContext>();
                var mockLogger = new Mock<ILogger>();

                mockCacheManager.Setup(s => s.GetEntityAsync(
                    It.Is<string>(name => name == partitionKey),
                    It.Is<string>(hash => hash == fileHash)))
                    .ReturnsAsync(expected);

                mockContext.Setup(s => s.GetInput<string>()).Returns(fileHash);

                var activityFunction = new GetEntityFromCache(mockCacheManager.Object);

                // Act
                var result = await activityFunction.Run(mockContext.Object, mockLogger.Object);

                // Assert
                Assert.That(result.FileType, Is.EqualTo(expected.FileType));
                Assert.That(result.FileStatus, Is.EqualTo(expected.FileStatus));
                Assert.That(result.RowKey, Is.EqualTo(expected.RowKey));
                Assert.That(result.PartitionKey, Is.EqualTo(expected.PartitionKey));
            }
        }
    }
}