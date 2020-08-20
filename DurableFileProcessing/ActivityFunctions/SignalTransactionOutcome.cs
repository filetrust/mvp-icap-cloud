using DurableFileProcessing.Interfaces;
using DurableFileProcessing.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DurableFileProcessing.ActivityFunctions
{
    public class SignalTransactionOutcome
    {
        private IAzureQueueClient _queueClient;

        public SignalTransactionOutcome(IAzureQueueClient queueClient)
        {
            _queueClient = queueClient;
        }

        [FunctionName("FileProcessing_SignalTransactionOutcome")]
        public async Task Run([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            (string fileId, RebuildOutcome outcome) = context.GetInput<(string, RebuildOutcome)>();

            log.LogInformation($"SignalTransactionOutcome, fileId='{fileId}', outcome='{outcome.Outcome}'");

            var message = new Message
            {
                Label = "transaction-outcome"
            };

            message.UserProperties.Add("file-id", fileId);
            message.UserProperties.Add("file-outcome", Enum.GetName(typeof(ProcessingOutcome), outcome.Outcome));
            message.UserProperties.Add("file-rebuild-sas", outcome.RebuiltFileSas);

            await _queueClient.SendAsync(message);

            await _queueClient.CloseAsync();
        }
    }
}