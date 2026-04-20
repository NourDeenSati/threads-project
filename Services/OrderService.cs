using FirstApi.DTOs;
using FirstApi.Models;

namespace FirstApi.Services;

public class OrderService
{
    private readonly InMemoryStore _store;

    public OrderService(InMemoryStore store)
    {
        _store = store;
    }

    public CheckoutResult Checkout(CheckoutRequest request)
    {
        if (request is null)
        {
            return CheckoutResult.Fail("invalid_request", "Request body is required.");
        }

        if (request.ProductId <= 0)
        {
            return CheckoutResult.Fail("invalid_product_id", "ProductId must be greater than 0.");
        }

        if (request.Quantity <= 0)
        {
            return CheckoutResult.Fail("invalid_quantity", "Quantity must be greater than 0.");
        }

        // Products, stock values, order IDs, and the Orders list are shared resources.
        // We protect the whole checkout critical section with one lock to avoid race conditions and lost updates.
        lock (_store.CheckoutLock)
        {
            var product = _store.Products.FirstOrDefault(p => p.Id == request.ProductId);

            if (product is null)
            {
                return CheckoutResult.Fail("product_not_found", "Product was not found.");
            }

            if (product.StockQuantity < request.Quantity)
            {
                return CheckoutResult.Fail(
                    "insufficient_stock",
                    "Not enough stock is available.",
                    product.StockQuantity);
            }

            // Keep the protected section small and synchronous.
            // This lock protects the full logical checkout operation, not just one line.
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
