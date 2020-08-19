using Flurl.Http.Testing;
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
    public class FileProcessingTests
    {
        public class RunOrchestratorTests : FileProcessingTests
        {
            private const string blobName = "TestBlob";

            private CloudBlobContainer _cloudBlobContainer;

            [SetUp]
            public void Setup()
            {
                _cloudBlobContainer = new CloudBlobContainer(
                    new Uri("http://tempuri.org/blob"),
                    new StorageCredentials(
                    "fakeaccoutn",
                    Convert.ToBase64String(Encoding.Unicode.GetBytes("fakekeyval")),
                    "fakekeyname"));
            }

            [Test]
            public async Task When_FileProcessing_Returns_Error_ErrorTransactionOutcome_Is_Signaled()
            {
                // Arrange
                ConfigurationSettings actualSettings = null;
                RebuildOutcome actualOutcome = null;
                string actualBlobName = string.Empty;

                var expectedSettings = new ConfigurationSettings
                {
                    FileProcessingStorage = "testStorage",
                };

                var expectedOutcome = new RebuildOutcome
                {
                    Outcome = ProcessingOutcome.Error,
                    RebuiltFileSas = string.Empty
                };

                var durableOrchestrationContext = new Mock<IDurableOrchestrationContext>();
                var logger = new Mock<ILogger>();

                durableOrchestrationContext.Setup(s => s.GetInput<string>()).Returns(blobName);
                durableOrchestrationContext.SetupGet(s => s.InstanceId).Returns(Guid.NewGuid().ToString());
                durableOrchestrationContext.Setup(s => s.CallActivityAsync<string>(
                    It.Is<string>(s => s == "FileProcessing_GetFileType"),
                    It.IsAny<object>()))
                    .ReturnsAsync("error");

                durableOrchestrationContext.Setup(s => s.CallActivityAsync<ConfigurationSettings>(
                    It.Is<string>(s => s == "FileProcessing_GetConfigurationSettings"),
                    It.IsAny<object>()))
                    .ReturnsAsync(expectedSettings);

                durableOrchestrationContext.Setup(s => s.CallActivityAsync<object>(
                    It.Is<string>(s => s == "FileProcessing_SignalTransactionOutcome"),
                    It.IsAny<object>()))
                .Callback<string, object>((s, obj) =>
                {
                    actualSettings = (ConfigurationSettings)obj.GetType().GetField("Item1").GetValue(obj);
                    actualBlobName = (string)obj.GetType().GetField("Item2").GetValue(obj);
                    actualOutcome = (RebuildOutcome)obj.GetType().GetField("Item3").GetValue(obj);
                });

                // Act
                await FileProcessing.RunOrchestrator(durableOrchestrationContext.Object, _cloudBlobContainer, logger.Object);

                // Assert
                Assert.That(actualSettings, Is.EqualTo(expectedSettings));
                Assert.That(actualBlobName, Is.EqualTo(blobName));
                Assert.That(actualOutcome.Outcome, Is.EqualTo(expectedOutcome.Outcome));
                Assert.That(actualOutcome.RebuiltFileSas, Is.EqualTo(expectedOutcome.RebuiltFileSas));
            }

            [Test]
            public async Task When_FileProcessing_Returns_Unmanaged_UnknownTransactionOutcome_Is_Signaled()
            {
                // Arrange
                ConfigurationSettings actualSettings = null;
                RebuildOutcome actualOutcome = null;
                string actualBlobName = string.Empty;

                var expectedSettings = new ConfigurationSettings
                {
                    FileProcessingStorage = "testStorage",
                };

                var expectedOutcome = new RebuildOutcome
                {
                    Outcome = ProcessingOutcome.Unknown,
                    RebuiltFileSas = string.Empty
                };

                var durableOrchestrationContext = new Mock<IDurableOrchestrationContext>();
                var logger = new Mock<ILogger>();

                durableOrchestrationContext.Setup(s => s.GetInput<string>()).Returns(blobName);
                durableOrchestrationContext.SetupGet(s => s.InstanceId).Returns(Guid.NewGuid().ToString());
                durableOrchestrationContext.Setup(s => s.CallActivityAsync<string>(
                    It.Is<string>(s => s == "FileProcessing_GetFileType"),
                    It.IsAny<object>()))
                    .ReturnsAsync("unmanaged");

                durableOrchestrationContext.Setup(s => s.CallActivityAsync<ConfigurationSettings>(
                    It.Is<string>(s => s == "FileProcessing_GetConfigurationSettings"),
                    It.IsAny<object>()))
                    .ReturnsAsync(expectedSettings);

                durableOrchestrationContext.Setup(s => s.CallActivityAsync<object>(
                    It.Is<string>(s => s == "FileProcessing_SignalTransactionOutcome"),
                    It.IsAny<object>()))
                .Callback<string, object>((s, obj) =>
                {
                    actualSettings = (ConfigurationSettings)obj.GetType().GetField("Item1").GetValue(obj);
                    actualBlobName = (string)obj.GetType().GetField("Item2").GetValue(obj);
                    actualOutcome = (RebuildOutcome)obj.GetType().GetField("Item3").GetValue(obj);
                });

                // Act
                await FileProcessing.RunOrchestrator(durableOrchestrationContext.Object, _cloudBlobContainer, logger.Object);

                // Assert
                Assert.That(actualSettings, Is.EqualTo(expectedSettings));
                Assert.That(actualBlobName, Is.EqualTo(blobName));
                Assert.That(actualOutcome.Outcome, Is.EqualTo(expectedOutcome.Outcome));
                Assert.That(actualOutcome.RebuiltFileSas, Is.EqualTo(expectedOutcome.RebuiltFileSas));
            }
        }

        public class GetConfigurationSettingsTests : FileProcessingTests
        {
            [Test]
            public async Task GetConfigurationSettings_Returns_The_Correct_Settings()
            {
                // Arrange
                var expectedSettings = new ConfigurationSettings
                {
                    FileProcessingStorage = "testStorage",
                    FiletypeDetectionKey = "testFileTypeDetectionKey",
                    FiletypeDetectionUrl = "testFileTypeDestectionUrl",
                    RebuildKey = "testRebuildKey",
                    RebuildUrl = "testRebuildUrl",
                    ServiceBusConnectionString = "TestServiceBusConnectionString",
                    TransactionOutcomeQueueName = "testQueueName"
                };

                var durableActivityContext = new Mock<IDurableActivityContext>();

                Environment.SetEnvironmentVariable("FileProcessingStorage", expectedSettings.FileProcessingStorage);
                Environment.SetEnvironmentVariable("TransactionOutcomeQueueName", expectedSettings.TransactionOutcomeQueueName);
                Environment.SetEnvironmentVariable("FiletypeDetectionUrl", expectedSettings.FiletypeDetectionUrl);
                Environment.SetEnvironmentVariable("ServiceBusConnectionString", expectedSettings.ServiceBusConnectionString);
                Environment.SetEnvironmentVariable("FiletypeDetectionKey", expectedSettings.FiletypeDetectionKey);
                Environment.SetEnvironmentVariable("RebuildUrl", expectedSettings.RebuildUrl);
                Environment.SetEnvironmentVariable("RebuildKey", expectedSettings.RebuildKey);

                // Act
                var actualSettings = await FileProcessing.GetConfigurationSettings(durableActivityContext.Object);

                // Assert
                Assert.That(actualSettings.FileProcessingStorage, Is.EqualTo(expectedSettings.FileProcessingStorage));
                Assert.That(actualSettings.FiletypeDetectionKey, Is.EqualTo(expectedSettings.FiletypeDetectionKey));
                Assert.That(actualSettings.FiletypeDetectionUrl, Is.EqualTo(expectedSettings.FiletypeDetectionUrl));
                Assert.That(actualSettings.RebuildKey, Is.EqualTo(expectedSettings.RebuildKey));
                Assert.That(actualSettings.RebuildUrl, Is.EqualTo(expectedSettings.RebuildUrl));
                Assert.That(actualSettings.ServiceBusConnectionString, Is.EqualTo(expectedSettings.ServiceBusConnectionString));
                Assert.That(actualSettings.TransactionOutcomeQueueName, Is.EqualTo(expectedSettings.TransactionOutcomeQueueName));
            }
        }

        public class CheckAvailableOutcomeTests : FileProcessingTests
        {
            [Test]
            public void UnknownOutcome_Is_Returned_When_Calling_CheckAvailableOutcome()
            {
                // Arrange
                const string hash = "I AM A HASH";

                var durableActivityContext = new Mock<IDurableActivityContext>();
                var logger = new Mock<ILogger>();

                durableActivityContext.Setup(s => s.GetInput<string>()).Returns(hash);

                // Act
                var outcome = FileProcessing.CheckAvailableOutcome(durableActivityContext.Object, logger.Object);

                // Assert
                Assert.That(outcome, Is.EqualTo(ProcessingOutcome.Unknown));
            }
        }

        public class GetFileTypeAsyncTests : FileProcessingTests
        {
            private HttpTest _httpTest;

            [SetUp]
            public void SetUp()
            {
                _httpTest = new HttpTest();
            }

            [TearDown]
            public void Dispose()
            {
                _httpTest.Dispose();
            }

            [Test]
            public async Task FileTypeName_Is_Returned_When_Successfully_Detecting_FileType()
            {
                // Arrange
                const string blobSas = "http://im-a-blob-sas";
                const string expectedFileType = "doc";

                var settings = new ConfigurationSettings
                {
                    FiletypeDetectionKey = "TestKey",
                    FiletypeDetectionUrl = "http://testrebuildurl"
                };

                var returnedInput = (settings, blobSas);

                var durableActivityContext = new Mock<IDurableActivityContext>();
                var logger = new Mock<ILogger>();

                durableActivityContext.Setup(s => s.GetInput<(ConfigurationSettings, string)>())
                    .Returns(returnedInput);

                _httpTest.RespondWithJson(new { FileTypeName = expectedFileType });

                // Act

                var outcome = await FileProcessing.GetFileTypeAsync(durableActivityContext.Object, logger.Object);

                // Assert
                Assert.That(outcome, Is.EqualTo(expectedFileType));
            }

            [Test]
            public async Task Error_Is_Returned_When_Issues_With_DetectingFileType()
            {
                // Arrange
                const string blobSas = "http://im-a-blob-sas";

                var settings = new ConfigurationSettings
                {
                    RebuildKey = "TestKey",
                    RebuildUrl = "http://testrebuildurl"
                };

                var returnedInput = (settings, blobSas);

                var durableActivityContext = new Mock<IDurableActivityContext>();
                var logger = new Mock<ILogger>();

                durableActivityContext.Setup(s => s.GetInput<(ConfigurationSettings, string)>())
                    .Returns(returnedInput);

                _httpTest.RespondWith(string.Empty, 500);

                // Act

                var outcome = await FileProcessing.GetFileTypeAsync(durableActivityContext.Object, logger.Object);

                // Assert
                Assert.That(outcome, Is.EqualTo("Error"));
            }
        }

        public class RebuildFileAsyncTests : FileProcessingTests
        {
            private HttpTest _httpTest;

            [SetUp]
            public void SetUp()
            {
                _httpTest = new HttpTest();
            }

            [TearDown]
            public void Dispose()
            {
                _httpTest.Dispose();
            }

            [Test]
            public async Task RebuiltOutcome_Is_Returned_When_Successfully_Rebuilt()
            {
                // Arrange
                const string receivedSas = "http://im-a-received-sas";
                const string rebuildSas = "http://im-a-rebuild-sas";
                const string receivedFileType = "doc";

                var settings = new ConfigurationSettings
                {
                    RebuildKey = "TestKey",
                    RebuildUrl = "http://testrebuildurl"
                };

                var returnedInput = (settings, receivedSas, rebuildSas, receivedFileType);

                var durableActivityContext = new Mock<IDurableActivityContext>();
                var logger = new Mock<ILogger>();

                durableActivityContext.Setup(s => s.GetInput<(ConfigurationSettings, string, string, string)>())
                    .Returns(returnedInput);

                _httpTest.RespondWith(string.Empty, 200);

                // Act

                var outcome = await FileProcessing.RebuildFileAsync(durableActivityContext.Object, logger.Object);

                // Assert
                Assert.That(outcome, Is.EqualTo(ProcessingOutcome.Rebuilt));
            }

            [Test]
            public async Task ErrorOutcome_Is_Returned_When_Issues_With_Rebuilding()
            {
                // Arrange
                const string receivedSas = "http://im-a-received-sas";
                const string rebuildSas = "http://im-a-rebuild-sas";
                const string receivedFileType = "doc";

                var settings = new ConfigurationSettings
                {
                    RebuildKey = "TestKey",
                    RebuildUrl = "http://testrebuildurl"
                };

                var returnedInput = (settings, receivedSas, rebuildSas, receivedFileType);

                var durableActivityContext = new Mock<IDurableActivityContext>();
                var logger = new Mock<ILogger>();

                durableActivityContext.Setup(s => s.GetInput<(ConfigurationSettings, string, string, string)>())
                    .Returns(returnedInput);

                _httpTest.RespondWith(string.Empty, 500);

                // Act

                var outcome = await FileProcessing.RebuildFileAsync(durableActivityContext.Object, logger.Object);

                // Assert
                Assert.That(outcome, Is.EqualTo(ProcessingOutcome.Error));
            } 
        }

        public class BlobTriggerTests : FileProcessingTests
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
                await FileProcessing.BlobTrigger(cloudblockBlob.Object, expectedInputName, durableOrchestrationClient.Object, logger.Object);

                // Assert
                durableOrchestrationClient.Verify(s => s.StartNewAsync(
                    It.Is<string>(s => s == "FileProcessing"),
                    It.Is<string>(s => s == string.Empty),
                    It.Is<string>(s => s == expectedInputName)));
            }
        }
    }
}