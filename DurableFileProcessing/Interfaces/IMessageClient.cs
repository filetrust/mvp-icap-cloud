using System.Threading.Tasks;

namespace DurableFileProcessing.Interfaces
{
    public interface IMessageClient<T>
    {
        Task SendAsync(T message);
        Task CloseAsync();
    }
}