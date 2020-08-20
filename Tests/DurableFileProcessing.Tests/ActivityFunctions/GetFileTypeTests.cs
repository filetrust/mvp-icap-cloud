using DurableFileProcessing.ActivityFunctions;
using DurableFileProcessing.Interfaces;
using Flurl.Http.Testing;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace DurableFileProcessing.Tests.ActivityFunctions
{
    public class GetFileTypeTests
    {
        public class RunMethod : GetFileTypeTests
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

                var durableActivityContext = new Mock<IDurableActivityContext>();
                var logger = new Mock<ILogger>();
                var configuration = new Mock<IConfigurationSettings>();

                configuration.SetupGet(s => s.FiletypeDetectionKey).Returns("TestKey");
                configuration.SetupGet(s => s.FiletypeDetectionUrl).Returns("http://testrebuildurl");

                durableActivityContext.Setup(s => s.GetInput<string>())
                    .Returns(blobSas);

                _httpTest.RespondWithJson(new { FileTypeName = expectedFileType });

                var classToTest = new GetFileType(configuration.Object);

                // Act
                var outcome = await classToTest.Run(durableActivityContext.Object, logger.Object);

                // Assert
                Assert.That(outcome, Is.EqualTo(expectedFileType));
            }

            [Test]
            public async Task Error_Is_Returned_When_Issues_With_DetectingFileType()
            {
                // Arrange
                const string blobSas = "http://im-a-blob-sas";

                var durableActivityContext = new Mock<IDurableActivityContext>();
                var logger = new Mock<ILogger>();
                var configuration = new Mock<IConfigurationSettings>();

                configuration.SetupGet(s => s.FiletypeDetectionKey).Returns("TestKey");
                configuration.SetupGet(s => s.FiletypeDetectionUrl).Returns("http://testrebuildurl");

                durableActivityContext.Setup(s => s.GetInput<string>())
                    .Returns(blobSas);

                _httpTest.RespondWith(string.Empty, 500);

                var classToTest = new GetFileType(configuration.Object);

                // Act

                var outcome = await classToTest.Run(durableActivityContext.Object, logger.Object);

                // Assert
                Assert.That(outcome, Is.EqualTo("Error"));
            }
        }
    }
}
