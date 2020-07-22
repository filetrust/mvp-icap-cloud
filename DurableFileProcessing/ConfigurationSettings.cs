namespace DurableFileProcessing
{
    public class ConfigurationSettings
    {
        public string FileProcessingStorage { get; set; }        
        public string ServiceBusConnectionString { get; set; }
        public string TransactionOutcomeQueueName  { get; set; }
        public string FiletypeDetectionUrl { get; set; }
        public string FiletypeDetectionKey { get; set; }
        public string RebuildUrl { get; set; }
        public string RebuildKey { get; set; }
    }
}
