using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PagueVeloz.Domain.Interfaces;
using PagueVeloz.Infrastructure.Persistence;
using PagueVeloz.Infrastructure.Persistence.Context;
using PagueVeloz.Infrastructure.Persistence.Repositories;

namespace PagueVeloz.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        services.AddHealthChecks()
            .AddNpgSql(
                connectionString!,
                name: "postgresql",
                tags: ["db", "ready"]);

        return services;
    }
}
