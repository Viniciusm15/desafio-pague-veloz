using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;

namespace PagueVeloz.Infrastructure.Observability.Logging;

public static class SerilogExtensions
{
    public static IHostBuilder UseCustomSerilog(this IHostBuilder builder, string serviceName)
    {
        return builder.UseSerilog((context, services, configuration) =>
        {
            var isDevelopment = context.HostingEnvironment.IsDevelopment();

            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("Service", serviceName);

            if (isDevelopment)
                configuration.WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
            else
                configuration.WriteTo.Console(new CompactJsonFormatter());
        });
    }
}
