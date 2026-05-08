using FirstApi.Services;

namespace FirstApi.Workers
{
    public class OrderProcessingWorker : BackgroundService
    {
        private readonly BackgroundTaskQueue _taskQueue;

        public OrderProcessingWorker(BackgroundTaskQueue taskQueue) => _taskQueue = taskQueue;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var orderId = await _taskQueue.DequeueAsync(stoppingToken);

                await Task.Delay(2000, stoppingToken);

                Console.WriteLine($"[Background Service]: Successfully processed order {orderId}.");
            }
        }
    }
}
