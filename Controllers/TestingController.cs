using FirstApi.DTOs;
using FirstApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FirstApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestingController : ControllerBase
{
    private readonly CapacityControlService _capacityControlService;
    private readonly InMemoryStore _store;
    private readonly OrderService _orderService;

    public TestingController(
        CapacityControlService capacityControlService,
        InMemoryStore store,
        OrderService orderService)
    {
        _capacityControlService = capacityControlService;
        _store = store;
        _orderService = orderService;
    }

    [HttpPost("simulate-race-condition")]
    public async Task<IActionResult> SimulateRaceCondition([FromBody] SimulationRequest request)
    {

        var tasks = Enumerable.Range(1, request.NumberOfRequests).Select(async _ => Task.Run(() =>
        {
            return _orderService.UnsafeCheckout(new CheckoutRequest
            {
                ProductId = request.ProductId,
                Quantity = request.QuantityPerRequest
            });
        }));

        var results = await Task.WhenAll(tasks);
        var finalProduct = _store.Products.FirstOrDefault(p => p.Id == request.ProductId);

        return Ok(new
        {
            totalRequests = request.NumberOfRequests,
            succeeded = results.Count(result => result.Result.Success),
            failed = results.Count(result => !result.Result.Success),
            finalStock = finalProduct?.StockQuantity,
            note = "This endpoint is only for learning and testing. It lets you simulate many checkout requests while the app now uses locking for correctness and SemaphoreSlim for capacity control."
        });
    }


    [HttpPost("simulate-concurrent-checkouts")]
    public async Task<IActionResult> SimulateConcurrentCheckouts([FromBody] SimulationRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        if (request.ProductId <= 0)
        {
            return BadRequest(new { message = "ProductId must be greater than 0." });
        }

        if (request.QuantityPerRequest <= 0)
        {
            return BadRequest(new { message = "QuantityPerRequest must be greater than 0." });
        }

        if (request.NumberOfRequests <= 0)
        {
            return BadRequest(new { message = "NumberOfRequests must be greater than 0." });
        }

        // Run many checkout operations at the same time for learning and testing.
        var tasks = Enumerable.Range(1, request.NumberOfRequests)
            .Select(async _ =>
            {
                var checkoutRequest = new CheckoutRequest
                {
                    ProductId = request.ProductId,
                    Quantity = request.QuantityPerRequest
                };

                return await _capacityControlService.RunAsync(async () =>
                {
                    return await _orderService.Checkout(checkoutRequest);
                });
            })
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var finalProduct = _store.Products.FirstOrDefault(p => p.Id == request.ProductId);

        return Ok(new
        {
            totalRequests = request.NumberOfRequests,
            succeeded = results.Count(result => result.Success),
            failed = results.Count(result => !result.Success),
            finalStock = finalProduct?.StockQuantity,
            maxConcurrentOperations = _capacityControlService.MaxConcurrentOperations,
            note = "This endpoint is only for learning and testing. It lets you simulate many checkout requests while the app now uses locking for correctness and SemaphoreSlim for capacity control."
        });
    }

    [HttpPost("safe-checkout")]
    public IActionResult SafeCheckout([FromBody] CheckoutRequest request)
    {
        lock (_store.GetLockForProduct(request.ProductId))
        {
            var product = _store.Products.FirstOrDefault(p => p.Id == request.ProductId);
            if (product.StockQuantity >= request.Quantity)
            {
                product.StockQuantity -= request.Quantity;
                return Ok(new { product.StockQuantity, Message = "Successful checkout" });
            }
            return BadRequest(new { Message = "Insufficient stock" });
        }
    }

    [HttpPost("unlimited-capacity")]
    public async Task<IActionResult> UnlimitedCapacity([FromBody] CheckoutRequest request)
    {
        var result = await _orderService.Checkout(request);
        return Ok(result);
    }

    [HttpPost("limited-capacity")]
    public async Task<IActionResult> LimitedCapacity([FromBody] CheckoutRequest request)
    {
        var result = await _capacityControlService.RunAsync(async () =>
        {
            return await _orderService.Checkout(request);
        });

        return Ok(result);
    }

    [HttpPost("asynchronous-checkout")]
    public async Task<IActionResult> AsynchronousCheckout([FromBody] CheckoutRequest request)
    {
        var result = await _orderService.CheckoutAsynchronous(request);
        return Ok(result);
    }

    [HttpPost("synchronous-checkout")]
    public IActionResult SynchronousCheckout([FromBody] CheckoutRequest request)
    {
        var result = _orderService.Checkout(request).Result;
        return Ok(result);
    }
}