namespace PagueVeloz.Application.DTOs.Requests.Account;

public record CaptureAccountRequest(Guid ReserveOperationId, string ReferenceId);
