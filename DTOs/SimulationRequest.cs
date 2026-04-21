using System.ComponentModel.DataAnnotations;

namespace FirstApi.DTOs;

public class SimulationRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int QuantityPerRequest { get; set; }

    [Range(1, 500)]
    public int NumberOfRequests { get; set; }

    [Range(1, 100)]
    public int? MaxConcurrency { get; set; }
}
