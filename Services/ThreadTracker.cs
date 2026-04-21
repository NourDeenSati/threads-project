using System.Collections.Concurrent;

namespace FirstApi.Services;

public class ThreadTracker
{
    private readonly ConcurrentDictionary<int, bool> _threadIds = new();

    public void TrackCurrentThread() =>
        _threadIds.TryAdd(Thread.CurrentThread.ManagedThreadId, true);

    public int GetUniqueThreadCount() => _threadIds.Count;

    public List<int> GetUniqueThreads() => _threadIds.Keys.OrderBy(id => id).ToList();
}
