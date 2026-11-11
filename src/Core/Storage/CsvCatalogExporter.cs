using System.Text;
using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class CsvCatalogExporter : ICatalogExporter
{
    private const char Separator = ';';

    public void Export(string path, IReadOnlyList<Product> products)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(products);

        var tmp = path + ".tmp";
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var lines = new List<string> { $"id{Separator}sku{Separator}name{Separator}unit{Separator}quantity" };
        lines.AddRange(products.Select(p =>
            $"{p.Id}{Separator}{p.Sku}{Separator}{p.Name}{Separator}{p.Unit}{Separator}{p.Quantity}"));

        File.WriteAllLines(tmp, lines, Encoding.UTF8);
        File.Move(tmp, path, overwrite: true);
    }
}

