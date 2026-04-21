using FirstApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FirstApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly StoreService _storeService;

    public ProductsController(StoreService storeService)
    {
        _storeService = storeService;
    }

    [HttpGet]
    public IActionResult GetAll() => Ok(_storeService.GetAllProducts());

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var product = _storeService.GetProductById(id);

        if (product is null)
        {
            return NotFound(new { message = "Product was not found." });
        }

        return Ok(product);
    }

    [HttpPost("reset")]
    public IActionResult Reset()
    {
        _storeService.Reset();

        return Ok(new
        {
            message = "successfully",
            productCount = _storeService.GetProductCount()
        });
    }
}
