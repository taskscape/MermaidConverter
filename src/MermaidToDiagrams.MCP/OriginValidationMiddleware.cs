namespace MermaidToDiagrams.MCP;

public sealed class OriginValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _allowedOrigins;

    public OriginValidationMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _allowedOrigins = configuration
            .GetSection("Mcp:AllowedOrigins")
            .Get<string[]>()?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().TrimEnd('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? [];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("Origin", out var originValues))
        {
            foreach (var origin in originValues)
            {
                var originText = origin?.Trim().TrimEnd('/') ?? "";
                if (!_allowedOrigins.Contains(originText))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        jsonrpc = "2.0",
                        error = new
                        {
                            code = -32000,
                            message = "Forbidden Origin header.",
                            data = new
                            {
                                origin = originText
                            }
                        }
                    });
                    return;
                }
            }
        }

        await _next(context);
    }
}
