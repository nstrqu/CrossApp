namespace Core.Domain;

public sealed class Warehouse
{
    private const int MaxIssuePerCustomer = 3;
    private readonly Dictionary<string, int> _issuesPerCustomer = new();

    public string Id { get; }

    private Warehouse(string id) => Id = id;

    public static Warehouse Create(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор складу обов'язковий", nameof(id));
        return new Warehouse(id);
    }

    // Інваріант охоплює ДВІ сутності: Product (залишок) і клієнта (ліміт видач).
    public void IssueToCustomer(Product product, string customerId, int amount)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Ідентифікатор клієнта обов'язковий", nameof(customerId));

        int alreadyIssued = _issuesPerCustomer.GetValueOrDefault(customerId, 0);
        if (alreadyIssued >= MaxIssuePerCustomer)
            throw new InvalidOperationException(
                $"Клієнт {customerId} вже отримав {alreadyIssued} видач — ліміт {MaxIssuePerCustomer} вичерпано");

        product.Issue(amount);
        _issuesPerCustomer[customerId] = alreadyIssued + 1;
    }
}