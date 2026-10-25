using Core.Domain;
using Core.Reports;
using Xunit;

namespace Core.Tests;

public sealed class ReportServiceTests
{
    [Theory] // 4) звіт на кількох наборах
    [InlineData(10, 2)]
    [InlineData(1, 0)]
    public void LowStock_CountsOnlyBelowThreshold(int threshold, int expected) =>
        Assert.Equal(expected, ReportService.LowStock(TestData.Items(), threshold).Count);

    [Fact] // 5) детермінований топ-N з тижня 7
    public void TopByQuantity_IsDeterministic() =>
        Assert.Equal(new[] { "SKU-BIG", "SKU-MID" },
            ReportService.TopByQuantity(TestData.Items(), 2).Select(l => l.Sku));

    [Fact] // 6) порожнє джерело не падає
    public void EmptySource_DoesNotThrow()
    {
        var empty = new List<Product>();

        Assert.Empty(ReportService.LowStock(empty, 10));
        Assert.Equal((0, 0, 0d, 0), ReportService.Totals(empty));
    }
}