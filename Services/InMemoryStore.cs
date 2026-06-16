using FirstApi.Models;
using System.Collections.Concurrent;

namespace FirstApi.Services;

public class InMemoryStore
{
    private int _nextOrderId = 1;

    private readonly ConcurrentDictionary<int, object> _productLocks = new();

    public object GetLockForProduct(int productId) =>
        _productLocks.GetOrAdd(productId, _ => new object());

    public List<Product> Products { get; } = [];

    public List<Order> Orders { get; } = [];

    public InMemoryStore()
    {
        Reset();
    }

    // Reset products and orders so testing starts from a known state.
    public void Reset()
    {
        lock (_productLocks)
        {
            Products.Clear();
            Orders.Clear();
            _nextOrderId = 1;

            Products.AddRange(
            [
                new Product { Id = 1, Name = "Laptop", Price = 1200.00m, StockQuantity = 100 },
                new Product { Id = 2, Name = "Headphones", Price = 150.00m, StockQuantity = 25 },
                new Product { Id = 3, Name = "Mechanical Keyboard", Price = 90.00m, StockQuantity = 15 },
                new Product { Id = 4, Name = "Gaming Mouse", Price = 70.00m, StockQuantity = 20 }
            ]);
        }
    }

    // Keep ID generation simple. Call this only inside the checkout lock.
    public int GetNextOrderId()
    {
        var nextId = _nextOrderId;
        _nextOrderId++;
        return nextId;
    }
}
