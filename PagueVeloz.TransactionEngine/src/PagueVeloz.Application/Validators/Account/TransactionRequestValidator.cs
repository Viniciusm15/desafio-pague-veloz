using FluentValidation;
using PagueVeloz.Application.DTOs.Requests.Account;
using PagueVeloz.Domain.Enums;

namespace PagueVeloz.Application.Validators.Account;

/// <summary>
/// Validates the unified transaction envelope (POST /api/account/transactions) before
/// it is translated into the operation-specific request used by AccountService.
/// This is what protects the "!.Value" null-forgiving reads in the controller/executor
/// switch (ReserveOperationId, OriginalOperationId, DestinationAccountId) from ever
/// running against a null value.
/// </summary>
public class TransactionRequestValidator : AbstractValidator<TransactionRequest>
{
    public TransactionRequestValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("account_id is required.");

        RuleFor(x => x.ReferenceId)
            .NotEmpty()
            .WithMessage("reference_id is required.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .WithMessage("currency must be a valid 3-letter ISO 4217 code.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .When(x => x.Operation is OperationType.Credit
                                    or OperationType.Debit
                                    or OperationType.Reserve
                                    or OperationType.Transfer)
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

        RuleFor(x => x)
            .Must(x => x.DestinationAccountId != x.AccountId)
            .When(x => x.Operation == OperationType.Transfer && x.DestinationAccountId.HasValue)
            .WithMessage("source and destination accounts must be different.")
            .WithName("destination_account_id");
    }
}
