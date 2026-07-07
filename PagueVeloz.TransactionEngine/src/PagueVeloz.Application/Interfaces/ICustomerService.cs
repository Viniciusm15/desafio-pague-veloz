using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Application.Interfaces;

public interface ICustomerService
{
    Task<Customer> CreateAsync(string name, string document);
    Task<Customer?> GetByIdAsync(Guid customerId);
}
