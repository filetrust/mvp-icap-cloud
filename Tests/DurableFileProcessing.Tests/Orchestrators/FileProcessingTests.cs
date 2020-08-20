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

            private string _instanceId = Guid.NewGuid().ToString();

            private CloudBlobContainer _cloudBlobContainer;

            private Mock<IDurableOrchestrationContext> _mockContext;
            private Mock<ILogger> _mockLogger;
            private Mock<IConfigurationSettings> _mockSettings;
            private Mock<IAzureStorageAccount> _mockAzureStorageAccount;
            private Mock<IBlobUtilities> _mockBlobUtilities;

            private Mock<CloudStorageAccount> _mockCloudStorageAccount;

            private FileProcessingOrchestrator _fileProcessingOrchestrator;

            [SetUp]
            public void Setup()
            {
                _cloudBlobContainer = new CloudBlobContainer(
                    new Uri("http://tempuri.org/blob"),
                    new StorageCredentials(
                    "fakeaccoutn",
                    Convert.ToBase64String(Encoding.Unicode.GetBytes("fakekeyval")),
                    "fakekeyname"));

                _mockContext = new Mock<IDurableOrchestrationContext>();
                _mockLogger = new Mock<ILogger>();
                _mockSettings = new Mock<IConfigurationSettings>();
                _mockAzureStorageAccount = new Mock<IAzureStorageAccount>();
                _mockBlobUtilities = new Mock<IBlobUtilities>();
                _mockCloudStorageAccount = new Mock<CloudStorageAccount>(new StorageCredentials("dummyAccountName", "dummykey"), false);

                _mockContext.Setup(s => s.GetInput<string>()).Returns(BlobName);
                _mockContext.SetupGet(s => s.InstanceId).Returns(_instanceId);

                _mockBlobUtilities.Setup(s => s.GetSharedAccessSignature(
                    It.IsAny<CloudBlobContainer>(),
                    It.Is<string>(s => s == _instanceId),
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
                    It.Is<string>(s => s == _instanceId), 
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
            public async Task When_FileProcessing_Returns_Error_ErrorTransactionOutcome_Is_Signaled()
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
            public async Task When_FileProcessing_Returns_Unmanaged_UnknownTransactionOutcome_Is_Signaled()
            {
                // Arrange
                RebuildOutcome actualOutcome = null;
                string actualBlobName = string.Empty;

                var expectedOutcome = new RebuildOutcome
                {
                    Outcome = ProcessingOutcome.Unknown,
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
                    Outcome = ProcessingOutcome.Failed,
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

        //public class GetConfigurationSettingsTests : FileProcessingTests
        //{
        //    [Test]
        //    public async Task GetConfigurationSettings_Returns_The_Correct_Settings()
        //    {
        //        // Arrange
        //        var expectedSettings = new ConfigurationSettings
        //        {
        //            FileProcessingStorage = "testStorage",
        //            FiletypeDetectionKey = "testFileTypeDetectionKey",
        //            FiletypeDetectionUrl = "testFileTypeDestectionUrl",
        //            RebuildKey = "testRebuildKey",
        //            RebuildUrl = "testRebuildUrl",
        //            ServiceBusConnectionString = "TestServiceBusConnectionString",
        //            TransactionOutcomeQueueName = "testQueueName"
        //        };

        //        var durableActivityContext = new Mock<IDurableActivityContext>();

        //        Environment.SetEnvironmentVariable("FileProcessingStorage", expectedSettings.FileProcessingStorage);
        //        Environment.SetEnvironmentVariable("TransactionOutcomeQueueName", expectedSettings.TransactionOutcomeQueueName);
        //        Environment.SetEnvironmentVariable("FiletypeDetectionUrl", expectedSettings.FiletypeDetectionUrl);
        //        Environment.SetEnvironmentVariable("ServiceBusConnectionString", expectedSettings.ServiceBusConnectionString);
        //        Environment.SetEnvironmentVariable("FiletypeDetectionKey", expectedSettings.FiletypeDetectionKey);
        //        Environment.SetEnvironmentVariable("RebuildUrl", expectedSettings.RebuildUrl);
        //        Environment.SetEnvironmentVariable("RebuildKey", expectedSettings.RebuildKey);

        //        // Act
        //        var actualSettings = await FileProcessing.GetConfigurationSettings(durableActivityContext.Object);

        //        // Assert
        //        Assert.That(actualSettings.FileProcessingStorage, Is.EqualTo(expectedSettings.FileProcessingStorage));
        //        Assert.That(actualSettings.FiletypeDetectionKey, Is.EqualTo(expectedSettings.FiletypeDetectionKey));
        //        Assert.That(actualSettings.FiletypeDetectionUrl, Is.EqualTo(expectedSettings.FiletypeDetectionUrl));
        //        Assert.That(actualSettings.RebuildKey, Is.EqualTo(expectedSettings.RebuildKey));
        //        Assert.That(actualSettings.RebuildUrl, Is.EqualTo(expectedSettings.RebuildUrl));
        //        Assert.That(actualSettings.ServiceBusConnectionString, Is.EqualTo(expectedSettings.ServiceBusConnectionString));
        //        Assert.That(actualSettings.TransactionOutcomeQueueName, Is.EqualTo(expectedSettings.TransactionOutcomeQueueName));
        //    }
        //}
    }
}