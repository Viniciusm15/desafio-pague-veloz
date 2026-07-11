using Serilog.Context;

namespace PagueVeloz.API.Middlewares;

public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        // Todo log emitido durante essa request (incluindo os do ExceptionHandlingMiddleware
        // e o log de request/response do Serilog) carrega essa propriedade automaticamente.
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
