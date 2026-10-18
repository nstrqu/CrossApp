using Core.Domain;

namespace Core.Reports;

public static class ReportService
{
    // 1) фільтр + сортування + проєкція
    public static IReadOnlyList<StockLine> LowStock(IEnumerable<Product> source, int threshold) =>
        source.Where(p => p.Quantity < threshold)
              .OrderBy(p => p.Quantity).ThenBy(p => p.Sku, StringComparer.Ordinal)
              .Select(p => new StockLine(p.Sku, p.Name, p.Quantity, p.Unit))
              .ToList();

    // 2) топ-N зі стабільним порядком
    public static IReadOnlyList<StockLine> TopByQuantity(IEnumerable<Product> source, int n) =>
        source.OrderByDescending(p => p.Quantity).ThenBy(p => p.Sku, StringComparer.Ordinal)
              .Take(n)
              .Select(p => new StockLine(p.Sku, p.Name, p.Quantity, p.Unit))
              .ToList();

    // 3) групування з підрахунком і сумою (методний синтаксис)
    public static IReadOnlyList<GroupSummary> ByUnit(IEnumerable<Product> source) =>
        source.GroupBy(p => p.Unit, StringComparer.OrdinalIgnoreCase)
              .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(p => p.Quantity)))
              .OrderByDescending(s => s.TotalQuantity)
              .ToList();

    // Додаткове 3: той самий звіт, але в синтаксисі запитів (from ... group ... into ...)
    public static IReadOnlyList<GroupSummary> ByUnitQuerySyntax(IEnumerable<Product> source) =>
        (from p in source
         group p by p.Unit into g
         orderby g.Sum(x => x.Quantity) descending
         select new GroupSummary(g.Key, g.Count(), g.Sum(x => x.Quantity)))
        .ToList();

    // 4) агрегати з обробкою порожньої послідовності
    public static (int Items, int Total, double Average, int Max) Totals(IEnumerable<Product> source)
    {
        var list = source.ToList();
        return list.Count == 0
            ? (0, 0, 0d, 0)
            : (list.Count, list.Sum(p => p.Quantity),
               list.Average(p => p.Quantity), list.Max(p => p.Quantity));
    }

    // Додаткове 2: топ-3 групи за сумарною кількістю через власний TopBy<T>
    public static IReadOnlyList<GroupSummary> TopGroupsByQuantity(IEnumerable<Product> source, int n) =>
        ByUnit(source).TopBy(g => g.TotalQuantity, n);

    // Додаткове 1: GroupJoin товарів з переміщеннями
    public static IReadOnlyList<ProductActivity> WithMovements(
        IEnumerable<Product> products, IEnumerable<StockMovement> movements) =>
        products.GroupJoin(movements,
                p => p.Id,
                m => m.ProductId,
                (p, ms) => new ProductActivity(p.Sku, p.Name, ms.Count(), ms.Sum(m => m.Delta)))
            .Where(a => a.MovementsCount > 0)
            .OrderByDescending(a => a.MovementsCount)
            .ToList();
}