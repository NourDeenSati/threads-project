using FirstApi.Models;

namespace FirstApi.Services;

public class StoreService
{
    private readonly InMemoryStore _store;

    public StoreService(InMemoryStore store)
    {
        _store = store;
    }

    public IReadOnlyList<Product> GetAllProducts() => _store.Products;

    public Product? GetProductById(int id) =>
        _store.Products.FirstOrDefault(product => product.Id == id);

    public Product GetSingleProduct() => _store.GetSingleProduct();

    public void Reset() => _store.Reset();

    public int GetProductCount() => _store.Products.Count;
}
