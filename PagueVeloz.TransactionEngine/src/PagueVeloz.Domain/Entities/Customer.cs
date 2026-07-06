namespace PagueVeloz.Domain.Entities;

public class Customer
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Document { get; private set; }

    private Customer(string name, string document)
    {
        Id = Guid.NewGuid();
        Name = name;
        Document = document;
    }

    public static Customer Create(string name, string document)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome do cliente é obrigatório.");

        if (string.IsNullOrWhiteSpace(document))
            throw new ArgumentException("Documento do cliente é obrigatório.");

        return new Customer(name, document);
    }
}
