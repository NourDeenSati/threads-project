using FirstApi.DTOs;
using FirstApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FirstApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly CapacityControlService _capacityControlService;
    private readonly OrderService _orderService;

    public OrdersController(
        CapacityControlService capacityControlService,
        OrderService orderService)
    {
        _capacityControlService = capacityControlService;
        _orderService = orderService;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        // Requirement 2: pass checkout through capacity control before running the business logic.
        var result = await _capacityControlService.RunAsync(
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
