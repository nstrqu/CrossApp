using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

public sealed class CatalogService(ICatalogStore store, INotificationSink? sink = null)
{
    private readonly ICatalogStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly INotificationSink? _sink = sink;

    public event EventHandler<CatalogChangedEventArgs>? Changed;

    private void OnChanged(Product p, CatalogChangeKind kind, int delta)
    {
        Changed?.Invoke(this, new CatalogChangedEventArgs(p.Id, kind, delta, p.Quantity));
        _sink?.Notify($"{kind} {p.Id} delta={delta} qty={p.Quantity}");
    }

    public Product Add(string sku, string name, string unit, int quantity)
    {
        var product = Product.Create(Guid.NewGuid().ToString("N")[..8], sku, name, unit, quantity);
        _store.Add(product);
        OnChanged(product, CatalogChangeKind.Added, quantity);
        return product;
    }

    public void Receive(string id, int quantity)
    {
        var product = _store.GetById(id)
            ?? throw new DomainException($"Немає запису з id={id}.");
        product.RegisterArrival(quantity);
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Received, quantity);
    }

    // Основний імпорт + додаткове 3: IProgress<int> (відсотки)
    public async Task<int> ImportAsync(ICatalogImporter importer, string path,
        CancellationToken ct = default, IProgress<int>? progress = null)
    {
        var items = await importer.LoadAsync(path, ct);
        var added = 0;
        var lastPercent = -1;

        for (var i = 0; i < items.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var item = items[i];

            if (_store.GetById(item.Id) is null) // ідемпотентність імпорту
            {
                _store.Add(item);
                OnChanged(item, CatalogChangeKind.Added, item.Quantity);
                added++;
            }

            var percent = (int)((i + 1) * 100L / items.Count);
            if (percent != lastPercent)
            {
                lastPercent = percent;
                progress?.Report(percent);
            }
        }
        return added;
    }

    // Додаткове 1: потоковий імпорт (записи додаються по мірі читання)
    public async Task<int> ImportStreamAsync(ICatalogStreamImporter importer, string path,
        CancellationToken ct = default)
    {
        var added = 0;
        await foreach (var item in importer.StreamAsync(path).WithCancellation(ct))
        {
            if (_store.GetById(item.Id) is not null) continue;
            _store.Add(item);
            OnChanged(item, CatalogChangeKind.Added, item.Quantity);
            added++;
        }
        return added;
    }

    // Додаткове 2: зв'язані токени — відрізняємо таймаут від скасування користувачем
    public async Task<int> ImportWithTimeoutAsync(ICatalogImporter importer, string path,
        TimeSpan timeout, CancellationToken userToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(userToken, timeoutCts.Token);

        try
        {
            return await ImportAsync(importer, path, linked.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !userToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Імпорт не вклався у {timeout.TotalMilliseconds:F0} мс.");
        }
    }

    public IReadOnlyList<Product> All() => _store.List();
    public Product? Find(string id) => _store.GetById(id);

    public IReadOnlyList<Product> Find(Func<Product, bool> predicate) =>
        _store.List().Where(predicate).ToList();

    public IReadOnlyList<Product> FindAll(Func<Product, bool> predicate) =>
        _store.List().Where(predicate).ToList();
}