using Core.Abstractions;

namespace Core.Storage;

public static class CatalogExporterFactory
{
    public static ICatalogExporter Create(string format) =>
        format.Trim().ToLowerInvariant() switch
        {
            "json" => new JsonCatalogExporter(),
            "csv" => new CsvCatalogExporter(),
            _ => throw new NotSupportedException($"Формат '{format}' не підтримується. Доступні формати: json, csv.")
        };
}

