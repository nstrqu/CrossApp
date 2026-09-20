using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    public static ImportResult<ProductDto> Load(string path)
    {
        var errors = new List<string>();
        List<ProductDto> items;

        try
        {
            string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            items = JsonSerializer.Deserialize<List<ProductDto>>(json, options) ?? [];
        }
        catch (JsonException ex)
        {
            errors.Add($"помилка розбору JSON: {ex.Message}");
            items = [];
        }

        return new ImportResult<ProductDto>(items, errors);
    }
}