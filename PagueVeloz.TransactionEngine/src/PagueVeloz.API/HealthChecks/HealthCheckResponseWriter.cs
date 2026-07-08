using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PagueVeloz.API.HealthChecks;

public static class HealthCheckResponseWriter
{
    public static async Task<object> BuildResponse(HttpContext context, HealthReport report)
    {
        return await Task.FromResult(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            environment = context.RequestServices.GetRequiredService<IHostEnvironment>().EnvironmentName,
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                durationMs = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                error = e.Value.Exception?.Message
            })
        });
    }
}
