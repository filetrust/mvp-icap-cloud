using Microsoft.Azure.ServiceBus;
using System;
using System.Threading.Tasks;

namespace ApiProxyApp
{
    internal class ServiceBusListener
    {
        private string connectionString;
        private string queueName;
        private IQueueClient queueClient;
        public ServiceBusListener(string connectionString, string queueName)
        {
            this.connectionString = connectionString;
            this.queueName = queueName;
            queueClient = new QueueClient(connectionString, queueName);
        }

        internal Task ListenFor(string messageType, string propertyKey, string propertyValue)
        {
            throw new NotImplementedException();
        }
    }
}