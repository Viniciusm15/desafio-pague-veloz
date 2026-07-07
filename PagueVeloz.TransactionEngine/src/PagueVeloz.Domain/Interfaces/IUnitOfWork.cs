namespace PagueVeloz.Domain.Interfaces;

public interface IUnitOfWork
{
    Task SaveChangesAsync();
}
