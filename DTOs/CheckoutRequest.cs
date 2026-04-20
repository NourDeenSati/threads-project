using System.ComponentModel.DataAnnotations;

namespace FirstApi.DTOs;

public class CheckoutRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
