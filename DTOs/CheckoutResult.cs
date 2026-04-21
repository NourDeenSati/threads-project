using FirstApi.Models;

namespace FirstApi.DTOs;

public class CheckoutResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? ErrorCode { get; init; }

    public int? RemainingStock { get; init; }

    public Order? Order { get; init; }

    public static CheckoutResult Ok(Order order, int remainingStock) =>
        new()
        {
            Success = true,
            Message = "Checkout completed successfully.",
            RemainingStock = remainingStock,
            Order = order
        };

    public static CheckoutResult Fail(string errorCode, string message, int? remainingStock = null) =>
        new()
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message,
            RemainingStock = remainingStock
        };
}
