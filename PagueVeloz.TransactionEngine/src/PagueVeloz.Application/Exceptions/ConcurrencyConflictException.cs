namespace PagueVeloz.Application.Exceptions;

public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string entityName, object id)
        : base($"Entity '{entityName}' with id '{id}' was modified by another operation. Please retry.")
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
