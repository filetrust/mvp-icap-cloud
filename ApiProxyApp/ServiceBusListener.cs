using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApiProxyApp
{
    internal class ServiceBusListener
    {
        private IQueueClient queueClient;
        private HashSet<IMessageListener> registeredListeners;

        public ServiceBusListener(string connectionString, string queueName)
        {
            queueClient = new QueueClient(connectionString, queueName);
            registeredListeners = new HashSet<IMessageListener>();
        }

        public void RegisterListener(IMessageListener messageListener)
        {
            registeredListeners.Add(messageListener);
        }

        public void StartListening(string identityKeyName)
        {
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = false
            };

            queueClient.RegisterMessageHandler(async (Message msg, CancellationToken ct) =>
            {
                var messageListeners = registeredListeners.Where(r => r.MessageType == msg.Label);
                if (!messageListeners.Any())
                {
                    Console.WriteLine($"No listeners for Message Type {msg.Label}.");
                    return;
                }

                messageListeners = registeredListeners.Where(r => r.Id == msg.UserProperties[identityKeyName] as string);
                if (messageListeners.Any())
                {
                    foreach(var messageListener in messageListeners)
                    {
                        Console.WriteLine($"Notifying listener for '{msg.UserProperties[identityKeyName]}.");

                        bool processed = messageListener.Notify(msg);
                        if (processed)
                        {
                            Console.WriteLine($"Processed '{msg.UserProperties[identityKeyName]}.");

                            await queueClient.CompleteAsync(msg.SystemProperties.LockToken);
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"No listeners for '{msg.UserProperties[identityKeyName]}.");
                    await queueClient.AbandonAsync(msg.SystemProperties.LockToken);
                }

            }, messageHandlerOptions);
        }

        static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            return Task.CompletedTask;
        }


    }
}