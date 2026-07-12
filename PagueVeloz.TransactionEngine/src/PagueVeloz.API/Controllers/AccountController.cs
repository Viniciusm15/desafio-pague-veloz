using MediatR;
using Microsoft.AspNetCore.Mvc;
using PagueVeloz.Application.Commands.Accounts;
using PagueVeloz.Application.Commands.Transactions;
using PagueVeloz.Application.DTOs.Accounts.Responses;
using PagueVeloz.Application.DTOs.Transactions.Requests;
using PagueVeloz.Application.DTOs.Transactions.Responses;
using PagueVeloz.Application.Queries.Accounts;
using PagueVeloz.Domain.Enums;

namespace PagueVeloz.API.Controllers;

/// <summary>
/// Handles account management and financial transactions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new account for an existing customer.
    /// </summary>
    /// <param name="command">Account creation command containing customer ID and credit limit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created account details.</returns>
    /// <response code="201">Account created successfully.</response>
    /// <response code="400">Validation error or invalid request.</response>
    /// <response code="404">Customer not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] OpenAccountCommand command, CancellationToken cancellationToken)
    {
        var account = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
    }

    /// <summary>
    /// Retrieves account details by its unique identifier.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The account details.</returns>
    /// <response code="200">Account found.</response>
    /// <response code="404">Account not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var account = await _mediator.Send(new GetAccountQuery(id), cancellationToken);
        if (account is null) return NotFound();
        return Ok(account);
    }

    /// <summary>
    /// Blocks an account, preventing any further operations (credit, debit, etc.).
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated account details.</returns>
    /// <response code="200">Account blocked successfully.</response>
    /// <response code="404">Account not found.</response>
    [HttpPost("{id}/block")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Block(Guid id, CancellationToken cancellationToken)
    {
        var account = await _mediator.Send(new BlockAccountCommand(id), cancellationToken);
        return Ok(account);
    }

    /// <summary>
    /// Reactivates a previously deactivated account.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated account details.</returns>
    /// <response code="200">Account reactivated successfully.</response>
    /// <response code="404">Account not found.</response>
    [HttpPost("{id}/reactivate")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken cancellationToken)
    {
        var account = await _mediator.Send(new ReactivateAccountCommand(id), cancellationToken);
        return Ok(account);
    }

    /// <summary>
    /// Deactivates an account, preventing operations until reactivated.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated account details.</returns>
    /// <response code="200">Account deactivated successfully.</response>
    /// <response code="404">Account not found.</response>
    [HttpPost("{id}/deactivate")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var account = await _mediator.Send(new DeactivateAccountCommand(id), cancellationToken);
        return Ok(account);
    }

    /// <summary>
    /// Executes a financial transaction (credit, debit, reserve, capture, reversal, or transfer).
    /// </summary>
    /// <param name="request">Transaction details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transaction result with updated balance.</returns>
    /// <response code="200">Transaction executed successfully.</response>
    /// <response code="400">Validation error or unsupported operation.</response>
    /// <response code="404">Account not found.</response>
    /// <response code="500">Internal server error.</response>
    /// <remarks>
    /// The following fields are required based on operation type:
    /// - Credit, Debit, Reserve: operation, accountId, amount, reference_id, currency
    /// - Capture: operation, accountId, amount, reference_id, currency, reserve_operation_id (required)
    /// - Reversal: operation, accountId, amount, reference_id, currency, original_operation_id (required)
    /// - Transfer: operation, accountId, amount, reference_id, currency, destination_account_id (required)
    /// </remarks>
    [HttpPost("transactions")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Execute([FromBody] TransactionRequest request, CancellationToken cancellationToken)
    {
        IRequest<TransactionResponse> command = request.Operation switch
        {
            OperationType.Credit =>
                new CreditCommand(request.AccountId, request.Amount, request.ReferenceId, request.Currency, request.Metadata),
            OperationType.Debit =>
                new DebitCommand(request.AccountId, request.Amount, request.ReferenceId, request.Currency, request.Metadata),
            OperationType.Reserve =>
                new ReserveCommand(request.AccountId, request.Amount, request.ReferenceId, request.Currency, request.Metadata),
            OperationType.Capture =>
                new CaptureCommand(request.AccountId, request.ReserveOperationId!.Value, request.ReferenceId, request.Currency, request.Metadata),
            OperationType.Reversal =>
                new ReversalCommand(request.AccountId, request.OriginalOperationId!.Value, request.ReferenceId, request.Currency, request.Metadata),
            OperationType.Transfer =>
                new TransferCommand(request.AccountId, request.DestinationAccountId!.Value, request.Amount, request.ReferenceId, request.Currency, request.Metadata),
            _ => throw new ArgumentException($"Unsupported operation: {request.Operation}")
        };

        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }
}
