using DurableFileProcessing.ActivityFunctions;
using DurableFileProcessing.Interfaces;
using DurableFileProcessing.Models;
using Flurl.Http.Testing;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace DurableFileProcessing.Tests.ActivityFunctions
{
    public class RebuildFileTests
    {
        public class RunMethod : RebuildFileTests
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

                var returnedInput = (receivedSas, rebuildSas, receivedFileType);

                var durableActivityContext = new Mock<IDurableActivityContext>();
                var logger = new Mock<ILogger>();
                var settings = new Mock<IConfigurationSettings>();

                settings.SetupGet(s => s.RebuildKey).Returns("TestKey");
                settings.SetupGet(s => s.RebuildUrl).Returns("http://testrebuildurl");

                durableActivityContext.Setup(s => s.GetInput<(string, string, string)>())
                    .Returns(returnedInput);

                _httpTest.RespondWith(string.Empty, 200);

                var classToTest = new RebuildFile(settings.Object);

                // Act
                var outcome = await classToTest.Run(durableActivityContext.Object, logger.Object);

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

                var returnedInput = (receivedSas, rebuildSas, receivedFileType);

                var durableActivityContext = new Mock<IDurableActivityContext>();
                var logger = new Mock<ILogger>();
                var settings = new Mock<IConfigurationSettings>();

                settings.SetupGet(s => s.RebuildKey).Returns("TestKey");
                settings.SetupGet(s => s.RebuildUrl).Returns("http://testrebuildurl");

                durableActivityContext.Setup(s => s.GetInput<(string, string, string)>())
                    .Returns(returnedInput);

                _httpTest.RespondWith(string.Empty, 500);

                var classToTest = new RebuildFile(settings.Object);

                // Act

                var outcome = await classToTest.Run(durableActivityContext.Object, logger.Object);

                // Assert
                Assert.That(outcome, Is.EqualTo(ProcessingOutcome.Error));
            }
        }
    }
}
