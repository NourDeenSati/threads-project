namespace FirstApi.DTOs;

public class SimulationMetricsResponse
{
    public string Scenario { get; init; } = string.Empty;

    public int TotalRequests { get; init; }

    public int SuccessCount { get; init; }

    public int FailureCount { get; init; }

    public int UniqueThreadCount { get; init; }

    public List<int> Threads { get; init; } = [];

    public int InitialStock { get; init; }

    public int FinalStock { get; init; }

    public bool OversellingOccurred { get; init; }
}
