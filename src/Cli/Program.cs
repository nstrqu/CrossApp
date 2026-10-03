using Core;
using Core.Abstractions;
using Core.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;

string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");
ICatalogStore store = StoreFactory.Create(args, dataPath);

var service = new CatalogService(store);
Console.WriteLine($"Сховище: {store.GetType().Name}");

var created = service.Add("SKU-101", "Кабель UTP cat6", "м", 50);
service.Receive(created.Id, 25);

Console.WriteLine();
Console.WriteLine("=== Список товарів ===");
foreach (var p in service.All())
    Console.WriteLine($"  {p.Id} {p.Sku,-10} {p.Name,-28} {p.Quantity,5} {p.Unit}");

Console.WriteLine();
Console.WriteLine("=== Пошук: товари з кількістю > 100 (додаткове завдання) ===");
foreach (var p in service.Find(x => x.Quantity > 100))
    Console.WriteLine($"  {p.Id} {p.Sku,-10} {p.Name,-28} {p.Quantity,5} {p.Unit}");

Console.WriteLine();
Console.WriteLine("=== Сценарій відмови ===");
TryDo("дубль id", () => store.Add(created));
TryDo("Receive з неіснуючим id", () => service.Receive("NOPE-999", 10));

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
    }
}