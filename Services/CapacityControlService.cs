namespace FirstApi.Services;

public class CapacityControlService
{
    private readonly SemaphoreSlim _semaphore;

    public int MaxConcurrentOperations { get; } = 5;

    public CapacityControlService()
    {
        // Requirement 2: limit how many checkout operations can run at the same time.
        _semaphore = new SemaphoreSlim(MaxConcurrentOperations, MaxConcurrentOperations);
    }

    public async Task<T> RunAsync<T>(Func<Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await _semaphore.WaitAsync();

        try
        {
            return await operation();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}