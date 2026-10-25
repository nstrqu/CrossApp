using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public sealed class ImportTests
{
    private sealed class FakeImporter(IReadOnlyList<Product> items) : ICatalogImporter
    {
        public Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default) =>
            Task.FromResult(items);
    }

    // «Завис»: чекає, доки токен не скасують
    private sealed class HangingImporter : ICatalogImporter
    {
        public async Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
        {
            await Task.Delay(Timeout.Infinite, ct);
            return Array.Empty<Product>();
        }
    }

    // Синхронний збирач прогресу (Progress<T> викликає обробник асинхронно)
    private sealed class ListProgress : IProgress<int>
    {
        public List<int> Values { get; } = new();
        public void Report(int value) => Values.Add(value);
    }

    [Fact] // скасування
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new CatalogService(new InMemoryCatalogStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonCatalogImporter(), "any.json", cts.Token));
    }

    [Fact] // зіпсований запис: DomainException з номером запису, тимчасовий файл видаляється
    public async Task LoadAsync_WithInvalidRecord_ThrowsDomainException_WithRecordNumber()
    {
        var path = Path.Combine(Path.GetTempPath(), $"import-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path,
                """[{"Id":"A1","Sku":"SKU-A","Name":"Ok","Unit":"шт","Quantity":5},{"Id":"A2","Sku":"","Name":"Bad","Unit":"шт","Quantity":3}]""");

            var ex = await Assert.ThrowsAsync<DomainException>(
                () => new JsonCatalogImporter().LoadAsync(path));

            Assert.Contains("Запис 2", ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact] // ідемпотентність імпорту і подія на кожен доданий запис
    public async Task ImportAsync_IsIdempotent_AndRaisesEventPerAddedItem()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var events = new List<CatalogChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);
        var importer = new FakeImporter(TestData.Items());

        var first = await service.ImportAsync(importer, "ignored.json");
        var second = await service.ImportAsync(importer, "ignored.json");

        Assert.Equal(4, first);
        Assert.Equal(0, second);
        Assert.Equal(4, events.Count);
    }

    [Fact] // додаткове 2: спрацював саме таймаут
    public async Task ImportWithTimeout_WhenTimeoutFires_ThrowsTimeoutException()
    {
        var service = new CatalogService(new InMemoryCatalogStore());

        await Assert.ThrowsAsync<TimeoutException>(
            () => service.ImportWithTimeoutAsync(new HangingImporter(), "x", TimeSpan.FromMilliseconds(50)));
    }

    [Fact] // додаткове 2: скасував користувач — це не таймаут
    public async Task ImportWithTimeout_WhenUserCancels_ThrowsCancellation_NotTimeout()
    {
        using var user = new CancellationTokenSource();
        await user.CancelAsync();
        var service = new CatalogService(new InMemoryCatalogStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportWithTimeoutAsync(new HangingImporter(), "x", TimeSpan.FromSeconds(30), user.Token));
    }

    [Fact] // додаткове 1: потік повертає записи з файлу
    public async Task StreamAsync_ReturnsRecordsFromFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"stream-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path,
                """[{"Id":"S1","Sku":"SKU-S1","Name":"One","Unit":"шт","Quantity":5},{"Id":"S2","Sku":"SKU-S2","Name":"Two","Unit":"шт","Quantity":7}]""");

            var ids = new List<string>();
            await foreach (var p in new JsonCatalogImporter().StreamAsync(path))
                ids.Add(p.Id);

            Assert.Equal(new[] { "S1", "S2" }, ids);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact] // додаткове 1: токен із WithCancellation доходить до ітератора ([EnumeratorCancellation])
    public async Task StreamAsync_TokenFromWithCancellation_IsObserved()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in new JsonCatalogImporter().StreamAsync("any.json").WithCancellation(cts.Token))
            {
            }
        });
    }

    [Fact] // додаткове 3: відсотки прогресу
    public async Task ImportAsync_ReportsProgressInPercents()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var progress = new ListProgress();

        await service.ImportAsync(new FakeImporter(TestData.Items()), "ignored.json", progress: progress);

        Assert.Equal(new[] { 25, 50, 75, 100 }, progress.Values);
    }
}