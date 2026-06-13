using System.ComponentModel.DataAnnotations;

namespace FirstApi.Models;

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    [ConcurrencyCheck]
    public int Version { get; set; } = 1;
}
