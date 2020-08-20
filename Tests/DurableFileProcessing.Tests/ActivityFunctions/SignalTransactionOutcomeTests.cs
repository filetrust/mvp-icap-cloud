using DurableFileProcessing.ActivityFunctions;
using DurableFileProcessing.Interfaces;
using DurableFileProcessing.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace DurableFileProcessing.Tests.ActivityFunctions
{
    public class SignalTransactionOutcomeTests
    {
        public class RunMethod : SignalTransactionOutcomeTests
        {
            private const string ExpectedLabel = "transaction-outcome";
            private const string ExpectedFileId = "file id";
            private const string ExpectedRebuiltFileSas = "rebuilt file sas";
            private const string ExpectedOutcome = "Rebuilt";

            private RebuildOutcome _rebuildOutcome;

            private Mock<IAzureQueueClient> _mockQueueClient;
            private Mock<IDurableActivityContext> _mockContext;
            private Mock<ILogger> _mockLogger;

            private SignalTransactionOutcome _signalTransactionOutcome;

            [SetUp]
            public void Setup()
            {
                _mockQueueClient = new Mock<IAzureQueueClient>();
                _mockContext = new Mock<IDurableActivityContext>();
                _mockLogger = new Mock<ILogger>();

                _rebuildOutcome = new RebuildOutcome
                {
                    Outcome = ProcessingOutcome.Rebuilt,
                    RebuiltFileSas = ExpectedRebuiltFileSas
                };

                _mockContext.Setup(s => s.GetInput<(string, RebuildOutcome)>()).Returns((ExpectedFileId, _rebuildOutcome));

                _signalTransactionOutcome = new SignalTransactionOutcome(_mockQueueClient.Object);
            }

            [Test]
            public async Task When_Run_Correct_Message_Is_Sent_To_The_Queue_Client()
            {
                // Arrange

                // Act
                await _signalTransactionOutcome.Run(_mockContext.Object, _mockLogger.Object);

                // Assert
                _mockQueueClient.Verify(s => s.SendAsync(
                    It.Is<Message>(msg =>
                        msg.Label == ExpectedLabel &&
                        msg.UserProperties["file-id"].ToString() == ExpectedFileId &&
                        msg.UserProperties["file-outcome"].ToString() == ExpectedOutcome &&
                        msg.UserProperties["file-rebuild-sas"].ToString() == ExpectedRebuiltFileSas
                    )), Times.Once);
            }

            [Test]
            public async Task When_Run_QueueClient_Is_Closed_After_Processing()
            {
                // Arrange

                // Act
                await _signalTransactionOutcome.Run(_mockContext.Object, _mockLogger.Object);

                // Assert
                _mockQueueClient.Verify(s => s.CloseAsync(), Times.Once);
            }
        }
    }
}
