using Core.Abstractions;
using Core.Storage;

namespace Core;

public static class StoreFactory
{
    public static ICatalogStore Create(string[] args, string dataPath)
    {
        bool useFile = args.Contains("--file");
        ICatalogStore baseStore = useFile
            ? new FileCatalogStore(dataPath)
            : new InMemoryCatalogStore(SampleData.Products());
        return new CachingCatalogStore(baseStore);
    }
}