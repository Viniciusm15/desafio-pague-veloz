using FluentValidation;
using PagueVeloz.Application.DTOs.Requests.Customer;

namespace PagueVeloz.Application.Validators.Customer;

public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("name is required.");

        RuleFor(x => x.Document)
            .NotEmpty()
            .WithMessage("document is required.");
    }
}
