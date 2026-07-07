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
            .Include(a => a.Operations)
            .FirstOrDefaultAsync(a => a.Id == accountId);
    }

    public async Task AddAsync(Account account)
    {
        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Account account)
    {
        _context.Entry(account).State = EntityState.Modified;

        foreach (var operation in account.Operations)
        {
            _context.Entry(operation).State = EntityState.Unchanged;
        }

        await _context.SaveChangesAsync();
    }
}
