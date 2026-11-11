using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Core;
using Core.Abstractions;
using Core.Configuration;
using Core.Domain;
using Core.Reports;
using Core.Storage;
using Microsoft.Extensions.Configuration;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var envName = Environment.GetEnvironmentVariable("CROSSAPP_ENVIRONMENT") ?? "Development";

var switchMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    { "--data-path", "Storage:DataPath" },
    { "-d", "Storage:DataPath" },
    { "--file-name", "Storage:FileName" },
    { "-f", "Storage:FileName" },
    { "--format", "Storage:Format" }
};

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{envName}.json", optional: true)
    .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
    .AddEnvironmentVariables(prefix: "CROSSAPP_")
    .AddCommandLine(args, switchMappings)
    .Build();

var options = config.Get<AppOptions>() ?? new AppOptions();
var legacy = Environment.GetEnvironmentVariable("CROSSAPP_DATA_PATH");

// Аргумент командного рядка має найвищий пріоритет (перекриває файл і змінні середовища)
bool hasCommandLineDataPath = args.Any(a =>
    a.StartsWith("--data-path=", StringComparison.OrdinalIgnoreCase) ||
    a.StartsWith("--Storage:DataPath=", StringComparison.OrdinalIgnoreCase) ||
    a.Equals("--data-path", StringComparison.OrdinalIgnoreCase) ||
    a.Equals("-d", StringComparison.OrdinalIgnoreCase));

string effectiveDataPath = hasCommandLineDataPath
    ? options.Storage.DataPath
    : (!string.IsNullOrWhiteSpace(legacy) ? legacy : options.Storage.DataPath);

var storage = new StorageOptions
{
    DataPath = effectiveDataPath,
    FileName = options.Storage.FileName,
    Format = options.Storage.Format
};

// Додаткове завдання 1: Валідація налаштувань на старті (код 2 без винятку)
if (string.IsNullOrWhiteSpace(storage.DataPath))
{
    Console.Error.WriteLine("Помилка конфігурації: параметр Storage:DataPath не може бути порожнім.");
    return 2;
}

bool isCsv = storage.Format.Equals("csv", StringComparison.OrdinalIgnoreCase);
bool hasValidExtension = storage.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                         (isCsv && storage.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));

if (string.IsNullOrWhiteSpace(storage.FileName) || !hasValidExtension)
{
    Console.Error.WriteLine("Помилка конфігурації: параметр Storage:FileName повинен містити розширення .json.");
    return 2;
}

try
{
    var fullPath = storage.ResolveFullPath(AppContext.BaseDirectory);
    var apiKey = config["Integration:ApiKey"];

    Console.WriteLine($"Файл даних : {fullPath}");
    Console.WriteLine(apiKey is null ? "ApiKey: не заданий" : $"ApiKey: присутній, довжина {apiKey.Length}");

    var command = args.FirstOrDefault(a => !a.StartsWith('-')) ?? "export";

    var store = new FileCatalogStore(fullPath);

    switch (command)
    {
        case "export":
            if (store.List().Count == 0)
            {
                store.Add(Product.Create("a1b2c3d4", "A-100", "Цемент М400", "мішок", 40));
                store.Add(Product.Create("b2c3d4e5", "A-200", "Пісок будівельний", "т", 15));
                store.Add(Product.Create("c3d4e5f6", "A-300", "Цегла червона", "шт", 1200));
            }

            var items = store.List();
            var exporter = CatalogExporterFactory.Create(storage.Format);
            exporter.Export(fullPath, items);

            int count = items.Count;
            string countWord = (count % 10 == 1 && count % 100 != 11) ? "запис" :
                               (count % 10 >= 2 && count % 10 <= 4 && (count % 100 < 10 || count % 100 >= 20)) ? "записи" : "записів";
            Console.WriteLine($"Експортовано {count} {countWord}.");

            string relativeDisplay = Path.GetRelativePath(AppContext.BaseDirectory, fullPath).Replace(Path.DirectorySeparatorChar, '/');
            Console.WriteLine($"{relativeDisplay}:");
            Console.WriteLine(File.ReadAllText(fullPath));
            break;

        case "import":
            var importArg = args.FirstOrDefault(a => a.StartsWith("--import="));
            var importPath = importArg is not null
                ? importArg.Substring("--import=".Length)
                : fullPath;

            if (!File.Exists(importPath))
            {
                Console.WriteLine($"Файл для імпорту не знайдено: {importPath}");
                return 1;
            }

            var importer = new JsonCatalogImporter();
            var loaded = await importer.LoadAsync(importPath);
            int added = 0;
            foreach (var p in loaded)
            {
                if (store.GetById(p.Id) is null)
                {
                    store.Add(p);
                    added++;
                }
            }
            Console.WriteLine($"Імпортовано {added} записів.");
            break;

        case "report-top":
            Print(ReportService.TopByQuantity(store.List(), 5));
            break;

        case "report-low":
            Print(ReportService.LowStock(store.List(), 10));
            break;

        case "report-groups":
            foreach (var g in ReportService.ByUnit(store.List()))
                Console.WriteLine($"  {g.Key,-6} позицій: {g.Count,3} сумарно: {g.TotalQuantity,6}");
            break;

        case "report-groups-query":
            foreach (var g in ReportService.ByUnitQuerySyntax(store.List()))
                Console.WriteLine($"  {g.Key,-6} позицій: {g.Count,3} сумарно: {g.TotalQuantity,6}");
            break;

        case "report-top-groups":
            foreach (var g in ReportService.TopGroupsByQuantity(store.List(), 3))
                Console.WriteLine($"  {g.Key,-6} позицій: {g.Count,3} сумарно: {g.TotalQuantity,6}");
            break;

        case "report-sum":
            var t = ReportService.Totals(store.List());
            Console.WriteLine($"  позицій {t.Items}, всього {t.Total}, середнє {t.Average:F2}, макс {t.Max}");
            break;

        case "report-activity":
            var movements = new List<StockMovement>
            {
                new("A-100", +50, "Received"),
                new("A-100", -20, "Issued"),
                new("A-200", +10, "Received"),
                new("A-300", -5, "Issued"),
            };
            foreach (var a in ReportService.WithMovements(store.List(), movements))
                Console.WriteLine($"  {a.Sku,-10} {a.Name,-22} рухів: {a.MovementsCount,3} сумарна зміна: {a.TotalDelta,5}");
            break;

        case "report-bench":
            RunBenchmark();
            break;

        default:
            Console.WriteLine($"Невідома команда: {command}");
            Console.WriteLine("Доступні команди: export | import | report-top | report-low | report-groups | report-groups-query | report-top-groups | report-sum | report-activity | report-bench");
            break;
    }
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine($"Помилка: {ex.Message}");
    return 1;
}
catch (DomainException ex)
{
    Console.Error.WriteLine($"Дані відхилено: {ex.Message}");
    return 1;
}
catch (JsonException ex)
{
    Console.Error.WriteLine($"Помилка JSON: {ex.Message}");
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Непередбачена помилка: {ex.Message}");
    return 1;
}

return 0;

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
