using PagueVeloz.Domain.Common;

namespace PagueVeloz.Domain.Events;

public record OperationReversedEvent(
    Guid AccountId,
    Guid OperationId,
    Guid OriginalOperationId,
    long Amount,
    string Currency,
    string ReferenceId
) : DomainEventBase;
