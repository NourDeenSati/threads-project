using FirstApi.DTOs;
using FirstApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FirstApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly ThreadLimiter _threadLimiter;

    public OrdersController(
        ThreadLimiter threadLimiter,
        OrderService orderService)
    {
        _orderService = orderService;
        _threadLimiter = threadLimiter;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        // The default checkout endpoint also uses the limiter so the API can avoid too many
        // requests entering the protected checkout path at the same time.
        var result = await _threadLimiter.RunAsync(
            "orders checkout",
            () => Task.FromResult(_orderService.Checkout(request)));

        if (!result.Success)
        {
            if (result.ErrorCode == "product_not_found")
            {
                return NotFound(result);
            }

            return BadRequest(result);
        }

        return Ok(result);
    }
}
