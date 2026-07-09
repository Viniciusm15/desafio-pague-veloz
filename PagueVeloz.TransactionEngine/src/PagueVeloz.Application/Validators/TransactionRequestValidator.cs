using FluentValidation;
using PagueVeloz.Application.DTOs.Requests.Account;
using PagueVeloz.Domain.Enums;

namespace PagueVeloz.Application.Validators;

public class TransactionRequestValidator : AbstractValidator<TransactionRequest>
{
    public TransactionRequestValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.ReferenceId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3);

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .When(x => x.Operation is OperationType.Credit or OperationType.Debit or OperationType.Reserve or OperationType.Transfer)
            .WithMessage("amount must be greater than zero for this operation.");

        RuleFor(x => x.ReserveOperationId)
            .NotNull()
            .When(x => x.Operation == OperationType.Capture)
            .WithMessage("reserve_operation_id is required for capture operations.");

        RuleFor(x => x.OriginalOperationId)
            .NotNull()
            .When(x => x.Operation == OperationType.Reversal)
            .WithMessage("original_operation_id is required for reversal operations.");

        RuleFor(x => x.DestinationAccountId)
            .NotNull()
            .When(x => x.Operation == OperationType.Transfer)
            .WithMessage("destination_account_id is required for transfer operations.");
    }
}
