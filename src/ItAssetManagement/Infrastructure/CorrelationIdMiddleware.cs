using Serilog.Context;

namespace ItAssetManagement.Infrastructure;

/// <summary>
/// Stamps every request with an id that ties together the log lines it produced.
/// <para>
/// The id is pushed into Serilog's <see cref="LogContext"/>, so every log line written
/// while handling the request carries it without anything having to pass it around;
/// returned on the response header so a proxy or a caller can record it; and shown on the
/// error page, so a user reporting "it broke" can quote a string that finds the exact
/// stack trace in the log file. An inbound id is honoured rather than replaced, which is
/// what lets a trace survive across a reverse proxy or a calling service.
/// </para>
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";

    public const string ItemsKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Items[ItemsKey] = correlationId;

        // Set on the way out rather than after the response has begun, which would throw.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(ItemsKey, correlationId))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var inbound = context.Request.Headers[HeaderName].FirstOrDefault();

        // Length-capped: the value is echoed into a response header and rendered on the
        // error page, so an unbounded caller-supplied string is not passed through.
        if (!string.IsNullOrWhiteSpace(inbound) && inbound.Length <= 64)
        {
            return inbound;
        }

        // TraceIdentifier is already unique per request under Kestrel and is cheaper than
        // minting a GUID, but a GUID is used when it is somehow unavailable.
        return string.IsNullOrEmpty(context.TraceIdentifier)
            ? Guid.NewGuid().ToString("N")
            : context.TraceIdentifier;
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();

    /// <summary>Reads the current request's correlation id, for views and controllers.</summary>
    public static string? GetCorrelationId(this HttpContext context) =>
        context.Items.TryGetValue(CorrelationIdMiddleware.ItemsKey, out var value) ? value as string : null;
}
