using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Templates;

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
                configuration.WriteTo.Console(new ExpressionTemplate(
                    "[{@t:HH:mm:ss} {@l:u3}] {@m}{#if CorrelationId is not null} | CorrelationId: {CorrelationId}{#end}\n{@x}"));
            else
                configuration.WriteTo.Console(new CompactJsonFormatter());
        });
    }
}
