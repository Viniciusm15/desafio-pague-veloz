namespace PagueVeloz.Domain.Entities;

public class Account
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal AvailableBalance { get; private set; }

    private Account(Guid customerId)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        AvailableBalance = 0m;
    }

    public static Account Open(Guid customerId)
    {
        return new Account(customerId);
    }
}
