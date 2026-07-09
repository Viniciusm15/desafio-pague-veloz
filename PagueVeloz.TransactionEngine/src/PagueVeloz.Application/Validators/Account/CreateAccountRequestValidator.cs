using FluentValidation;
using PagueVeloz.Application.DTOs.Requests.Account;

namespace PagueVeloz.Application.Validators.Account;

public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("customer_id is required.");

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0)
            .WithMessage("credit_limit cannot be negative.");
    }
}
