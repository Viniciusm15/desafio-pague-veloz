using FluentValidation;
using PagueVeloz.Application.DTOs.Requests.Account;

namespace PagueVeloz.Application.Validators.Account;

public class DebitAccountRequestValidator : AbstractValidator<DebitAccountRequest>
{
    public DebitAccountRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("amount must be greater than zero.");

        RuleFor(x => x.ReferenceId)
            .NotEmpty()
            .WithMessage("reference_id is required.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .WithMessage("currency must be a valid 3-letter ISO 4217 code.");
    }
}
