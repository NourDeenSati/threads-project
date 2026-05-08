using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace FirstApi.AOP;

public class PerformanceActionFilter : IAsyncActionFilter
{
    private readonly ILogger<PerformanceActionFilter> _logger;

    public PerformanceActionFilter(ILogger<PerformanceActionFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
        var name = controllerActionDescriptor != null
            ? $"{controllerActionDescriptor.ControllerName}.{controllerActionDescriptor.ActionName}"
            : context.ActionDescriptor.DisplayName ?? "UnknownAction";

        var sw = Stopwatch.StartNew();
        var executed = await next();
        sw.Stop();

        var elapsedMs = sw.Elapsed.TotalMilliseconds;
        _logger.LogInformation("Performance: {Action} took {Elapsed} ms", name, elapsedMs);
    }
}
