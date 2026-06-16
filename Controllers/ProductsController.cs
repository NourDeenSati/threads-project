using FirstApi.Models;
using FirstApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace FirstApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly InMemoryStore _store;
    private readonly IDistributedCache _cache;
    private readonly ApplicationDbContext _context;

    public ProductsController(InMemoryStore store, IDistributedCache cache, ApplicationDbContext context)
    {
        _store = store;
        _cache = cache;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _context.Products.ToListAsync());
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

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
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
