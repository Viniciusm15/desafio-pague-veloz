namespace PagueVeloz.Application.DTOs.Requests.Account;

public record CreateAccountRequest(Guid CustomerId, decimal CreditLimit = 0m);
