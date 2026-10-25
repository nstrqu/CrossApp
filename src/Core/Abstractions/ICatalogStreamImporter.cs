using Core.Domain;

namespace Core.Abstractions;

public interface ICatalogStreamImporter
{
    IAsyncEnumerable<Product> StreamAsync(string path, CancellationToken ct = default);
}