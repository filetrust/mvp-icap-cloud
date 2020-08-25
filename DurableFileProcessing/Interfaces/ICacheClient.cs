namespace DurableFileProcessing.Interfaces
{
    public interface ICacheClient<T>
    {
        T Client { get; }
    }
}
