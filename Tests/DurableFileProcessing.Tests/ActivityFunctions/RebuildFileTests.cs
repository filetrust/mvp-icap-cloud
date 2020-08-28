using DurableFileProcessing.ActivityFunctions;
using DurableFileProcessing.Interfaces;
using DurableFileProcessing.Models;
using Flurl.Http.Testing;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Threading.Tasks;

namespace DurableFileProcessing.Tests.ActivityFunctions
{
    public class RebuildFileTests
    {
        public class RunMethod : RebuildFileTests
        {
            private const string ReceivedSas = "http://im-a-received-sas";
            private const string RebuildSas = "http://im-a-rebuild-sas";
            private const string ReceivedFileType = "doc";
            private const string ApiUrl = "http://im-a-rebuild-sas";
            private const string ApiKey = "TestKey";

            private Mock<IDurableActivityContext> _mockContext;
            private Mock<ILogger> _mockLogger;
            private Mock<IConfigurationSettings> _mockSettings;
            private RebuildFile _classUnderTest;
            private HttpTest _httpTest;

            [SetUp]
            public void SetUp()
            {
                _mockContext = new Mock<IDurableActivityContext>();
                _mockSettings = new Mock<IConfigurationSettings>();
                _mockLogger = new Mock<ILogger>();

                _mockSettings.SetupGet(s => s.RebuildKey).Returns(ApiKey);
                _mockSettings.SetupGet(s => s.RebuildUrl).Returns(ApiUrl);

                _mockContext.Setup(s => s.GetInput<(string, string, string)>()).Returns((ReceivedSas, RebuildSas, ReceivedFileType));

                _httpTest = new HttpTest();

                _classUnderTest = new RebuildFile(_mockSettings.Object);
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
                _httpTest.RespondWith("Success", 200);

                // Act
                var outcome = await _classUnderTest.Run(_mockContext.Object, _mockLogger.Object);

                // Assert
                Assert.That(outcome, Is.EqualTo(ProcessingOutcome.Rebuilt));
                
                _httpTest.ShouldHaveCalled(ApiUrl)
                    .WithQueryParamValue("code", ApiKey)
                    .Times(1);
            }

            [Test]
            public async Task FailedOutcome_Is_Returned_When_File_Is_Not_Rebuildable()
            {
                // Arrange
                _httpTest.RespondWith("Unable to rebuild file", status: (int)HttpStatusCode.UnprocessableEntity);

                // Act
                var outcome = await _classUnderTest.Run(_mockContext.Object, _mockLogger.Object);

                // Assert
                Assert.That(outcome, Is.EqualTo(ProcessingOutcome.Failed));

                _httpTest.ShouldHaveCalled(ApiUrl)
                    .WithQueryParamValue("code", ApiKey)
                    .Times(1);
            }

            [TestCase(HttpStatusCode.InternalServerError)]
            [TestCase(HttpStatusCode.RequestTimeout)]
            [TestCase(HttpStatusCode.BadGateway)]
            [TestCase(HttpStatusCode.ServiceUnavailable)]
            [TestCase(HttpStatusCode.GatewayTimeout)]
            public async Task Call_Is_Retried_And_ErrorOutcome_Is_Returned_When_Issues_With_Rebuilding(HttpStatusCode returnedStatus)
            {
                // Arrange
                _httpTest.RespondWith(string.Empty, (int)returnedStatus);
                _httpTest.RespondWith(string.Empty, (int)returnedStatus);
                _httpTest.RespondWith(string.Empty, (int)returnedStatus);
                _httpTest.RespondWith(string.Empty, (int)returnedStatus);
                _httpTest.RespondWith(string.Empty, (int)returnedStatus);
                _httpTest.RespondWith(string.Empty, (int)returnedStatus);

                // Act
                var outcome = await _classUnderTest.Run(_mockContext.Object, _mockLogger.Object);

                // Assert
                Assert.That(outcome, Is.EqualTo(ProcessingOutcome.Error));

                _httpTest.ShouldHaveCalled(ApiUrl)
                    .WithQueryParamValue("code", ApiKey)
                    .Times(6);
            }

            [Test]
            public async Task RebuildCall_Is_Retried_On_Timeout()
            {
                // Arrange
                _httpTest.SimulateTimeout();
                _httpTest.RespondWith(string.Empty, 200);

                // Act
                var outcome = await _classUnderTest.Run(_mockContext.Object, _mockLogger.Object);

                // Assert
                Assert.That(outcome, Is.EqualTo(ProcessingOutcome.Rebuilt));

                _httpTest.ShouldHaveCalled(ApiUrl)
                    .WithQueryParamValue("code", ApiKey)
                    .Times(2);
            }
        }
    }
}
