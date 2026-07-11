using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Common;
using PagueVeloz.Domain.Interfaces;
using PagueVeloz.Infrastructure.Persistence.Context;

namespace PagueVeloz.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            EntityEntry? entry = ex.Entries.FirstOrDefault();
            var entityName = entry?.Entity.GetType().Name ?? "Entity";
            var entityId = entry?.Entity is Entity trackedEntity ? (object)trackedEntity.Id : "unknown";

            throw new ConcurrencyConflictException(entityName, entityId);
        }
    }
}
