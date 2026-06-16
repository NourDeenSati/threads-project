using FirstApi.Services;

namespace FirstApi.Workers
{
    public class DailySalesBatchJob : BackgroundService
    {
        private readonly InMemoryStore _store;
        private readonly ILogger<DailySalesBatchJob> _logger;
        private const int ChunkSize = 5;
        public DailySalesBatchJob(InMemoryStore store, ILogger<DailySalesBatchJob> logger)
        {
            _store = store;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                _logger.LogInformation("--- Starting sales inventory and batch processing ---");
                var allOrders = _store.Orders.ToList();
                if (!allOrders.Any()) continue;
                var chunks = allOrders.Chunk(ChunkSize);
                int batchNumber = 1;
                foreach (var chunk in chunks)
                {
                    _logger.LogInformation($"Processing batch {batchNumber} (contains {chunk.Length} orders)...");

                    decimal batchTotal = chunk.Sum(o => o.TotalPrice);

                    await Task.Delay(1000, stoppingToken);
                    _logger.LogInformation($"Batch {batchNumber} completed. Total sales in this batch: {batchTotal}$");
                    batchNumber++;
                }
                _logger.LogInformation("--- Daily sales batch processing completed successfully ---");
            }
        }
    }
}
