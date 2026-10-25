using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class JsonCatalogImporter : ICatalogImporter, ICatalogStreamImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested(); // уже скасовано: файл навіть не відкриваємо

        await using var stream = OpenAsync(path);

        var dtos = await JsonSerializer.DeserializeAsync<List<ProductDto>>(stream, Options, ct)
            ?? throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку.");

        var result = new List<Product>(dtos.Count);
        for (var i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            result.Add(ToProduct(dtos[i], i + 1));
        }
        return result;
    }

    // Додаткове 1: потоковий імпорт. [EnumeratorCancellation] зливає токен із WithCancellation(...)
    public async IAsyncEnumerable<Product> StreamAsync(string path,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var stream = OpenAsync(path);

        var number = 0;
        await foreach (var dto in JsonSerializer.DeserializeAsyncEnumerable<ProductDto>(stream, Options, ct))
        {
            ct.ThrowIfCancellationRequested();
            yield return ToProduct(dto, ++number);
        }
    }

    private static FileStream OpenAsync(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

    private static Product ToProduct(ProductDto? dto, int number)
    {
        if (dto is null)
            throw new DomainException($"Запис {number}: порожній елемент.");
        if (dto.Quantity == 0)
            throw new DomainException($"Запис {number}: кількість не може бути 0.");

        try { return Product.FromDto(dto); }
        catch (ArgumentException ex)
        { throw new DomainException($"Запис {number}: {ex.Message}", ex); }
    }
}