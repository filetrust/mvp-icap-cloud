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
    public class InsertEntityIntoCacheTests
    {
        public class RunMethod : InsertEntityIntoCacheTests
        {
            [Test]
            public async Task Entity_Is_Inserted_When_Called()
            {
                // Arrange
                const string fileHash = "IAMHASH";
                const string fileStatus = "rebuilt";
                const string fileType = "pdf";
                const string partitionKey = "durablefileprocessing";

                var actual = new OutcomeEntity();

                var mockCacheManager = new Mock<ICacheManager<OutcomeEntity>>();
                var mockContext = new Mock<IDurableActivityContext>();
                var mockLogger = new Mock<ILogger>();

                mockContext.Setup(s => s.GetInput<(string, string, string)>()).Returns((fileHash, fileStatus, fileType));

                mockCacheManager.Setup(s => s.InsertEntityAsync(It.IsAny<OutcomeEntity>())).Callback<OutcomeEntity>((entity) =>
                {
                    actual = entity;
                });

                var activityFunction = new InsertEntityIntoCache(mockCacheManager.Object);

                // Act
                await activityFunction.Run(mockContext.Object, mockLogger.Object);

                // Assert
                Assert.That(actual.FileType, Is.EqualTo(fileType));
                Assert.That(actual.FileStatus, Is.EqualTo(fileStatus));
                Assert.That(actual.RowKey, Is.EqualTo(fileHash));
                Assert.That(actual.PartitionKey, Is.EqualTo(partitionKey));
            }
        }
    }
}