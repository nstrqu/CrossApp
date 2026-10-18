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
            ?? throw new InvalidOperationException($"Немає запису з id={id}.");
        product.RegisterArrival(quantity);
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Received, quantity);
    }

    public IReadOnlyList<Product> All() => _store.List();
    public Product? Find(string id) => _store.GetById(id);

    public IReadOnlyList<Product> Find(Func<Product, bool> predicate) =>
        _store.List().Where(predicate).ToList();
}