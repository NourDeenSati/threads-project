namespace FirstApi.Services;

public class ThreadLimiter
{
    private readonly ILogger<ThreadLimiter> _logger;
    private readonly SemaphoreSlim _semaphore;

    public int MaxConcurrency { get; }

    public ThreadLimiter(int maxConcurrency, ILogger<ThreadLimiter> logger)
    {
        if (maxConcurrency <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Max concurrency must be greater than 0.");
        }

        _logger = logger;
        MaxConcurrency = maxConcurrency;
        _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }

    public Task<T> RunAsync<T>(Func<Task<T>> action) =>
        RunAsync("operation", action);

    public async Task<T> RunAsync<T>(string operationName, Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        // SemaphoreSlim does not create threads.
        // It simply says how many operations are allowed to run inside this section at the same time.
        await _semaphore.WaitAsync();

        _logger.LogInformation(
            "Thread {ThreadId} entered semaphore for {OperationName}. Remaining slots: {RemainingSlots}.",
            Thread.CurrentThread.ManagedThreadId,
            operationName,
            _semaphore.CurrentCount);

        try
        {
            return await action();
        }
        finally
        {
            _semaphore.Release();

            _logger.LogInformation(
                "Thread {ThreadId} exited semaphore for {OperationName}. Remaining slots: {RemainingSlots}.",
                Thread.CurrentThread.ManagedThreadId,
                operationName,
                _semaphore.CurrentCount);
        }
    }
}
