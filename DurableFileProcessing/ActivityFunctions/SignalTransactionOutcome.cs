using DurableFileProcessing.Models;
using DurableFileProcessing.Services;
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
        [FunctionName("FileProcessing_SignalTransactionOutcome")]
        public static async Task Run([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            (IConfigurationSettings configuration, string fileId, RebuildOutcome outcome) = context.GetInput<(IConfigurationSettings, string, RebuildOutcome)>();
            log.LogInformation($"SignalTransactionOutcome, fileId='{fileId}', outcome='{outcome.Outcome}'");
            log.LogInformation($"SignalTransactionOutcome, ServiceBusConnectionString='{configuration.ServiceBusConnectionString}', TransactionOutcomeQueueName='{configuration.TransactionOutcomeQueueName}'");

            var queueClient = new QueueClient(configuration.ServiceBusConnectionString, configuration.TransactionOutcomeQueueName);
            var message = new Message
            {
                Label = "transaction-outcome"
            };
            message.UserProperties.Add("file-id", fileId);
            message.UserProperties.Add("file-outcome", Enum.GetName(typeof(ProcessingOutcome), outcome.Outcome));
            message.UserProperties.Add("file-rebuild-sas", outcome.RebuiltFileSas);
            await queueClient.SendAsync(message);

            await queueClient.CloseAsync();
        }
    }
}