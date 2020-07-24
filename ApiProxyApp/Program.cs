using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ApiProxyApp
{
    class Program
    {
        const string FolderKey = "folder-path";
        const string BlobConnectionStringKey = "blob-container-connection-string";
        const string ServiceBusConnectionStringKey = "service-bus-connection-string";
        const string InputContainerNameKey = "input-container-name";
        const string OutcomeQueueNameKey = "outcome-queue-name";
        static async Task Main(string[] args)
        {
            var switchMapping = GetSwitchMapping();
            IConfiguration Configuration = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
              .AddJsonFile("secret.appsettings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables()
              .AddCommandLine(args, switchMapping)
              .Build();

            Console.WriteLine($"{FolderKey} = {Configuration[FolderKey]}");
            Console.WriteLine($"{BlobConnectionStringKey} = {Configuration[BlobConnectionStringKey]}");
            Console.WriteLine($"{ServiceBusConnectionStringKey} = {Configuration[ServiceBusConnectionStringKey]}");
            Console.WriteLine($"{InputContainerNameKey} = {Configuration[InputContainerNameKey]}");
            Console.WriteLine($"{OutcomeQueueNameKey} = {Configuration[OutcomeQueueNameKey]}");

            var serviceBusListener = new ServiceBusListener(Configuration[ServiceBusConnectionStringKey], Configuration[OutcomeQueueNameKey]);
            serviceBusListener.StartListening("file-id");

            var files = GetFolderContents(Configuration[FolderKey]);

            var receivedOutcomeInformation = new List<OutcomeInformation>();

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            foreach (var file in files)
            {
                var messageListener = new TransactionOutcomeListener(Path.GetFileName(file));
                messageListener.RegisterNotificationAction(m =>
                {
                    var receivedOutcome = new OutcomeInformation
                    {
                        FileId = m.GetMessageProperty("file-id"),
                        Outcome = m.GetMessageProperty("file-outcome"),
                        RebuildSas = m.GetMessageProperty("file-rebuild-sas")
                    };
                    receivedOutcomeInformation.Add(receivedOutcome);
                    return true;
                });
                serviceBusListener.RegisterListener(messageListener);
            }

            var writer = new ContainerWriter(Configuration[BlobConnectionStringKey], Configuration[InputContainerNameKey]);
            var submissionTasks = SubmitFiles(writer, files);

            Task.WaitAll(submissionTasks);

            while(files.Count() > receivedOutcomeInformation.Count())
            {
                Console.WriteLine($"{receivedOutcomeInformation.Count()} outcomes received, out of {files.Count()}");
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            stopWatch.Stop();
            OutputResults(receivedOutcomeInformation, stopWatch.Elapsed);

            Console.WriteLine("Press any key to finish");
            Console.ReadKey();            
        }

        private static void OutputResults(List<OutcomeInformation> receivedOutcomeInformation, TimeSpan elapsed)
        {
            Console.WriteLine("Received the following Outcomes");
            foreach(var outcome in receivedOutcomeInformation)
            {
                Console.WriteLine($"{outcome.FileId} as outcome of {outcome.Outcome}\n\t{outcome.RebuildSas}");
            }
            Console.WriteLine($"Processing time = {elapsed:g}");
        }

        private static Task[] SubmitFiles(ContainerWriter writer, IEnumerable<string> files)
        {
            var pendingTasks = new List<Task>();
            foreach (var file in files)
            {
                pendingTasks.Add(writer.Write(file));
            }

            return pendingTasks.ToArray();
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
