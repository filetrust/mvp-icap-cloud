namespace DurableFileProcessing.Services
{
    public interface IConfigurationSettings
    {
        string FileProcessingStorage { get; }
        string ServiceBusConnectionString { get; }
        string TransactionOutcomeQueueName { get; }
        string FiletypeDetectionUrl { get; }
        string FiletypeDetectionKey { get; }
        string RebuildUrl { get; }
        string RebuildKey { get; }
    }
}