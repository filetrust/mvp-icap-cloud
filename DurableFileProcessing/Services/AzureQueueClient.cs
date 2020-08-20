using DurableFileProcessing.Interfaces;
using Microsoft.Azure.ServiceBus;
using System;
using System.Threading.Tasks;

namespace DurableFileProcessing.Services
{
    public class AzureQueueClient : IAzureQueueClient
    {
        private readonly QueueClient _queueClient;
        private readonly IConfigurationSettings _configurationSettings;

        public AzureQueueClient(IConfigurationSettings configurationSettings)
        {
            _configurationSettings = configurationSettings;

            _queueClient = new QueueClient(_configurationSettings.ServiceBusConnectionString, _configurationSettings.TransactionOutcomeQueueName);
        }

        public async Task CloseAsync()
        {
            await _queueClient.CloseAsync();
        }

        public async Task SendAsync(Message message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            await _queueClient.SendAsync(message);
        }
    }
}