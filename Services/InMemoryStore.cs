using FirstApi.Models;

namespace FirstApi.Services;

public class InMemoryStore
{
    private int _nextOrderId = 1;

    public object CheckoutLock { get; } = new();

    public List<Product> Products { get; } = [];

    public List<Order> Orders { get; } = [];

    public InMemoryStore()
    {
        Reset();
    }

    // Reset products and orders so testing starts from a known state.
    public void Reset()
    {
        lock (CheckoutLock)
        {
            Products.Clear();
            Orders.Clear();
            _nextOrderId = 1;

            Products.AddRange(
            [
                new Product { Id = 1, Name = "Laptop", Price = 1200.00m, StockQuantity = 10 },
                new Product { Id = 2, Name = "Headphones", Price = 150.00m, StockQuantity = 25 },
                new Product { Id = 3, Name = "Mechanical Keyboard", Price = 90.00m, StockQuantity = 15 }
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
