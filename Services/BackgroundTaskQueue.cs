using System.Threading.Channels;

namespace FirstApi.Services
{
    public class BackgroundTaskQueue
    {
        private readonly Channel<int> _queue;

        public BackgroundTaskQueue()
        {
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<int>(options);
        }

        public async ValueTask QueueOrderAsync(int orderId) =>
            await _queue.Writer.WriteAsync(orderId);

        public async ValueTask<int> DequeueAsync(CancellationToken ct) =>
            await _queue.Reader.ReadAsync(ct);
    }
}
