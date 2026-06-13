using FirstApi.Models;
using FirstApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace FirstApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly InMemoryStore _store;
    private readonly IDistributedCache _cache;

    public ProductsController(InMemoryStore store, IDistributedCache cache)
    {
        _store = store;
        _cache = cache;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_store.Products);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        string cacheKey = $"product_{id}";
        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (cachedData != null)
        {
            return Ok(JsonSerializer.Deserialize<Product>(cachedData));
        }

        var product = _store.Products.FirstOrDefault(p => p.Id == id);
        if(product is null)
        {
            return NotFound(new { message = "Product was not found." });
        }

        var options = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10));
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(product), options);

        return Ok(product);
    }

    [HttpPost("reset")]
    public IActionResult Reset()
    {
        _store.Reset();

        return Ok(new
        {
            message = "Products and orders were reset to the seeded in-memory data.",
            productCount = _store.Products.Count
        });
    }
}
