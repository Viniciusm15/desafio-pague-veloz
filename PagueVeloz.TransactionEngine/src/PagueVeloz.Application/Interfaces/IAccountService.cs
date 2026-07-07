using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Application.Interfaces;

public interface IAccountService
{
    Task<Account> OpenAccountAsync(Guid customerId);
    Task<Account?> GetByIdAsync(Guid accountId);
}
