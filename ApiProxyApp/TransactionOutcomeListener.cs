using Microsoft.Azure.ServiceBus;
using System;

namespace ApiProxyApp
{
    class TransactionOutcomeListener : IMessageListener
    {
        private Func<Message, bool> notificationFunction;

        public TransactionOutcomeListener(string id)
        {
            Id = id;
        }

        public string MessageType => "transaction-outcome";

        public string Id { get; private set; }

        public bool Notify(Message message)
        {
            if (notificationFunction == null)
                return false;

            return notificationFunction(message);
        }

        public void RegisterNotificationAction(Func<Message, bool> function)
        {
            notificationFunction = function;
        }
    }
}
