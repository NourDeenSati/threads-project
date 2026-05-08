using System.Diagnostics;

namespace FirstApi.AOP;

public class PerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMiddleware> _logger;

    public PerformanceMiddleware(RequestDelegate next, ILogger<PerformanceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();
        var elapsed = sw.Elapsed.TotalMilliseconds;
        var path = context.Request.Path;
        _logger.LogInformation("Request {Path} took {Elapsed} ms", path, elapsed);
    }
}
