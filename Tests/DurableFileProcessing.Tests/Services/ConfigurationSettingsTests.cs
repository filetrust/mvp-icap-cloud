using DurableFileProcessing.Services;
using NUnit.Framework;
using System;

namespace DurableFileProcessing.Tests.Services
{
    public class ConfigurationSettingsTests
    {
        private const string FileProcessingStorage = "testStorage";
        private const string FiletypeDetectionKey = "testFileTypeDetectionKey";
        private const string FiletypeDetectionUrl = "testFileTypeDestectionUrl";
        private const string RebuildKey = "testRebuildKey";
        private const string RebuildUrl = "testRebuildUrl";
        private const string ServiceBusConnectionString = "TestServiceBusConnectionString";
        private const string TransactionOutcomeQueueName = "testQueueName";

        [Test]
        public void Configuration_Properties_Are_The_Correct_Values()
        {
            // Arrange
            Environment.SetEnvironmentVariable("FileProcessingStorage", FileProcessingStorage);
            Environment.SetEnvironmentVariable("TransactionOutcomeQueueName", TransactionOutcomeQueueName);
            Environment.SetEnvironmentVariable("FiletypeDetectionUrl", FiletypeDetectionUrl);
            Environment.SetEnvironmentVariable("ServiceBusConnectionString", ServiceBusConnectionString);
            Environment.SetEnvironmentVariable("FiletypeDetectionKey", FiletypeDetectionKey);
            Environment.SetEnvironmentVariable("RebuildUrl", RebuildUrl);
            Environment.SetEnvironmentVariable("RebuildKey", RebuildKey);

            // Act
            var configurationSettings = new ConfigurationSettings();

            // Assert
            Assert.That(configurationSettings.FileProcessingStorage, Is.EqualTo(FileProcessingStorage));
            Assert.That(configurationSettings.FiletypeDetectionKey, Is.EqualTo(FiletypeDetectionKey));
            Assert.That(configurationSettings.FiletypeDetectionUrl, Is.EqualTo(FiletypeDetectionUrl));
            Assert.That(configurationSettings.RebuildKey, Is.EqualTo(RebuildKey));
            Assert.That(configurationSettings.RebuildUrl, Is.EqualTo(RebuildUrl));
            Assert.That(configurationSettings.ServiceBusConnectionString, Is.EqualTo(ServiceBusConnectionString));
            Assert.That(configurationSettings.TransactionOutcomeQueueName, Is.EqualTo(TransactionOutcomeQueueName));
        }
    }
}
