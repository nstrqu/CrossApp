using Core.Dto;

namespace Core.Domain;

public static class ProductFactory
{
    public static ImportResult<Product> FromImport(ImportResult<ProductDto> imported)
    {
        var products = new List<Product>();
        var errors = new List<string>(imported.Errors);

        foreach (ProductDto dto in imported.Items)
        {
            try
            {
                products.Add(Product.FromDto(dto));
            }
            catch (Exception ex)
            {
                errors.Add($"{dto.Id}: {ex.Message}");
            }
        }

        return new ImportResult<Product>(products, errors);
    }
}