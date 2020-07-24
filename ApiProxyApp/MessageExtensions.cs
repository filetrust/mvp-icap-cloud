using Microsoft.Azure.ServiceBus;

namespace ApiProxyApp
{
    public static class MessageExtensions
    {
        public static string GetMessageProperty(this Message message, string propertyKey)
        {
            return message.UserProperties.ContainsKey(propertyKey) ? message.UserProperties[propertyKey] as string : "missing outcome";
        }
    }
}
