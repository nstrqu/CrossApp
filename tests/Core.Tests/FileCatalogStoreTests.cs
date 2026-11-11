using Core.Domain;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public sealed class FileCatalogStoreTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _filePath;

    public FileCatalogStoreTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "CrossApp_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _filePath = Path.Combine(_testDir, "items.json");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch
        {
            // Ігноруємо помилки очищення у Dispose
        }
    }

    [Fact]
    public void Add_AndNewStoreInstance_ListReturnsSameProduct()
    {
        // 1. Створюємо перший екземпляр сховища
        var store1 = new FileCatalogStore(_filePath);
        var product = Product.Create("p-001", "SKU-001", "Цемент М400", "мішок", 40);

        // 2. Додаємо товар
        store1.Add(product);

        // 3. Створюємо новий екземпляр сховища над тим самим файлом
        var store2 = new FileCatalogStore(_filePath);
        var items = store2.List();

        // 4. Перевіряємо, що List повертає той самий Product
        var loaded = Assert.Single(items);
        Assert.Equal(product.Id, loaded.Id);
        Assert.Equal(product.Sku, loaded.Sku);
        Assert.Equal(product.Name, loaded.Name);
        Assert.Equal(product.Unit, loaded.Unit);
        Assert.Equal(product.Quantity, loaded.Quantity);
    }

    [Fact]
    public void Flush_IsAtomic_NoTempFileRemains()
    {
        var store = new FileCatalogStore(_filePath);
        store.Add(Product.Create("p-002", "SKU-002", "Пісок", "т", 10));

        Assert.True(File.Exists(_filePath));
        Assert.False(File.Exists(_filePath + ".tmp"));
    }

    [Fact]
    public void CorruptedJson_ThrowsInvalidOperationException()
    {
        File.WriteAllText(_filePath, "{ this is not valid json! }");
        var store = new FileCatalogStore(_filePath);

        var ex = Assert.Throws<InvalidOperationException>(() => store.List());
        Assert.Contains("некоректний JSON", ex.Message);
    }

    [Fact]
    public void MissingDirectory_IsCreatedAutomaticallyOnFlush()
    {
        var nestedPath = Path.Combine(_testDir, "sub", "folder", "items.json");
        var store = new FileCatalogStore(nestedPath);

        store.Add(Product.Create("p-003", "SKU-003", "Цегла", "шт", 100));

        Assert.True(File.Exists(nestedPath));
    }
}

