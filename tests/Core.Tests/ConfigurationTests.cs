using Core.Configuration;
using Core.Domain;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public sealed class ConfigurationTests : IDisposable
{
    private readonly string _testDir;

    public ConfigurationTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "CrossApp_ConfigTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }
        catch { }
    }

    [Fact]
    public void StorageOptions_ResolveFullPath_RelativePath_ResolvesRelativeToBaseDir()
    {
        var options = new StorageOptions
        {
            DataPath = "my-data",
            FileName = "test.json"
        };

        var resolved = options.ResolveFullPath(_testDir);
        var expected = Path.GetFullPath(Path.Combine(_testDir, "my-data", "test.json"));

        Assert.Equal(expected, resolved);
        Assert.True(Directory.Exists(Path.Combine(_testDir, "my-data")));
    }

    [Fact]
    public void StorageOptions_ResolveFullPath_RootedPath_ResolvesDirectly()
    {
        var absoluteDir = Path.Combine(_testDir, "absolute-root");
        var options = new StorageOptions
        {
            DataPath = absoluteDir,
            FileName = "rooted.json"
        };

        var resolved = options.ResolveFullPath(AppContext.BaseDirectory);
        var expected = Path.GetFullPath(Path.Combine(absoluteDir, "rooted.json"));

        Assert.Equal(expected, resolved);
        Assert.True(Directory.Exists(absoluteDir));
    }

    [Fact]
    public void JsonCatalogExporter_ExportsValidJson_WithCyrillicUnescaped()
    {
        var exporter = new JsonCatalogExporter();
        var filePath = Path.Combine(_testDir, "export.json");
        var items = new List<Product>
        {
            Product.Create("id-1", "SKU-1", "Цемент М400", "мішок", 40)
        };

        exporter.Export(filePath, items);

        Assert.True(File.Exists(filePath));
        var content = File.ReadAllText(filePath);
        Assert.Contains("Цемент М400", content);
        Assert.Contains("мішок", content);
        Assert.DoesNotContain("\\u0426", content);
        Assert.DoesNotContain("\\u04", content);
    }

    [Fact]
    public void CsvCatalogExporter_ExportsValidSemicolonSeparatedValues()
    {
        var exporter = new CsvCatalogExporter();
        var filePath = Path.Combine(_testDir, "export.csv");
        var items = new List<Product>
        {
            Product.Create("id-1", "SKU-1", "Пісок", "т", 15)
        };

        exporter.Export(filePath, items);

        Assert.True(File.Exists(filePath));
        var lines = File.ReadAllLines(filePath);
        Assert.Equal("id;sku;name;unit;quantity", lines[0]);
        Assert.Equal("id-1;SKU-1;Пісок;т;15", lines[1]);
    }

    [Fact]
    public void CatalogExporterFactory_CreatesCorrectInstance()
    {
        var jsonExporter = CatalogExporterFactory.Create("json");
        var csvExporter = CatalogExporterFactory.Create("csv");

        Assert.IsType<JsonCatalogExporter>(jsonExporter);
        Assert.IsType<CsvCatalogExporter>(csvExporter);
        Assert.Throws<NotSupportedException>(() => CatalogExporterFactory.Create("xml"));
    }
}

