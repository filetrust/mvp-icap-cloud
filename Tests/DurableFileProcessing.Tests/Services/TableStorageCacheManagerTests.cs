using DurableFileProcessing.Interfaces;
using DurableFileProcessing.Models;
using DurableFileProcessing.Services;
using Microsoft.Azure.Cosmos.Table;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace DurableFileProcessing.Tests.Services
{
    public class TableStorageCacheManagerTests
    {
        public class GetEntityAsyncMethod : TableStorageCacheManagerTests
        {
            private const string TableName = "TableName";

            private Mock<ICacheClient<CloudTableClient>> _mockCacheClient;
            private Mock<IConfigurationSettings> _mockConfigurationSettings;

            private Mock<CloudTableClient> _mockCloudTableClient;
            private Mock<CloudTable> _mockCloudTable;

            [SetUp]
            public void SetUp()
            {
                _mockCacheClient = new Mock<ICacheClient<CloudTableClient>>();
                _mockConfigurationSettings = new Mock<IConfigurationSettings>();
                _mockCloudTableClient = new Mock<CloudTableClient>(new Uri("http://localhost"), new StorageCredentials(accountName: "blah", keyValue: "blah"), (TableClientConfiguration)null);

                _mockCloudTable = new Mock<CloudTable>(new Uri("http://tableaddress.org"), (TableClientConfiguration)null);

                _mockConfigurationSettings.SetupGet(s => s.TransactionOutcomeTableName).Returns(TableName);

                _mockCacheClient.SetupGet(s => s.Client).Returns(_mockCloudTableClient.Object);

                _mockCloudTableClient.Setup(s => s.GetTableReference(It.Is<string>(s => s == TableName))).Returns(_mockCloudTable.Object);
            }

            [Test]
            public async Task Null_Is_Returned_When_Entity_IsNot_Found()
            {
                // Arrange
                _mockCloudTable.Setup(s => s.ExecuteAsync(It.IsAny<TableOperation>())).ReturnsAsync((TableResult)null);

                var cacheManager = new TableStorageCacheManager(_mockCacheClient.Object, _mockConfigurationSettings.Object);

                // Act
                var result = await cacheManager.GetEntityAsync("partitionKey", "rowKey");

                // Assert
                Assert.That(result, Is.Null);
            }

            [Test]
            public async Task Result_Is_Returned_When_Entity_Is_Found()
            {
                // Arrange
                var expected = new OutcomeEntity
                {
                    FileType = "docx",
                    FileStatus = "rebuilt",
                    RowKey = "1234567"
                };

                _mockCloudTable.Setup(s => s.ExecuteAsync(It.IsAny<TableOperation>())).ReturnsAsync(new TableResult { Result = expected });

                var cacheManager = new TableStorageCacheManager(_mockCacheClient.Object, _mockConfigurationSettings.Object);

                // Act
                var result = await cacheManager.GetEntityAsync("partitionKey", "rowKey");

                // Assert
                Assert.That(result.FileType, Is.EqualTo(expected.FileType));
                Assert.That(result.FileStatus, Is.EqualTo(expected.FileStatus));
                Assert.That(result.RowKey, Is.EqualTo(expected.RowKey));
            }
        }

        public class InsertEntityAsyncMethod : TableStorageCacheManagerTests
        {
            private const string TableName = "TableName";

            private Mock<ICacheClient<CloudTableClient>> _mockCacheClient;
            private Mock<IConfigurationSettings> _mockConfigurationSettings;

            private Mock<CloudTableClient> _mockCloudTableClient;
            private Mock<CloudTable> _mockCloudTable;

            [SetUp]
            public void SetUp()
            {
                _mockCacheClient = new Mock<ICacheClient<CloudTableClient>>();
                _mockConfigurationSettings = new Mock<IConfigurationSettings>();
                _mockCloudTableClient = new Mock<CloudTableClient>(new Uri("http://localhost"), new StorageCredentials(accountName: "blah", keyValue: "blah"), (TableClientConfiguration)null);

                _mockCloudTable = new Mock<CloudTable>(new Uri("http://tableaddress.org"), (TableClientConfiguration)null);

                _mockConfigurationSettings.SetupGet(s => s.TransactionOutcomeTableName).Returns(TableName);

                _mockCacheClient.SetupGet(s => s.Client).Returns(_mockCloudTableClient.Object);

                _mockCloudTableClient.Setup(s => s.GetTableReference(It.Is<string>(s => s == TableName))).Returns(_mockCloudTable.Object);
            }

            [Test]
            public async Task Correct_Entity_Is_Inserted_When_Called()
            {
                // Arrange
                var expected = new OutcomeEntity
                {
                    FileType = "docx",
                    FileStatus = "rebuilt",
                    RowKey = "1234567",
                    PartitionKey = "durablefileprocessing"
                };

                OutcomeEntity actualEntity = null;

                _mockCloudTable.Setup(s => s.ExecuteAsync(It.IsAny<TableOperation>())).Callback<TableOperation>((op) =>
                {
                    actualEntity = (OutcomeEntity)op.Entity;
                });

                var cacheManager = new TableStorageCacheManager(_mockCacheClient.Object, _mockConfigurationSettings.Object);

                // Act
                await cacheManager.InsertEntityAsync(expected);

                // Assert
                Assert.That(actualEntity.FileType, Is.EqualTo(expected.FileType));
                Assert.That(actualEntity.FileStatus, Is.EqualTo(expected.FileStatus));
                Assert.That(actualEntity.RowKey, Is.EqualTo(expected.RowKey));
                Assert.That(actualEntity.PartitionKey, Is.EqualTo(expected.PartitionKey));
            }
        }
    }
}
