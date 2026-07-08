using Microsoft.EntityFrameworkCore;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;
using PagueVeloz.Infrastructure.Persistence.Context;

namespace PagueVeloz.Infrastructure.Persistence.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly AppDbContext _context;

    public AccountRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByIdAsync(Guid accountId)
    {
        return await _context.Accounts
            .Include(a => a.Operations.OrderByDescending(o => o.OccurredAt))
            .FirstOrDefaultAsync(a => a.Id == accountId);
    }

    public async Task AddAsync(Account account)
    {
        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AccountOperation>> GetOperationsByReferenceIdAsync(string referenceId)
    {
        return await _context.AccountOperations
            .Where(o => o.ReferenceId == referenceId)
            .ToListAsync();
    }
}
