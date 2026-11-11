using Core.Domain;

namespace Core.Abstractions;

public interface ICatalogExporter
{
    void Export(string path, IReadOnlyList<Product> products);
}

