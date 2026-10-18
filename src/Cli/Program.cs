using System.Diagnostics;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Reports;
using Core.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;

string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");
ICatalogStore store = StoreFactory.Create(args, dataPath);
var service = new CatalogService(store);

Console.WriteLine($"Сховище: {store.GetType().Name}");
Console.WriteLine();

// --- Крок 3: демонстрація відкладеного виконання ---
var source = service.All().ToList();
var query = source.Where(p => p.Quantity < 10); // запит побудовано, не виконано

Console.WriteLine("=== Демо відкладеного виконання ===");
Console.WriteLine($"Дефіцитних до змін: {query.Count()}");

source.Add(Product.Create("tmp00001", "SKU-999", "Скоба", "шт", 3));
Console.WriteLine($"Дефіцитних після змін: {query.Count()}");

var snapshot = query.ToList();
source.Clear();
Console.WriteLine($"У знімку лишилось: {snapshot.Count}");
Console.WriteLine();

// --- Крок 4 + додаткові підкоманди ---
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
        // демо-переміщення для GroupJoin (додаткове завдання 1)
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