using DurableFileProcessing.Interfaces;
using DurableFileProcessing.Models;
using DurableFileProcessing.Orchestrators;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Text;
using System.Threading.Tasks;

namespace DurableFileProcessing.Tests
{
    public class FileProcessingOrchestratorTests
    {
        public class RunOrchestratorMethod : FileProcessingOrchestratorTests
        {
            private const string BlobName = "TestBlob";
            private const string BlobReadWriteSas = "BlobReadWriteSas";
            private const string SourceReadSas = "SourceReadSas";
            private const string RebuiltWriteSas = "RebuiltWriteSas";
            private const string RebuiltReadSas = "RebuiltReadSas";
            private const string FileHash = "FileHash";

            private CloudBlobContainer _cloudBlobContainer;

            private Mock<IDurableOrchestrationContext> _mockContext;
            private Mock<ILogger> _mockLogger;
            private Mock<IConfigurationSettings> _mockSettings;
            private Mock<IStorageAccount<CloudStorageAccount>> _mockAzureStorageAccount;
            private Mock<IBlobUtilities> _mockBlobUtilities;

            private Mock<CloudStorageAccount> _mockCloudStorageAccount;

            private FileProcessingOrchestrator _fileProcessingOrchestrator;

            [SetUp]
            public void Setup()
            {
                _cloudBlobContainer = new CloudBlobContainer(
                    new Uri("http://tempuri.org/blob"),
                    new StorageCredentials(
                    "fakeaccount",
                    Convert.ToBase64String(Encoding.Unicode.GetBytes("fakekeyval")),
                    "fakekeyname"));

                _mockContext = new Mock<IDurableOrchestrationContext>();
                _mockLogger = new Mock<ILogger>();
                _mockSettings = new Mock<IConfigurationSettings>();
                _mockAzureStorageAccount = new Mock<IStorageAccount<CloudStorageAccount>>();
                _mockBlobUtilities = new Mock<IBlobUtilities>();
                _mockCloudStorageAccount = new Mock<CloudStorageAccount>(new StorageCredentials("dummyAccountName", "dummykey"), false);

                _mockContext.Setup(s => s.GetInput<string>()).Returns(BlobName);
                _mockContext.SetupGet(s => s.InstanceId).Returns(FileHash);
                _mockContext.Setup(s => s.CallActivityAsync<string>(
                    It.Is<string>(s => s == "FileProcessing_HashGenerator"),
                    It.IsAny<object>()))
                    .ReturnsAsync(FileHash);

                _mockBlobUtilities.Setup(s => s.GetSharedAccessSignature(
                    It.IsAny<CloudBlobContainer>(),
                    It.Is<string>(s => s == FileHash),
                    It.IsAny<DateTimeOffset>(),
                    It.Is<SharedAccessBlobPermissions>(s => s.Equals(SharedAccessBlobPermissions.Read))))
                    .Returns(RebuiltReadSas);

                _mockBlobUtilities.Setup(s => s.GetSharedAccessSignature(
                    It.IsAny<CloudBlobContainer>(), 
                    It.Is<string>(s => s == BlobName), 
                    It.IsAny<DateTimeOffset>(), 
                    It.Is<SharedAccessBlobPermissions>(s => s.Equals(SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write))))
                    .Returns(BlobReadWriteSas);

                _mockBlobUtilities.Setup(s => s.GetSharedAccessSignature(
                    It.IsAny<CloudBlobContainer>(), 
                    It.Is<string>(s => s == FileHash), 
                    It.IsAny<DateTimeOffset>(),
                    It.Is<SharedAccessBlobPermissions>(s => s.Equals(SharedAccessBlobPermissions.Write))))
                    .Returns(RebuiltWriteSas);

                _mockBlobUtilities.Setup(s => s.GetSharedAccessSignature(
                    It.IsAny<CloudBlobContainer>(), 
                    It.Is<string>(s => s == BlobName), 
                    It.IsAny<DateTimeOffset>(),
                    It.Is<SharedAccessBlobPermissions>(s => s.Equals(SharedAccessBlobPermissions.Read))))
                    .Returns(SourceReadSas);

                _mockSettings.SetupGet(s => s.FileProcessingStorage).Returns("testStorage");
                _mockAzureStorageAccount.Setup(s => s.GetClient(It.IsAny<string>())).Returns(_mockCloudStorageAccount.Object);

                _fileProcessingOrchestrator = new FileProcessingOrchestrator(_mockAzureStorageAccount.Object, _mockBlobUtilities.Object, _mockSettings.Object);
            }

            [Test]
            public async Task When_Hash_Is_Found_In_Cache_GetFileType_IsNot_Called()
            {
                // Arrange
                _mockContext.Setup(s => s.CallActivityAsync<OutcomeEntity>(
                    It.Is<string>(s => s == "FileProcessing_GetEntityFromCache"),
                    It.IsAny<object>()))
                    .ReturnsAsync(new OutcomeEntity
                    {
                        FileType = "docx"
                    });

                // Act
                await _fileProcessingOrchestrator.RunOrchestrator(_mockContext.Object, _cloudBlobContainer, _mockLogger.Object);

                // Assert
                _mockContext.Verify(s => s.CallActivityAsync<string>(
                    It.Is<string>(s => s == "FileProcessing_GetFileType"),
                    It.IsAny<object>()), Times.Never);
            }

            [Test]
            public async Task When_Hash_IsNot_Found_In_Cache_GetFileType_Is_Called()
            {
                // Arrange
                _mockContext.Setup(s => s.CallActivityAsync<OutcomeEntity>(
                    It.Is<string>(s => s == "FileProcessing_GetEntityFromCache"),
                    It.IsAny<object>()))
                    .ReturnsAsync((OutcomeEntity)null);

                _mockContext.Setup(s => s.CallActivityAsync<string>(
                    It.Is<string>(s => s == "FileProcessing_GetFileType"),
                    It.IsAny<object>()))
                    .ReturnsAsync("Docx");

                // Act
                await _fileProcessingOrchestrator.RunOrchestrator(_mockContext.Object, _cloudBlobContainer, _mockLogger.Object);

                // Assert
                _mockContext.Verify(s => s.CallActivityAsync<string>(
                    It.Is<string>(s => s == "FileProcessing_GetFileType"),
                    It.IsAny<object>()), Times.Once);
            }

            [Test]
            public async Task When_Hash_Is_Found_In_Cache_RebuildFile_IsNot_Called_And_Cached_Status_Is_Used()
            {
                // Arrange
                ProcessingOutcome actualOutcome = ProcessingOutcome.Unmanaged;
                ProcessingOutcome expectedOutcome = ProcessingOutcome.Failed;

                _mockContext.Setup(s => s.CallActivityAsync<OutcomeEntity>(
                    It.Is<string>(s => s == "FileProcessing_GetEntityFromCache"),
                    It.IsAny<object>()))
                    .ReturnsAsync(new OutcomeEntity
                    {
                        FileType = "docx",
                        FileStatus = expectedOutcome.ToString()
                    });

                _mockContext.Setup(s => s.CallActivityAsync<object>(
                    It.Is<string>(s => s == "FileProcessing_SignalTransactionOutcome"),
                    It.IsAny<object>()))
                .Callback<string, object>((s, obj) =>
                {
                    var rebuiltOutcome = (RebuildOutcome)obj.GetType().GetField("Item2").GetValue(obj);

                    actualOutcome = rebuiltOutcome.Outcome;
                });

                // Act
                await _fileProcessingOrchestrator.RunOrchestrator(_mockContext.Object, _cloudBlobContainer, _mockLogger.Object);

                // Assert
                _mockContext.Verify(s => s.CallActivityAsync<object>(
                    It.Is<string>(s => s == "FileProcessing_RebuildFile"),
                    It.IsAny<object>()), Times.Never);

                Assert.That(actualOutcome, Is.EqualTo(expectedOutcome));
            }

            [Test]
            public async Task When_Hash_IsNot_Found_In_Cache_RebuildFile_Is_Called()
            {
                // Arrange
                _mockContext.Setup(s => s.CallActivityAsync<OutcomeEntity>(
                    It.Is<string>(s => s == "FileProcessing_GetEntityFromCache"),
                    It.IsAny<object>()))
                    .ReturnsAsync((OutcomeEntity)null);

                _mockContext.Setup(s => s.CallActivityAsync<string>(
                    It.Is<string>(s => s == "FileProcessing_GetFileType"),
                    It.IsAny<object>()))
                    .ReturnsAsync("Docx");

                // Act
                await _fileProcessingOrchestrator.RunOrchestrator(_mockContext.Object, _cloudBlobContainer, _mockLogger.Object);

                // Assert
                _mockContext.Verify(s => s.CallActivityAsync<object>(
                    It.Is<string>(s => s == "FileProcessing_RebuildFile"),
                    It.IsAny<object>()), Times.Once);            
            }

            [Test]
            public async Task When_Hash_IsNot_Found_In_Cache_InsertEntityIntoCache_Is_Called()
            {
                // Arrange
                const string expectedFileType = "docx";
                var expectedProcessingStatus = ProcessingOutcome.Rebuilt;

                string actualFileHash = string.Empty;
                string actualFileStatus = string.Empty;
                string actualFileType = string.Empty;

                _mockContext.Setup(s => s.CallActivityAsync<OutcomeEntity>(
                    It.Is<string>(s => s == "FileProcessing_GetEntityFromCache"),
                    It.IsAny<object>()))
                    .ReturnsAsync((OutcomeEntity)null);

                _mockContext.Setup(s => s.CallActivityAsync<string>(
                    It.Is<string>(s => s == "FileProcessing_GetFileType"),
                    It.IsAny<object>()))
                    .ReturnsAsync(expectedFileType);

                _mockContext.Setup(s => s.CallActivityAsync<ProcessingOutcome>(
                    It.Is<string>(s => s == "FileProcessing_RebuildFile"),
                    It.IsAny<object>()))
                    .ReturnsAsync(expectedProcessingStatus);

                _mockContext.Setup(s => s.CallActivityAsync<object>(
                    It.Is<string>(s => s == "FileProcessing_InsertEntityIntoCache"),
                    It.IsAny<object>()))
                .Callback<string, object>((s, obj) =>
                {
                    actualFileHash = (string)obj.GetType().GetField("Item1").GetValue(obj);
                    actualFileStatus = (string)obj.GetType().GetField("Item2").GetValue(obj);
                    actualFileType = (string)obj.GetType().GetField("Item3").GetValue(obj);
}               );

                // Act
                await _fileProcessingOrchestrator.RunOrchestrator(_mockContext.Object, _cloudBlobContainer, _mockLogger.Object);

                // Assert
                Assert.That(actualFileHash, Is.EqualTo(FileHash));
                Assert.That(actualFileStatus, Is.EqualTo(expectedProcessingStatus.ToString()));
                Assert.That(actualFileType, Is.EqualTo(expectedFileType));
            }

            [Test]
            public async Task When_Hash_Is_Found_In_Cache_InsertEntityIntoCache_IsNot_Called()
            {
                // Arrange
                var expectedProcessingStatus = ProcessingOutcome.Rebuilt;
                
                _mockContext.Setup(s => s.CallActivityAsync<OutcomeEntity>(
                    It.Is<string>(s => s == "FileProcessing_GetEntityFromCache"),
                    It.IsAny<object>()))
                    .ReturnsAsync(new OutcomeEntity
                    {
                        FileType = "docx"
                    });

                _mockContext.Setup(s => s.CallActivityAsync<ProcessingOutcome>(
                    It.Is<string>(s => s == "FileProcessing_RebuildFile"),
                    It.IsAny<object>()))
                    .ReturnsAsync(expectedProcessingStatus);

                // Act
                await _fileProcessingOrchestrator.RunOrchestrator(_mockContext.Object, _cloudBlobContainer, _mockLogger.Object);

                // Assert
                _mockContext.Verify(s => s.CallActivityAsync<object>(
                    It.Is<string>(s => s == "FileProcessing_InsertEntityIntoCache"),
                    It.IsAny<object>()), Times.Never);
            }

            [Test]
            public async Task When_GetFileType_Returns_Error_ErrorTransactionOutcome_Is_Signaled()
            {
                // Arrange
                RebuildOutcome actualOutcome = null;
                string actualBlobName = string.Empty;

                var expectedOutcome = new RebuildOutcome
                {
                    Outcome = ProcessingOutcome.Error,
                    RebuiltFileSas = string.Empty
                };

                _mockContext.Setup(s => s.CallActivityAsync<string>(
                    It.Is<string>(s => s == "FileProcessing_GetFileType"),
                    It.IsAny<object>()))
                    .ReturnsAsync("error");

                _mockContext.Setup(s => s.CallActivityAsync<object>(
                    It.Is<string>(s => s == "FileProcessing_SignalTransactionOutcome"),
                    It.IsAny<object>()))
                .Callback<string, object>((s, obj) =>
                {
                    actualBlobName = (string)obj.GetType().GetField("Item1").GetValue(obj);
                    actualOutcome = (RebuildOutcome)obj.GetType().GetField("Item2").GetValue(obj);
                });

                // Act
                await _fileProcessingOrchestrator.RunOrchestrator(_mockContext.Object, _cloudBlobContainer, _mockLogger.Object);

                // Assert
                Assert.That(actualBlobName, Is.EqualTo(BlobName));
                Assert.That(actualOutcome.Outcome, Is.EqualTo(expectedOutcome.Outcome));
                Assert.That(actualOutcome.RebuiltFileSas, Is.EqualTo(expectedOutcome.RebuiltFileSas));
            }

            [Test]
            public async Task When_GetFileType_Returns_Unmanaged_UnmanagedTransactionOutcome_Is_Signaled()
            {
                // Arrange
                RebuildOutcome actualOutcome = null;
                string actualBlobName = string.Empty;

                var expectedOutcome = new RebuildOutcome
                {
                    Outcome = ProcessingOutcome.Unmanaged,
                    RebuiltFileSas = string.Empty
                };

                _mockContext.Setup(s => s.CallActivityAsync<string>(
                    It.Is<string>(s => s == "FileProcessing_GetFileType"),
                    It.IsAny<object>()))
                    .ReturnsAsync("unmanaged");

                _mockContext.Setup(s => s.CallActivityAsync<object>(
                    It.Is<string>(s => s == "FileProcessing_SignalTransactionOutcome"),
                    It.IsAny<object>()))
                .Callback<string, object>((s, obj) =>
                {
                    actualBlobName = (string)obj.GetType().GetField("Item1").GetValue(obj);
                    actualOutcome = (RebuildOutcome)obj.GetType().GetField("Item2").GetValue(obj);
                });

                // Act
                await _fileProcessingOrchestrator.RunOrchestrator(_mockContext.Object, _cloudBlobContainer, _mockLogger.Object);

                // Assert
                Assert.That(actualBlobName, Is.EqualTo(BlobName));
                Assert.That(actualOutcome.Outcome, Is.EqualTo(expectedOutcome.Outcome));
                Assert.That(actualOutcome.RebuiltFileSas, Is.EqualTo(expectedOutcome.RebuiltFileSas));
            }

            [Test]
            public async Task When_GetFileType_Returns_Unknown_UnmanagedTransactionOutcome_Is_Signaled()
            {
                // Arrange
                RebuildOutcome actualOutcome = null;
                string actualBlobName = string.Empty;

                var expectedOutcome = new RebuildOutcome
                {
                    Outcome = ProcessingOutcome.Unmanaged,
                    RebuiltFileSas = string.Empty
                };

                _mockContext.Setup(s => s.CallActivityAsync<string>(
                    It.Is<string>(s => s == "FileProcessing_GetFileType"),
                    It.IsAny<object>()))
                    .ReturnsAsync("unknown");

                _mockContext.Setup(s => s.CallActivityAsync<object>(
                    It.Is<string>(s => s == "FileProcessing_SignalTransactionOutcome"),
                    It.IsAny<object>()))
                .Callback<string, object>((s, obj) =>
                {
                    actualBlobName = (string)obj.GetType().GetField("Item1").GetValue(obj);
                    actualOutcome = (RebuildOutcome)obj.GetType().GetField("Item2").GetValue(obj);
                });

                // Act
                await _fileProcessingOrchestrator.RunOrchestrator(_mockContext.Object, _cloudBlobContainer, _mockLogger.Object);

                // Assert
                Assert.That(actualBlobName, Is.EqualTo(BlobName));
                Assert.That(actualOutcome.Outcome, Is.EqualTo(expectedOutcome.Outcome));
                Assert.That(actualOutcome.RebuiltFileSas, Is.EqualTo(expectedOutcome.RebuiltFileSas));
            }


            [Test]
            public async Task When_RebuildOutcome_Is_Rebuilt_RebuiltOutcome_Is_Signaled()
            {
                // Arrange
                RebuildOutcome actualOutcome = null;
                string actualBlobName = string.Empty;

                var expectedOutcome = new RebuildOutcome
                {
                    Outcome = ProcessingOutcome.Rebuilt,
                    RebuiltFileSas = RebuiltReadSas
                };

                _mockContext.Setup(s => s.CallActivityAsync<string>(
                    It.Is<string>(s => s == "FileProcessing_GetFileType"),
                    It.IsAny<object>()))
                    .ReturnsAsync("success");

                _mockContext.Setup(s => s.CallActivityAsync<ProcessingOutcome>(
                    It.Is<string>(s => s == "FileProcessing_RebuildFile"),
                    It.IsAny<object>()))
                    .ReturnsAsync(ProcessingOutcome.Rebuilt);

                _mockContext.Setup(s => s.CallActivityAsync<object>(
                    It.Is<string>(s => s == "FileProcessing_SignalTransactionOutcome"),
                    It.IsAny<object>()))
                .Callback<string, object>((s, obj) =>
                {
                    actualBlobName = (string)obj.GetType().GetField("Item1").GetValue(obj);
                    actualOutcome = (RebuildOutcome)obj.GetType().GetField("Item2").GetValue(obj);
                });

                // Act
                await _fileProcessingOrchestrator.RunOrchestrator(_mockContext.Object, _cloudBlobContainer, _mockLogger.Object);

                // Assert
                Assert.That(actualBlobName, Is.EqualTo(BlobName));
                Assert.That(actualOutcome.Outcome, Is.EqualTo(expectedOutcome.Outcome));
                Assert.That(actualOutcome.RebuiltFileSas, Is.EqualTo(expectedOutcome.RebuiltFileSas));
            }

            [TestCase(ProcessingOutcome.Error)]
            [TestCase(ProcessingOutcome.Failed)]
            [TestCase(ProcessingOutcome.Unknown)]
            [TestCase(ProcessingOutcome.Unmanaged)]
            public async Task When_RebuildOutcome_IsNot_Rebuilt_FailedOutcome_Is_Signaled(ProcessingOutcome returnedOutcome)
            {
                // Arrange
                RebuildOutcome actualOutcome = null;
                string actualBlobName = string.Empty;

                var expectedOutcome = new RebuildOutcome
                {
                    Outcome = returnedOutcome,
                    RebuiltFileSas = string.Empty
                };

                _mockContext.Setup(s => s.CallActivityAsync<string>(
                    It.Is<string>(s => s == "FileProcessing_GetFileType"),
                    It.IsAny<object>()))
                    .ReturnsAsync("success");

                _mockContext.Setup(s => s.CallActivityAsync<ProcessingOutcome>(
                    It.Is<string>(s => s == "FileProcessing_RebuildFile"),
                    It.IsAny<object>()))
                    .ReturnsAsync(returnedOutcome);

                _mockContext.Setup(s => s.CallActivityAsync<object>(
                    It.Is<string>(s => s == "FileProcessing_SignalTransactionOutcome"),
                    It.IsAny<object>()))
                .Callback<string, object>((s, obj) =>
                {
                    actualBlobName = (string)obj.GetType().GetField("Item1").GetValue(obj);
                    actualOutcome = (RebuildOutcome)obj.GetType().GetField("Item2").GetValue(obj);
                });

                // Act
                await _fileProcessingOrchestrator.RunOrchestrator(_mockContext.Object, _cloudBlobContainer, _mockLogger.Object);

                // Assert
                Assert.That(actualBlobName, Is.EqualTo(BlobName));
                Assert.That(actualOutcome.Outcome, Is.EqualTo(expectedOutcome.Outcome));
                Assert.That(actualOutcome.RebuiltFileSas, Is.EqualTo(expectedOutcome.RebuiltFileSas));
            }
        }
    }
}