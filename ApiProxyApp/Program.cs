using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApiProxyApp
{

    class Program
    {
        const string FolderKey = "folder-key";
        const string BlobConnectionStringKey = "blob-container-connection-string";
        const string ServiceBusConnectionStringKey = "service-bus-connection-string";
        const string InputContainerNameKey = "input-container-name";
        const string OutcomeQueueNameKey = "outcome-queue-name";
        static void Main(string[] args)
        {
            var switchMapping = GetSwitchMapping();
            IConfiguration Configuration = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables()
              .AddCommandLine(args, switchMapping)
              .Build();

            Console.WriteLine($"{FolderKey} = {Configuration[FolderKey]}");
            Console.WriteLine($"{BlobConnectionStringKey} = {Configuration[BlobConnectionStringKey]}");
            Console.WriteLine($"{ServiceBusConnectionStringKey} = {Configuration[ServiceBusConnectionStringKey]}");
            Console.WriteLine($"{InputContainerNameKey} = {Configuration[InputContainerNameKey]}");
            Console.WriteLine($"{OutcomeQueueNameKey} = {Configuration[OutcomeQueueNameKey]}");

            var files = GetFolderContents(Configuration[FolderKey]);
            var messageStore = new BlockingCollection<Message>();

            SubmitFiles(Configuration, files, messageStore);

            ProcessMessages(files, messageStore);

            Console.WriteLine("Press any key to finish");
            Console.ReadKey();
        }

        private static void SubmitFiles(IConfiguration Configuration, IEnumerable<string> files, BlockingCollection<Message> messageStore)
        {
            var queueClient = new QueueClient(Configuration[ServiceBusConnectionStringKey], Configuration[OutcomeQueueNameKey]);
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = false
            };
            queueClient.RegisterMessageHandler(async (Message msg, CancellationToken ct) =>
            {
                if (msg.Label != "transaction-outcome")
                    return;

                messageStore.Add(msg);

                await queueClient.CompleteAsync(msg.SystemProperties.LockToken);
            }, messageHandlerOptions);

            var writer = new ContainerWriter(Configuration[BlobConnectionStringKey], Configuration[InputContainerNameKey]);
            var pendingTasks = new List<Task>();
            foreach (var file in files)
            {
                pendingTasks.Add(writer.Write(file));
            }

            Task.WaitAll(pendingTasks.ToArray());
        }

        private static void ProcessMessages(IEnumerable<string> files, BlockingCollection<Message> messageStore)
        {
            var outstandingOutcomes = new List<string>(files);
            var receivedOutcomes = new List<OutcomeInformation>();
            try
            {
                do
                {
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                    var message = messageStore.Take(cts.Token);
                    var receivedOutcome = new OutcomeInformation
                    {
                        FileId = message.GetMessageProperty("file-id"),
                        Outcome = message.GetMessageProperty("file-outcome"),
                        RebuildSas = message.GetMessageProperty("file-rebuild-sas")
                    };
                    receivedOutcomes.Add(receivedOutcome);
                    Console.WriteLine($"Received outcome for {receivedOutcome.FileId}");
                } while (outstandingOutcomes.Any());
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"Time-out waiting on outcomes for {String.Join(',', outstandingOutcomes)}");
            }

            foreach (var outcome in receivedOutcomes)
            {
                Console.WriteLine($"{outcome.FileId} as outcome of {outcome.Outcome}\n\t{outcome.RebuildSas}");
            }
        }

        static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            return Task.CompletedTask;
        }

        private static Task WriteToContainer(string container, string file)
        {
            throw new NotImplementedException();
        }

        private static IDictionary<string, string> GetSwitchMapping()
        {
            IDictionary<string, string> mapping = new Dictionary<string, string>
            {
                { "-f", FolderKey },
                { "--folder", FolderKey},
            };
            return mapping;
        }

        private static IEnumerable<string> GetFolderContents(string folderName)
        {
            Console.WriteLine($"The specified folder is {folderName}");
            return  Directory.GetFiles(folderName);
        }
    }
}
