using FirstApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FirstApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly InMemoryStore _store;

    public ProductsController(InMemoryStore store)
    {
        _store = store;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_store.Products);
    }

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var product = _store.Products.FirstOrDefault(p => p.Id == id);

        if (product is null)
        {
            return NotFound(new { message = "Product was not found." });
        }

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
