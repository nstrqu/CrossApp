using System.Diagnostics;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Reports;
using Core.Services;
using Cli.Notifications;

Console.OutputEncoding = System.Text.Encoding.UTF8;

string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");
ICatalogStore store = StoreFactory.Create(args, dataPath);
var service = new CatalogService(store, new ConsoleSink());

Console.WriteLine($"Сховище: {store.GetType().Name}");
Console.WriteLine();

// ===== Лабка 6: подія Changed, підписка/відписка =====
var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

void OnCatalogChanged(object? sender, CatalogChangedEventArgs e)
{
    var line = $"{e.At:HH:mm:ss} {e.Kind,-8} {e.Id} {e.Delta,4:+0;-0} → залишок {e.QuantityAfter}";
    Console.WriteLine(line);
    File.AppendAllText(logPath, line + Environment.NewLine);
}

service.Changed += OnCatalogChanged;

int changes = 0;
service.Changed += (_, _) => changes++;

Console.WriteLine("=== Події: Add/Receive ===");
var created = service.Add("SKU-101", "Кабель UTP cat6", "м", 50);
service.Receive(created.Id, 25);
Console.WriteLine($"Подій отримано: {changes}");

service.Changed -= OnCatalogChanged;
service.Receive(created.Id, 5); // тиха операція: лог/консоль-обробник мовчать, лямбда рахує далі
Console.WriteLine($"Подій отримано: {changes}");

Console.WriteLine();
Console.WriteLine("=== Сценарій відмови ===");
TryDo("Receive з неіснуючим id", () => service.Receive("NOPE-999", 10));
TryDo("Receive з від'ємною кількістю", () => service.Receive(created.Id, -5));
Console.WriteLine();

// ===== Лабка 7: демо відкладеного виконання =====
var source = service.All().ToList();
var query = source.Where(p => p.Quantity < 10);

Console.WriteLine("=== Демо відкладеного виконання ===");
Console.WriteLine($"Дефіцитних до змін: {query.Count()}");

source.Add(Product.Create("tmp00001", "SKU-999", "Скоба", "шт", 3));
Console.WriteLine($"Дефіцитних після змін: {query.Count()}");

var snapshot = query.ToList();
source.Clear();
Console.WriteLine($"У знімку лишилось: {snapshot.Count}");
Console.WriteLine();

// ===== Лабка 7: підкоманди звітів =====
var items = service.All();
var command = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "report-top";

switch (command)
{
    case "report-top":
        Print(ReportService.TopByQuantity(items, 5));
        break;
    case "report-low":
        Print(ReportService.LowStock(items, 10));
        break;
    case "report-groups":
        foreach (var g in ReportService.ByUnit(items))
            Console.WriteLine($"  {g.Key,-6} позицій: {g.Count,3} сумарно: {g.TotalQuantity,6}");
        break;
    case "report-groups-query":
        foreach (var g in ReportService.ByUnitQuerySyntax(items))
            Console.WriteLine($"  {g.Key,-6} позицій: {g.Count,3} сумарно: {g.TotalQuantity,6}");
        break;
    case "report-top-groups":
        foreach (var g in ReportService.TopGroupsByQuantity(items, 3))
            Console.WriteLine($"  {g.Key,-6} позицій: {g.Count,3} сумарно: {g.TotalQuantity,6}");
        break;
    case "report-sum":
        var t = ReportService.Totals(items);
        Console.WriteLine($"  позицій {t.Items}, всього {t.Total}, середнє {t.Average:F2}, макс {t.Max}");
        break;
    case "report-activity":
        var movements = new List<StockMovement>
        {
            new("SD-01", +50, "Received"),
            new("SD-01", -20, "Issued"),
            new("SD-06", +10, "Received"),
            new("SD-14", -5, "Issued"),
            new("SD-14", -5, "Issued"),
        };
        foreach (var a in ReportService.WithMovements(items, movements))
            Console.WriteLine($"  {a.Sku,-10} {a.Name,-22} рухів: {a.MovementsCount,3} сумарна зміна: {a.TotalDelta,5}");
        break;
    case "report-bench":
        RunBenchmark();
        break;
    default:
        Console.WriteLine("report-top | report-low | report-groups | report-groups-query | report-top-groups | report-sum | report-activity | report-bench");
        break;
}

static void Print(IReadOnlyList<StockLine> lines)
{
    foreach (var l in lines)
        Console.WriteLine($"  {l.Sku,-10} {l.Name,-22} {l.Quantity,6} {l.Unit}");
}

static void RunBenchmark()
{
    var rnd = new Random(42);
    var big = Enumerable.Range(1, 100_000)
        .Select(i => Product.Create($"bench{i:D6}", $"SKU-{i:D6}", $"Товар {i}",
            i % 2 == 0 ? "шт" : "кг", rnd.Next(0, 500)))
        .ToList();

    var sw = Stopwatch.StartNew();
    var withoutToList = big.Where(p => p.Quantity < 50)
                            .OrderBy(p => p.Quantity)
                            .Select(p => p.Sku)
                            .Count();
    sw.Stop();
    Console.WriteLine($"Без ToList у середині: {sw.ElapsedMilliseconds} мс, знайдено {withoutToList}");

    sw.Restart();
    var withToList = big.Where(p => p.Quantity < 50)
                         .ToList()
                         .OrderBy(p => p.Quantity)
                         .Select(p => p.Sku)
                         .Count();
    sw.Stop();
    Console.WriteLine($"З ToList у середині: {sw.ElapsedMilliseconds} мс, знайдено {withToList}");
}

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