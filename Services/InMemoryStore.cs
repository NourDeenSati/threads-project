using FirstApi.Models;

namespace FirstApi.Services;

public class InMemoryStore
{
    private int _nextOrderId = 1;
    private readonly List<Product> _products = [];

    public object CheckoutLock { get; } = new();

    public IReadOnlyList<Product> Products => _products;

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
            _products.Clear();
            Orders.Clear();
            _nextOrderId = 1;

            _products.Add(new Product
            {
                Id = 1,
                Name = "Test Product",
                Price = 100.00m,
                StockQuantity = 10
            });
        }
    }

    public Product GetSingleProduct()
    {
        lock (CheckoutLock)
        {
            return _products.Single();
        }
    }

    // Keep ID generation simple. Call this only inside the checkout lock.
    public int GetNextOrderId()
    {
        var nextId = _nextOrderId;
        _nextOrderId++;
        return nextId;
    }

    // Intentionally unsafe for race-condition demonstrations.
    public int GetNextOrderIdUnsafe()
    {
        var nextId = _nextOrderId;
        _nextOrderId++;
        return nextId;
    }
}
