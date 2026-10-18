using Core.Domain;

namespace Core;

public static class SampleData
{
    public static List<Product> Products() => new()
    {
        Product.Create("SD-01", "SKU-001", "Цемент М400 25кг", "шт", 120),
        Product.Create("SD-02", "SKU-002", "Пісок будівельний", "т", 18),
        Product.Create("SD-03", "SKU-003", "Цегла червона", "шт", 4200),
        Product.Create("SD-04", "SKU-004", "Фарба водоемульсійна 10л", "шт", 36),
        Product.Create("SD-05", "SKU-005", "Шпаклівка фінішна", "кг", 250),
        Product.Create("SD-06", "SKU-006", "Кабель UTP cat6", "м", 75),
        Product.Create("SD-07", "SKU-007", "Кабель силовий ВВГ", "м", 40),
        Product.Create("SD-08", "SKU-008", "Труба ПВХ 50мм", "м", 5),
        Product.Create("SD-09", "SKU-009", "Саморізи 4х40", "шт", 1500),
        Product.Create("SD-10", "SKU-010", "Дюбелі 6х40", "шт", 800),
        Product.Create("SD-11", "SKU-011", "Грунтовка глибокого проникнення", "л", 60),
        Product.Create("SD-12", "SKU-012", "Розчинник уайт-спірит", "л", 8),
        Product.Create("SD-13", "SKU-013", "Утеплювач мінвата (пак.)", "шт", 30),
        Product.Create("SD-14", "SKU-014", "Плитка керамічна", "шт", 500),
        Product.Create("SD-15", "SKU-015", "Клей для плитки", "кг", 90),
        Product.Create("SD-16", "SKU-016", "Герметик силіконовий", "шт", 4),
        Product.Create("SD-17", "SKU-017", "Профіль металевий", "м", 150),
        Product.Create("SD-18", "SKU-018", "Скоба будівельна", "шт", 2000),
    };
}