namespace DurableFileProcessing.Models
{
    public class RebuildOutcome
    {
        public ProcessingOutcome Outcome { get; set; }
        public string RebuiltFileSas { get; set; }
    }
}
