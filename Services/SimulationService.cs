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

    public Task<SimulationMetricsResponse> RunSequentialAsync(SimulationRequest request) =>
        RunSimulationAsync(request, SimulationType.Sequential);

    public Task<SimulationMetricsResponse> RunConcurrentAsync(SimulationRequest request) =>
        RunSimulationAsync(request, SimulationType.Concurrent);

    public Task<SimulationMetricsResponse> RunRaceConditionAsync(SimulationRequest request) =>
        RunSimulationAsync(request, SimulationType.Race);

    private async Task<SimulationMetricsResponse> RunSimulationAsync(
        SimulationRequest request,
        SimulationType simulationType)
    {
        _storeService.Reset();

        var initialStock = _storeService.GetProductById(request.ProductId)?.StockQuantity ?? 0;
        var threadTracker = new ThreadTracker();

        IReadOnlyCollection<CheckoutResult> results = simulationType switch
        {
            SimulationType.Sequential => await RunSequentialRequestsAsync(request, threadTracker),
            SimulationType.Concurrent => await RunConcurrentRequestsAsync(request, threadTracker),
            SimulationType.Race => await RunRaceConditionRequestsAsync(request, threadTracker),
            _ => throw new ArgumentOutOfRangeException(nameof(simulationType), simulationType, null)
        };

        return BuildSimulationMetricsResponse(request, simulationType, results, threadTracker, initialStock);
    }

    // Sequential means one request starts only after the previous request finishes.
    // This is the easiest model to understand because there is no overlap and only one
    // checkout operation is active at a time.
    private async Task<IReadOnlyCollection<CheckoutResult>> RunSequentialRequestsAsync(
        SimulationRequest request,
        ThreadTracker threadTracker)
    {
        var results = new List<CheckoutResult>();

        for (var requestNumber = 1; requestNumber <= request.NumberOfRequests; requestNumber++)
        {
            results.Add(await ExecuteRequestAsync(
                request,
                requestNumber,
                SimulationType.Sequential,
                threadTracker,
                checkoutRequest => Task.FromResult(_orderService.Checkout(checkoutRequest))));
        }

        return results;
    }

    // Limited concurrency still starts many tasks together with Task.WhenAll.
    // The difference is that CapacityControlService uses SemaphoreSlim to allow only
    // 5 operations inside the checkout section at the same time.
    // Max = 5 means the sixth operation must wait until one of the first five finishes.
    // This is safer than unlimited concurrency because it reduces overload and keeps
    // parallel execution under control.
    private async Task<IReadOnlyCollection<CheckoutResult>> RunConcurrentRequestsAsync(
        SimulationRequest request,
        ThreadTracker threadTracker)
    {
        _logger.LogInformation(
            "Concurrent simulation is using controlled parallelism with max {MaxConcurrentOperations} operations.",
            _capacityControlService.MaxConcurrentOperations);

        var tasks = Enumerable.Range(1, request.NumberOfRequests)
            .Select(requestNumber => Task.Run(() =>
                ExecuteRequestAsync(
                    request,
                    requestNumber,
                    SimulationType.Concurrent,
                    threadTracker,
                    checkoutRequest => RunLimitedCheckoutAsync(checkoutRequest))))
            .ToArray();

        return await Task.WhenAll(tasks);
    }

    // Race-condition mode also launches many requests together, but there is no limiter here.
    // That means many operations can overlap without control.
    // This is the "unlimited concurrency" demo, so it is easier to overload shared state and
    // observe incorrect results.
    private async Task<IReadOnlyCollection<CheckoutResult>> RunRaceConditionRequestsAsync(
        SimulationRequest request,
        ThreadTracker threadTracker)
    {
        var tasks = Enumerable.Range(1, request.NumberOfRequests)
            .Select(requestNumber => Task.Run(() =>
                ExecuteRequestAsync(
                    request,
                    requestNumber,
                    SimulationType.Race,
                    threadTracker,
                    checkoutRequest => _orderService.CheckoutWithoutSynchronizationForDemoAsync(checkoutRequest))))
            .ToArray();

        return await Task.WhenAll(tasks);
    }

    private async Task<CheckoutResult> ExecuteRequestAsync(
        SimulationRequest request,
        int requestNumber,
        SimulationType simulationType,
        ThreadTracker threadTracker,
        Func<CheckoutRequest, Task<CheckoutResult>> checkoutStrategy)
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
            var checkoutRequest = CreateCheckoutRequest(request);

            var result = await checkoutStrategy(checkoutRequest);

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

    private Task<CheckoutResult> RunLimitedCheckoutAsync(CheckoutRequest request)
    {
        return _capacityControlService.RunAsync(() =>
            Task.FromResult(_orderService.Checkout(request)));
    }

    private static CheckoutRequest CreateCheckoutRequest(SimulationRequest request) =>
        new()
        {
            ProductId = request.ProductId,
            Quantity = request.QuantityPerRequest
        };

    private SimulationMetricsResponse BuildSimulationMetricsResponse(
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
