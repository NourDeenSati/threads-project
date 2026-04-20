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
            .Select(_ => Task.Run(async () =>
            {
                var checkoutRequest = new CheckoutRequest
                {
                    ProductId = request.ProductId,
                    Quantity = request.QuantityPerRequest
                };

                return await _capacityControlService.RunAsync(
                    () => Task.FromResult(_orderService.Checkout(checkoutRequest)));
            }))
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
}
