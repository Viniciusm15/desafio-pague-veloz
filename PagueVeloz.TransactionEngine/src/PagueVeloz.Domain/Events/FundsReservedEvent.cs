using PagueVeloz.Domain.Common;

namespace PagueVeloz.Domain.Events;

public record FundsReservedEvent(
    Guid AccountId,
    Guid OperationId,
    long Amount,
    string Currency,
    string ReferenceId
) : DomainEventBase;
