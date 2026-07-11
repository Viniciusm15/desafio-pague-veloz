namespace PagueVeloz.Infrastructure.Observability;

public class CorrelationIdAccessor : ICorrelationIdProvider
{
    private static readonly AsyncLocal<string?> _current = new();

    public string? CorrelationId => _current.Value;

    public void SetCorrelationId(string correlationId)
    {
        _current.Value = correlationId;
    }
}

