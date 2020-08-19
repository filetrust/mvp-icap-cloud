using DurableFileProcessing.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace DurableFileProcessing.ActivityFunctions
{
    public class CheckAvailableOutcome
    {
        [FunctionName("FileProcessing_CheckAvailableOutcome")]
        public static ProcessingOutcome Run([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            string hash = context.GetInput<string>();
            var outcome = ProcessingOutcome.Unknown;
            log.LogInformation($"CheckAvailableOutcome, hash='{hash}', Outcome = {outcome}");
            return outcome;
        }
    }
}