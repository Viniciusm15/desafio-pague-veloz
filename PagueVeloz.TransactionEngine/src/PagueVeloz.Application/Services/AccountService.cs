using PagueVeloz.Application.Exceptions;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AccountService(
        IAccountRepository accountRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Account> OpenAccountAsync(Guid customerId, decimal creditLimit = 0m)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId)
            ?? throw new NotFoundException(nameof(Customer), customerId);

        var account = Account.Open(customer.Id, creditLimit);
        await _accountRepository.AddAsync(account);

        return account;
    }

    public Task<Account?> GetByIdAsync(Guid accountId)
    {
        return _accountRepository.GetByIdAsync(accountId);
    }

    public async Task<Account> BlockAsync(Guid accountId)
    {
        var account = await GetAccountByIdAsync(accountId);

        account.Block();
        await _unitOfWork.SaveChangesAsync();

        return account;
    }

    public async Task<Account> ReactivateAsync(Guid accountId)
    {
        var account = await GetAccountByIdAsync(accountId);

        account.Activate();
        await _unitOfWork.SaveChangesAsync();

        return account;
    }

    public async Task<Account> DeactivateAsync(Guid accountId)
    {
        var account = await GetAccountByIdAsync(accountId);

        account.Deactivate();
        await _unitOfWork.SaveChangesAsync();

        return account;
    }

    public async Task<Account> CreditAsync(Guid accountId, decimal amount, string referenceId)
    {
        var account = await GetAccountByIdAsync(accountId);

        account.Credit(amount, referenceId);
        await _unitOfWork.SaveChangesAsync();

        return account;
    }

    public async Task<Account> DebitAsync(Guid accountId, decimal amount, string referenceId)
    {
        var account = await GetAccountByIdAsync(accountId);

        account.Debit(amount, referenceId);
        await _unitOfWork.SaveChangesAsync();

        return account;
    }

    public async Task<Account> ReserveAsync(Guid accountId, decimal amount, string referenceId)
    {
        var account = await GetAccountByIdAsync(accountId);

        account.Reserve(amount, referenceId);
        await _unitOfWork.SaveChangesAsync();

        return account;
    }

    public async Task<Account> CaptureAsync(Guid accountId, Guid reserveOperationId, string referenceId)
    {
        var account = await GetAccountByIdAsync(accountId);

        account.Capture(reserveOperationId, referenceId);
        await _unitOfWork.SaveChangesAsync();

        return account;
    }

    public async Task<Account> ReversalAsync(Guid accountId, Guid originalOperationId, string referenceId)
    {
        var account = await GetAccountByIdAsync(accountId);

        account.Reversal(originalOperationId, referenceId);
        await _unitOfWork.SaveChangesAsync();

        return account;
    }

    public async Task<(Account Source, Account Destination)> TransferAsync(
        Guid sourceAccountId, Guid destinationAccountId, decimal amount, string referenceId)
    {
        if (sourceAccountId == destinationAccountId)
            throw new ArgumentException("Source and destination accounts must be different.");

        var source = await GetAccountByIdAsync(sourceAccountId);
        var destination = await GetAccountByIdAsync(destinationAccountId);

        source.Debit(amount, referenceId);
        destination.Credit(amount, referenceId);

        await _unitOfWork.SaveChangesAsync();

        return (source, destination);
    }

    #region Private Methods

    private async Task<Account> GetAccountByIdAsync(Guid accountId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);
        return account ?? throw new NotFoundException(nameof(Account), accountId);
    }

    #endregion
}