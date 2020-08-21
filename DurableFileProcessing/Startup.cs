using DurableFileProcessing.Interfaces;
using DurableFileProcessing.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(DurableFileProcessing.Startup))]
namespace DurableFileProcessing
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IBlobUtilities, BlobUtilities>();
            builder.Services.AddTransient(typeof(IMessageClient<>), typeof(AzureServiceBusClient));
            builder.Services.AddTransient(typeof(IStorageAccount<>), typeof(AzureStorageAccount));
            builder.Services.AddSingleton<IConfigurationSettings, ConfigurationSettings>();
        }
    }
}