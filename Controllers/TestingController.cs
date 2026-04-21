using FirstApi.DTOs;
using FirstApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FirstApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestingController : ControllerBase
{
    private readonly SimulationService _simulationService;

    public TestingController(SimulationService simulationService)
    {
        _simulationService = simulationService;
    }

    [HttpPost("simulate-concurrent-checkouts")]
    public async Task<IActionResult> SimulateConcurrentCheckouts([FromBody] SimulationRequest request)
    {
        return await RunSimulationAsync(request, _simulationService.RunConcurrentAsync, validateRequest: true);
    }

    [HttpPost("simulate-sequential-checkouts")]
    public async Task<IActionResult> SimulateSequentialCheckouts([FromBody] SimulationRequest request)
    {
        return await RunSimulationAsync(request, _simulationService.RunSequentialAsync);
    }

    [HttpPost("simulate-race-condition")]
    public async Task<IActionResult> SimulateRaceCondition([FromBody] SimulationRequest request)
    {
        return await RunSimulationAsync(request, _simulationService.RunRaceConditionAsync);
    }

    private async Task<IActionResult> RunSimulationAsync(
        SimulationRequest? request,
        Func<SimulationRequest, Task<SimulationMetricsResponse>> simulationRunner,
        bool validateRequest = false)
    {
        if (validateRequest)
        {
            var validationError = ValidateSimulationRequest(request);
            if (validationError is not null)
            {
                return validationError;
            }
        }

        return Ok(await simulationRunner(NormalizeSimulationRequest(request)));
    }

    private IActionResult? ValidateSimulationRequest(SimulationRequest? request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        if (request.ProductId <= 0)
        {
            return BadRequest(new { message = "ProductId must be greater than 0." });
        }

        if (request.QuantityPerRequest <= 0)
        {
            return BadRequest(new { message = "QuantityPerRequest must be greater than 0." });
        }

        if (request.NumberOfRequests <= 0)
        {
            return BadRequest(new { message = "NumberOfRequests must be greater than 0." });
        }

        if (request.MaxConcurrency is <= 0)
        {
            return BadRequest(new { message = "MaxConcurrency must be greater than 0." });
        }

        return null;
    }

    private static SimulationRequest NormalizeSimulationRequest(SimulationRequest? request) =>
        new()
        {
            ProductId = Math.Max(1, request?.ProductId ?? 1),
            QuantityPerRequest = Math.Max(1, request?.QuantityPerRequest ?? 1),
            NumberOfRequests = Math.Max(1, request?.NumberOfRequests ?? 1),
            MaxConcurrency = request?.MaxConcurrency is > 0 ? request.MaxConcurrency : null
        };
}
