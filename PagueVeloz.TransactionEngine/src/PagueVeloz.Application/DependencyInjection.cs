using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Application.Services;
using PagueVeloz.Application.Validators.Account;

namespace PagueVeloz.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddValidatorsFromAssemblyContaining<TransactionRequestValidator>();

        return services;
    }
}
