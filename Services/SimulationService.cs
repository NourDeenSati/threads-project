using FirstApi.DTOs;
using FirstApi.Models;

namespace FirstApi.Services;

public class SimulationService
{
    private readonly CapacityControlService _capacityControlService;
    private readonly ILogger<SimulationService> _logger;
    private readonly OrderService _orderService;
    private readonly StoreService _storeService;

    public SimulationService(
        CapacityControlService capacityControlService,
        ILogger<SimulationService> logger,
        OrderService orderService,
        StoreService storeService)
    {
        _capacityControlService = capacityControlService;
        _logger = logger;
        _orderService = orderService;
        _storeService = storeService;
    }

    public Task<SimulationMetricsResponse> RunSequentialAsync(
        SimulationRequest request,
        CancellationToken cancellationToken = default) =>
        RunSimulationAsync(request, SimulationType.Sequential, cancellationToken);

    public Task<SimulationMetricsResponse> RunConcurrentAsync(
        SimulationRequest request,
        CancellationToken cancellationToken = default) =>
        RunSimulationAsync(request, SimulationType.Concurrent, cancellationToken);

    public Task<SimulationMetricsResponse> RunRaceConditionAsync(
        SimulationRequest request,
        CancellationToken cancellationToken = default) =>
        RunSimulationAsync(request, SimulationType.Race, cancellationToken);

    public async Task<SimulationMetricsResponse> RunSimulationAsync(
        SimulationRequest request,
        SimulationType simulationType,
        CancellationToken cancellationToken = default)
    {
        _storeService.Reset();

        var initialStock = _storeService.GetProductById(request.ProductId)?.StockQuantity ?? 0;
        var threadTracker = new ThreadTracker();

        IReadOnlyCollection<CheckoutResult> results = simulationType switch
        {
            SimulationType.Sequential => await RunSequentialStrategyAsync(
                request,
                threadTracker,
                cancellationToken),
            SimulationType.Concurrent => await RunConcurrentStrategyAsync(
                request,
                threadTracker,
                cancellationToken),
            SimulationType.Race => await RunRaceConditionStrategyAsync(
                request,
                threadTracker,
                cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(simulationType), simulationType, null)
        };

        return BuildMetricsResponse(request, simulationType, results, threadTracker, initialStock);
    }

    // ===== Sequential Simulation =====
    // One request finishes before the next request starts.
    // This usually reuses the same thread because there is no parallel fan-out.
    private async Task<IReadOnlyCollection<CheckoutResult>> RunSequentialStrategyAsync(
        SimulationRequest request,
        ThreadTracker threadTracker,
        CancellationToken cancellationToken)
    {
        var results = new List<CheckoutResult>();

        for (var requestNumber = 1; requestNumber <= request.NumberOfRequests; requestNumber++)
        {
            results.Add(await ExecuteRequestAsync(
                request,
                requestNumber,
                SimulationType.Sequential,
                threadTracker,
                RunSafeCheckoutAsync,
                cancellationToken));
        }

        return results;
    }

    // ===== Concurrent Simulation =====
    // We start many requests together with Task.WhenAll.
    // Capacity control still protects the shared checkout path.
    private async Task<IReadOnlyCollection<CheckoutResult>> RunConcurrentStrategyAsync(
        SimulationRequest request,
        ThreadTracker threadTracker,
        CancellationToken cancellationToken)
    {
        var tasks = Enumerable.Range(1, request.NumberOfRequests)
            .Select(requestNumber => Task.Run(
                async () => await ExecuteRequestAsync(
                    request,
                    requestNumber,
                    SimulationType.Concurrent,
                    threadTracker,
                    RunSafeCheckoutAsync,
                    cancellationToken),
                cancellationToken))
            .ToArray();

        return await Task.WhenAll(tasks);
    }

    // ===== Race Condition Simulation =====
    // We also start many requests together with Task.WhenAll,
    // but this path intentionally skips synchronization and capacity control.
    // Because many requests read the same stock before writing it back,
    // students can observe inconsistent results and overselling.
    private async Task<IReadOnlyCollection<CheckoutResult>> RunRaceConditionStrategyAsync(
        SimulationRequest request,
        ThreadTracker threadTracker,
        CancellationToken cancellationToken)
    {
        var tasks = Enumerable.Range(1, request.NumberOfRequests)
            .Select(requestNumber => Task.Run(
                async () => await ExecuteRequestAsync(
                    request,
                    requestNumber,
                    SimulationType.Race,
                    threadTracker,
                    RunUnsafeCheckoutAsync,
                    cancellationToken),
                cancellationToken))
            .ToArray();

        return await Task.WhenAll(tasks);
    }

    private async Task<CheckoutResult> ExecuteRequestAsync(
        SimulationRequest request,
        int requestNumber,
        SimulationType simulationType,
        ThreadTracker threadTracker,
        Func<CheckoutRequest, CancellationToken, Task<CheckoutResult>> checkoutStrategy,
        CancellationToken cancellationToken)
    {
        threadTracker.TrackCurrentThread();
        var startingThreadId = Thread.CurrentThread.ManagedThreadId;

        _logger.LogInformation(
            "Simulation {SimulationType}: request {RequestNumber} started on thread {ThreadId}.",
            ToScenarioName(simulationType),
            requestNumber,
            startingThreadId);

        try
        {
            var checkoutRequest = new CheckoutRequest
            {
                ProductId = request.ProductId,
                Quantity = request.QuantityPerRequest
            };

            var result = await checkoutStrategy(checkoutRequest, cancellationToken);

            threadTracker.TrackCurrentThread();
            _logger.LogInformation(
                "Simulation {SimulationType}: request {RequestNumber} finished on thread {ThreadId}.",
                ToScenarioName(simulationType),
                requestNumber,
                Thread.CurrentThread.ManagedThreadId);

            return result;
        }
        catch (Exception exception)
        {
            threadTracker.TrackCurrentThread();
            _logger.LogWarning(
                exception,
                "Simulation {SimulationType}: request {RequestNumber} failed on thread {ThreadId}.",
                ToScenarioName(simulationType),
                requestNumber,
                Thread.CurrentThread.ManagedThreadId);

            return CheckoutResult.Fail("simulation_exception", exception.Message);
        }
    }

    private Task<CheckoutResult> RunSafeCheckoutAsync(
        CheckoutRequest request,
        CancellationToken _)
    {
        return _capacityControlService.RunAsync(() => Task.FromResult(_orderService.Checkout(request)));
    }

    private Task<CheckoutResult> RunUnsafeCheckoutAsync(
        CheckoutRequest request,
        CancellationToken cancellationToken) =>
        _orderService.CheckoutWithoutSynchronizationForDemoAsync(request, cancellationToken);

    private SimulationMetricsResponse BuildMetricsResponse(
        SimulationRequest request,
        SimulationType simulationType,
        IReadOnlyCollection<CheckoutResult> results,
        ThreadTracker threadTracker,
        int initialStock)
    {
        var successCount = results.Count(result => result.Success);
        var finalStock = _storeService.GetProductById(request.ProductId)?.StockQuantity ?? 0;

        return new SimulationMetricsResponse
        {
            Scenario = ToScenarioName(simulationType),
            TotalRequests = request.NumberOfRequests,
            SuccessCount = successCount,
            FailureCount = request.NumberOfRequests - successCount,
            UniqueThreadCount = threadTracker.GetUniqueThreadCount(),
            Threads = threadTracker.GetUniqueThreads(),
            InitialStock = initialStock,
            FinalStock = finalStock,
            OversellingOccurred = successCount > initialStock
        };
    }

    private static string ToScenarioName(SimulationType simulationType) =>
        simulationType switch
        {
            SimulationType.Sequential => "sequential",
            SimulationType.Concurrent => "concurrent",
            SimulationType.Race => "race",
            _ => throw new ArgumentOutOfRangeException(nameof(simulationType), simulationType, null)
        };
}
