using FirstApi.DTOs;
using FirstApi.Models;
using FirstApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FirstApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestingController : ControllerBase
{
    private readonly CapacityControlService _capacityControlService;
    private readonly InMemoryStore _store;
    private readonly OrderService _orderService;
    private readonly LoadBalancerService _loadBalancerService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TestingController> _logger;

    public TestingController(
        CapacityControlService capacityControlService,
        InMemoryStore store,
        OrderService orderService,
        LoadBalancerService loadBalancerService,
        ApplicationDbContext context,
        ILogger<TestingController> logger)
    {
        _capacityControlService = capacityControlService;
        _store = store;
        _orderService = orderService;
        _loadBalancerService = loadBalancerService;
        _context = context;
        _logger = logger;
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
        });
    }

    [HttpPost("resolve-rece-condition")]
    public async Task<IActionResult> ResolveRaceCondition([FromBody] SimulationRequest request)
    {
        var tasks = Enumerable.Range(1, request.NumberOfRequests).Select(async _ => Task.Run(() =>
        {
            return _orderService.Checkout(new CheckoutRequest
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
        });
    }

    [HttpPost("simulate-concurrent-checkouts")]
    public async Task<IActionResult> SimulateConcurrentCheckouts([FromBody] SimulationRequest request)
    {
        if (request is null)
            return BadRequest(new { message = "Request body is required." });
        if (request.ProductId <= 0)
            return BadRequest(new { message = "ProductId must be greater than 0." });
        if (request.QuantityPerRequest <= 0)
            return BadRequest(new { message = "QuantityPerRequest must be greater than 0." });
        if (request.NumberOfRequests <= 0)
            return BadRequest(new { message = "NumberOfRequests must be greater than 0." });
        var tasks = Enumerable.Range(1, request.NumberOfRequests)
            .Select(async _ =>
            {
                var checkoutRequest = new CheckoutRequest
                {
                    ProductId = request.ProductId,
                    Quantity = request.QuantityPerRequest
                };
                return await _orderService.Checkout(checkoutRequest);
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
        });
    }

    [HttpPost("resolve-concurrent-checkouts")]
    public async Task<IActionResult> ResloveConcurrentCheckouts([FromBody] SimulationRequest request)
    {
        if (request is null)
            return BadRequest(new { message = "Request body is required." });
        if (request.ProductId <= 0)
            return BadRequest(new { message = "ProductId must be greater than 0." });
        if (request.QuantityPerRequest <= 0)
            return BadRequest(new { message = "QuantityPerRequest must be greater than 0." });
        if (request.NumberOfRequests <= 0)
            return BadRequest(new { message = "NumberOfRequests must be greater than 0." });
        var tasks = Enumerable.Range(1, request.NumberOfRequests)
            .Select(async _ =>
            {
                return await _capacityControlService.RunAsync(async () =>
                {
                    var checkoutRequest = new CheckoutRequest
                    {
                        ProductId = request.ProductId,
                        Quantity = request.QuantityPerRequest
                    };
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
        });
    }

    [HttpPost("safe-checkout")]
    public IActionResult SafeCheckout([FromBody] CheckoutRequest request)
    {
        Console.WriteLine($"[Node {Request.Host}] Received request for product {request.ProductId}");
        lock (_store.GetLockForProduct(request.ProductId))
        {
            var product = _store.Products.FirstOrDefault(p => p.Id == request.ProductId);
            if (product!.StockQuantity >= request.Quantity)
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

    [HttpPost("simulate-load-balancing")]
    public async Task<IActionResult> SimulateLoadBalancing([FromBody] SimulationRequest request)
    {
        var client = new HttpClient();
        var results = new List<string>();
        for (int i = 1; i <= request.NumberOfRequests; i++)
        {
            string targetServer = _loadBalancerService.GetNextServer();
            string fullUrl = $"{targetServer}/api/Testing/after-optimistic-checkout";
            try
            {
                var checkoutData = new{ProductId = request.ProductId, Quantity = request.QuantityPerRequest};
                var response = await client.PostAsJsonAsync(fullUrl, checkoutData);
                string responseContent = await response.Content.ReadAsStringAsync();
                string log = $"Task {i} -> Sent to {fullUrl} | Status: {response.StatusCode} | Body: {responseContent}";
                results.Add(log);
                Console.WriteLine(log);
            }
            catch (Exception ex)
            {
                string errorLog = $"Task {i} -> Failed to send to {fullUrl} | Error: {ex.Message}";
                results.Add(errorLog);
                Console.WriteLine(errorLog);
            }
        }
        return Ok(new { distribution = results });
    }

    [HttpPost("before-optimistic-checkout")]
    public async Task<IActionResult> CheckoutOrderBefore([FromBody] CheckoutRequest request)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId);
        if (product == null) return NotFound("Product not found");

        if (product.StockQuantity < request.Quantity) return BadRequest(new { Message = "Insufficient stock" }); ;

        product.StockQuantity -= request.Quantity;
        var order = new Order
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = request.Quantity,
            UnitPrice = product.Price,
            TotalPrice = product.Price * request.Quantity,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Orders.Add(order);

        await _context.SaveChangesAsync();
        return Ok(new { product.StockQuantity, Message = "Successful checkout" });
    }

    [HttpPost("after-optimistic-checkout")]
    public async Task<IActionResult> CheckoutOrderAfter([FromBody] CheckoutRequest request)
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId);
            if (product == null) return NotFound("Product not found");

            if (product.StockQuantity < request.Quantity) return BadRequest(new { Message = "Insufficient stock" }); ;

            product.StockQuantity -= request.Quantity;
            product.Version += 1;
            var order = new Order
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = request.Quantity,
                UnitPrice = product.Price,
                TotalPrice = product.Price * request.Quantity,
                CreatedAtUtc = DateTime.UtcNow
            };
            _context.Orders.Add(order);

            await _context.SaveChangesAsync();
            return Ok(new { product.StockQuantity, Message = "Successful checkout" });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Conflict: Please retry your request.");
        }
    }

    [HttpPost("checkout-acid")]
    public async Task<IActionResult> CheckoutWithACID([FromBody] CheckoutRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null) return NotFound("Product not found");
            if (product.StockQuantity < request.Quantity) return BadRequest("Insufficient stock");
            product.StockQuantity -= request.Quantity;
            product.Version += 1;
            var order = new Order
            {
                Id = _store.GetNextOrderId(),
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = request.Quantity,
                UnitPrice = product.Price,
                TotalPrice = product.Price * request.Quantity,
                CreatedAtUtc = DateTime.UtcNow
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok(new { Message = "Order created and stock updated successfully" });
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict detected for ProductId: {ProductId} at {Time}", request.ProductId, DateTime.UtcNow);
            await transaction.RollbackAsync();
            return Conflict("Conflict: Please retry your request.");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "An internal error occurred.");
        }
    }
}