namespace PagueVeloz.Application.DTOs.Requests.Account;

public record ReversalAccountRequest(Guid OriginalOperationId, string ReferenceId);
