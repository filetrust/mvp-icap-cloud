namespace DurableFileProcessing.Interfaces
{
    public interface IStorageAccount<T>
    {
        public T GetClient(string connectionString);
    }
}
