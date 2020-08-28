using DurableFileProcessing.ActivityFunctions;
using DurableFileProcessing.Interfaces;
using Flurl.Http.Testing;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Threading.Tasks;

namespace DurableFileProcessing.Tests.ActivityFunctions
{
    public class GetFileTypeTests
    {
        public class RunMethod : GetFileTypeTests
        {
            private const string BlobSas = "http://im-a-blob-sas";
            private const string ApiUrl = "http://filetypedetectionurl";
            private const string ApiKey = "TestKey";

            private Mock<IDurableActivityContext> _mockContext;
            private Mock<IConfigurationSettings> _mockSettings;
            private Mock<ILogger> _mockLogger;
            private GetFileType _classUnderTest;
            private HttpTest _httpTest;

            [SetUp]
            public void SetUp()
            {
                _mockContext = new Mock<IDurableActivityContext>();
                _mockSettings = new Mock<IConfigurationSettings>();
                _mockLogger = new Mock<ILogger>();

                _mockSettings.SetupGet(s => s.FiletypeDetectionKey).Returns(ApiKey);
                _mockSettings.SetupGet(s => s.FiletypeDetectionUrl).Returns(ApiUrl);

                _mockContext.Setup(s => s.GetInput<string>()).Returns(BlobSas);

                _classUnderTest = new GetFileType(_mockSettings.Object);

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
                const string expectedFileType = "doc";

                _httpTest.RespondWithJson(new { FileTypeName = expectedFileType });

                // Act
                var outcome = await _classUnderTest.Run(_mockContext.Object, _mockLogger.Object);

                // Assert
                Assert.That(outcome, Is.EqualTo(expectedFileType));

                _httpTest.ShouldHaveCalled(ApiUrl)
                .WithHeader("x-api-key", ApiKey)
                .Times(1);
            }

            [TestCase(HttpStatusCode.InternalServerError)]
            [TestCase(HttpStatusCode.RequestTimeout)]
            [TestCase(HttpStatusCode.BadGateway)]
            [TestCase(HttpStatusCode.ServiceUnavailable)]
            [TestCase(HttpStatusCode.GatewayTimeout)]
            public async Task FileTypeDetectionCall_Is_Retried_And_Error_Is_Returned_When_Issues_With_DetectingFileType(HttpStatusCode returnedStatusCode)
            {
                // Arrange
                _httpTest.RespondWith(string.Empty, (int)returnedStatusCode);
                _httpTest.RespondWith(string.Empty, (int)returnedStatusCode);
                _httpTest.RespondWith(string.Empty, (int)returnedStatusCode);
                _httpTest.RespondWith(string.Empty, (int)returnedStatusCode);
                _httpTest.RespondWith(string.Empty, (int)returnedStatusCode);
                _httpTest.RespondWith(string.Empty, (int)returnedStatusCode);

                // Act
                var outcome = await _classUnderTest.Run(_mockContext.Object, _mockLogger.Object);

                // Assert
                Assert.That(outcome, Is.EqualTo("Error"));

                _httpTest.ShouldHaveCalled(ApiUrl)
                    .WithHeader("x-api-key", ApiKey)
                    .Times(6);
            }

            [Test]
            public async Task FileTypeDetectionCall_Is_Retried_On_Timeout()
            {
                // Arrange
                const string expectedFileType = "doc";
                
                _httpTest.SimulateTimeout();
                _httpTest.RespondWithJson(new { FileTypeName = expectedFileType });

                // Act
                var outcome = await _classUnderTest.Run(_mockContext.Object, _mockLogger.Object);

                // Assert
                Assert.That(outcome, Is.EqualTo(expectedFileType));

                _httpTest.ShouldHaveCalled(ApiUrl)
                    .WithHeader("x-api-key", ApiKey)
                    .Times(2);
            }
        }
    }
}
