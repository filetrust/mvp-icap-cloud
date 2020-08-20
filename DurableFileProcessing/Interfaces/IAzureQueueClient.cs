using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace DurableFileProcessing.Interfaces
{
    public interface IAzureQueueClient
    {
        Task SendAsync(Message message);
        Task CloseAsync();
    }
}