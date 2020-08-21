using DurableFileProcessing.ActivityFunctions;
using DurableFileProcessing.Models;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace DurableFileProcessing.Tests.ActivityFunctions
{
    public class CheckAvailableOutcomeTests
    {
        public class RunMethod : CheckAvailableOutcomeTests
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
                var outcome = CheckAvailableOutcome.Run(durableActivityContext.Object, logger.Object);

                // Assert
                Assert.That(outcome, Is.EqualTo(ProcessingOutcome.Unknown));
            }
        }
    }
}