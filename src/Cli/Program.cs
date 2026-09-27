using Core.Domain;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("=== Сценарій 1: успіх ===");
Product product = Product.Create("P-001", "sku-001", "Цемент М400 25кг", "шт", 100);
Console.WriteLine(product);
product.RegisterArrival(50);
product.Issue(30);
Console.WriteLine(product);

Console.WriteLine();
Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");
TryDo("видача більша за залишок", () => product.Issue(1000));
TryDo("порожній SKU", () => Product.Create("P-002", " ", "Пісок", "т", 10));
TryDo("від'ємний залишок", () => Product.Create("P-003", "SKU-003", "Цегла", "шт", -5));

Console.WriteLine();
Console.WriteLine("=== Сценарій 3: перехід станів (додаткове завдання) ===");
product.ChangeStatus(ProductStatus.OutOfStock);
Console.WriteLine($"Новий статус: {product.Status}");
TryDo("заборонений перехід зі знятого з обліку", () =>
{
    Product discontinued = Product.Create("P-999", "SKU-999", "Старий товар", "шт", 0);
    discontinued.ChangeStatus(ProductStatus.Discontinued);
    discontinued.ChangeStatus(ProductStatus.Active);
});

Console.WriteLine();
Console.WriteLine("=== Сценарій 4: інваріант на дві сутності — Warehouse (додаткове завдання) ===");
Product stock = Product.Create("P-100", "SKU-100", "Дошка обрізна", "шт", 50);
Warehouse warehouse = Warehouse.Create("WH-1");
warehouse.IssueToCustomer(stock, "CUST-1", 5);
warehouse.IssueToCustomer(stock, "CUST-1", 5);
warehouse.IssueToCustomer(stock, "CUST-1", 5);
Console.WriteLine($"Після трьох видач: {stock}");
TryDo("четверта видача тому самому клієнту (ліміт 3)", () =>
    warehouse.IssueToCustomer(stock, "CUST-1", 5));

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
    }
}