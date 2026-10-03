using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

// Декоратор: обгортає інший ICatalogStore і кешує List(), щоб не питати внутрішнє
// сховище повторно, поки дані не зміняться через Add/Update/Remove.
public sealed class CachingCatalogStore(ICatalogStore inner) : ICatalogStore
{
    private readonly ICatalogStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private IReadOnlyList<Product>? _cachedList;

    public IReadOnlyList<Product> List()
    {
        _cachedList ??= _inner.List();
        return _cachedList;
    }

    public Product? GetById(string id) => _inner.GetById(id);

    public void Add(Product item)
    {
        _inner.Add(item);
        _cachedList = null; // кеш застарів, скинути
    }

    public void Update(Product item)
    {
        _inner.Update(item);
        _cachedList = null;
    }

    public bool Remove(string id)
    {
        bool removed = _inner.Remove(id);
        if (removed) _cachedList = null;
        return removed;
    }
}