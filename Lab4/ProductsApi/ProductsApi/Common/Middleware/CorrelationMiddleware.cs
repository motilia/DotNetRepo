using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ProductsApi.Common.Middleware;

public class CorrelationMiddleware
{
    private const string CorrelationHeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationMiddleware> _logger;

    public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(CorrelationHeaderName, out var cid) ||
            string.IsNullOrWhiteSpace(cid))
        {
            cid = Guid.NewGuid().ToString("N")[..8];
            context.Request.Headers[CorrelationHeaderName] = cid;
        }

        context.Response.Headers[CorrelationHeaderName] = cid!;

        using (_logger.BeginScope(new Dictionary<string, object?>
               {
                   ["CorrelationId"] = cid.ToString()
               }))
        {
            await _next(context);
        }
    }
}
