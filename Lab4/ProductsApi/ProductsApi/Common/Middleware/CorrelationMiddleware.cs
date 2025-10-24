namespace BooksApi.Common.Middleware;

public class CorrelationMiddleware
{
    private const string Header = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx)
    {
        if (!ctx.Request.Headers.TryGetValue(Header, out var id) || string.IsNullOrWhiteSpace(id))
            id = Guid.NewGuid().ToString("N")[..8];

        ctx.Response.Headers[Header] = id!;
        using (ctx.RequestServices.GetRequiredService<ILogger<CorrelationMiddleware>>()
                   .BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id!.ToString() }))
        {
            await _next(ctx);
        }
    }
}