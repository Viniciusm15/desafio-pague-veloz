namespace PagueVeloz.Application.DTOs.Requests.Account;

public record TransferAccountRequest(Guid SourceAccountId, Guid DestinationAccountId, decimal Amount, string ReferenceId);
