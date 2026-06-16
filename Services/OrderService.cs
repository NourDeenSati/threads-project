using FirstApi.DTOs;
using FirstApi.Models;

namespace FirstApi.Services;

public class OrderService
{
    private readonly InMemoryStore _store;
    private readonly BackgroundTaskQueue _taskQueue;

    public OrderService(InMemoryStore store, BackgroundTaskQueue backgroundService)
    {
        _store = store;
        _taskQueue = backgroundService;
    }

    public async Task<CheckoutResult> UnsafeCheckout(CheckoutRequest request)
    {
        var product = _store.Products.FirstOrDefault(p => p.Id == request.ProductId);

        if (product is null || product.StockQuantity < request.Quantity)
        {
            return CheckoutResult.Fail("failed", "No stock");
        }

        Thread.Sleep(50);

        product.StockQuantity -= request.Quantity;

        var order = new Order {};
        _store.Orders.Add(order);

        return CheckoutResult.Ok(order, product.StockQuantity);
    }

    public async Task<CheckoutResult> Checkout(CheckoutRequest request)
    {
        if (request is null)
            return CheckoutResult.Fail("invalid_request", "Request body is required.");
        if (request.ProductId <= 0)
            return CheckoutResult.Fail("invalid_product_id", "ProductId must be greater than 0.");
        if (request.Quantity <= 0)
            return CheckoutResult.Fail("invalid_quantity", "Quantity must be greater than 0.");
        await Task.Delay(3000);
        lock (_store.GetLockForProduct(request.ProductId))
        {
            var product = _store.Products.FirstOrDefault(p => p.Id == request.ProductId);

            if (product is null)
                return CheckoutResult.Fail("product_not_found", "Product was not found.");
            if (product.StockQuantity < request.Quantity)
                return CheckoutResult.Fail(
                    "insufficient_stock",
                    "Not enough stock is available.",
                    product.StockQuantity);
            product.StockQuantity -= request.Quantity;
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
            _store.Orders.Add(order);
            return CheckoutResult.Ok(order, product.StockQuantity);
        }
    }

    public async Task<CheckoutResult> CheckoutAsynchronous(CheckoutRequest request)
    {
        if (request is null)
            return CheckoutResult.Fail("invalid_request", "Request body is required.");
        if (request.ProductId <= 0)
            return CheckoutResult.Fail("invalid_product_id", "ProductId must be greater than 0.");
        if (request.Quantity <= 0)
            return CheckoutResult.Fail("invalid_quantity", "Quantity must be greater than 0.");
        lock (_store.GetLockForProduct(request.ProductId))
        {
            var product = _store.Products.FirstOrDefault(p => p.Id == request.ProductId);
            if (product is null)
                return CheckoutResult.Fail("product_not_found", "Product was not found.");
            if (product.StockQuantity < request.Quantity)
                return CheckoutResult.Fail(
                    "insufficient_stock",
                    "Not enough stock is available.",
                    product.StockQuantity);
            product.StockQuantity -= request.Quantity;
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
            _store.Orders.Add(order);
            _ = _taskQueue.QueueOrderAsync(order.Id);
            return CheckoutResult.Ok(order, product.StockQuantity);
        }
    }
}

public class CheckoutResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? ErrorCode { get; init; }

    public int? RemainingStock { get; init; }

    public Order? Order { get; init; }

    public static CheckoutResult Ok(Order order, int remainingStock)
    {
        return new CheckoutResult
        {
            Success = true,
            Message = "Checkout completed successfully.",
            RemainingStock = remainingStock,
            Order = order
        };
    }

    public static CheckoutResult Fail(string errorCode, string message, int? remainingStock = null)
    {
        return new CheckoutResult
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message,
            RemainingStock = remainingStock
        };
    }
}
