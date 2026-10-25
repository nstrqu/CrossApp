using Core.Events;
using Core.Services;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public sealed class CatalogServiceTests
{
    [Fact] // 1) інваріант: відмова і незмінний стан
    public void Receive_WithNegativeQuantity_Throws_AndKeepsState()
    {
        var store = new InMemoryCatalogStore();
        var service = new CatalogService(store);
        var p = service.Add("SKU-1", "Кабель", "м", 10);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.Receive(p.Id, -5));

        Assert.Equal(10, store.GetById(p.Id)!.Quantity);
    }

    [Fact] // 2) подія з тижня 6 як спосіб перевірки
    public void Add_RaisesChangedExactlyOnce()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var events = new List<CatalogChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.Add("SKU-2", "Гвинт", "шт", 7);

        Assert.Single(events);
        Assert.Equal(CatalogChangeKind.Added, events[0].Kind);
    }

    [Fact] // 3) бізнес-помилка: DomainException і жодної події
    public void Receive_UnknownId_ThrowsDomainException_AndRaisesNoEvent()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var events = new List<CatalogChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        Assert.Throws<DomainException>(() => service.Receive("NOPE", 5));

        Assert.Empty(events);
    }
}