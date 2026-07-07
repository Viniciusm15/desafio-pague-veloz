namespace PagueVeloz.Application.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string entityName, object id)
        : base($"Entity '{entityName}' with id '{id}' was not found.")
    {
    }
}
