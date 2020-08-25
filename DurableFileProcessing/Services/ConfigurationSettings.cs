using DurableFileProcessing.Interfaces;
using System;

namespace DurableFileProcessing.Services
{
    public class ConfigurationSettings : IConfigurationSettings
    {
        public ConfigurationSettings()
        {
            FileProcessingStorage = Environment.GetEnvironmentVariable("FileProcessingStorage", EnvironmentVariableTarget.Process);
            CacheConnectionString = Environment.GetEnvironmentVariable("CacheConnectionString", EnvironmentVariableTarget.Process);
            TransactionOutcomeQueueName = Environment.GetEnvironmentVariable("TransactionOutcomeQueueName", EnvironmentVariableTarget.Process);
            TransactionOutcomeTableName = Environment.GetEnvironmentVariable("TransactionOutcomeTableName", EnvironmentVariableTarget.Process);
            FiletypeDetectionUrl = Environment.GetEnvironmentVariable("FiletypeDetectionUrl", EnvironmentVariableTarget.Process);
            ServiceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString", EnvironmentVariableTarget.Process);
            FiletypeDetectionKey = Environment.GetEnvironmentVariable("FiletypeDetectionKey", EnvironmentVariableTarget.Process);
            RebuildUrl = Environment.GetEnvironmentVariable("RebuildUrl", EnvironmentVariableTarget.Process);
            RebuildKey = Environment.GetEnvironmentVariable("RebuildKey", EnvironmentVariableTarget.Process);
        }

        public string FileProcessingStorage { get; }
        public string CacheConnectionString { get; }
        public string ServiceBusConnectionString { get; }
        public string TransactionOutcomeQueueName { get; }
        public string TransactionOutcomeTableName { get; }
        public string FiletypeDetectionUrl { get; }
        public string FiletypeDetectionKey { get; }
        public string RebuildUrl { get; }
        public string RebuildKey { get; }
    }
}