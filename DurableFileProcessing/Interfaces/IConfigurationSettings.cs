namespace DurableFileProcessing.Interfaces
{
    public interface IConfigurationSettings
    {
        string FileProcessingStorage { get; }
        string CacheConnectionString { get; }
        string ServiceBusConnectionString { get; }
        string TransactionOutcomeQueueName { get; }
        string TransactionOutcomeTableName { get; }
        string FiletypeDetectionUrl { get; }
        string FiletypeDetectionKey { get; }
        string RebuildUrl { get; }
        string RebuildKey { get; }
    }
}