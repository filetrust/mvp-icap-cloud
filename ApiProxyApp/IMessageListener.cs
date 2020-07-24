using Microsoft.Azure.ServiceBus;

namespace ApiProxyApp
{
    interface IMessageListener
    {
        string MessageType { get; }
        string Id { get; }

        bool Notify(Message message);
    }
}
