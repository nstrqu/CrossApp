\# CrossApp

Наскрізний проєкт з крос-платформного програмування.



Предметна область: Склад. Сутності: Product, StockBatch, Warehouse, Movement.

Призначення: облік залишків товарів по партіях.



\## Структура solution

\- src/Core — бібліотека (multi-target net8.0;net10.0), збирає інформацію про середовище

\- src/Cli — консольний застосунок (net8.0), викликає Core і форматує вивід



\## Запуск

dotnet build

dotnet run --project src/Cli



\## Публікація

dotnet publish src/Cli -c Release -r win-x64 --self-contained true

dotnet publish src/Cli -c Release -r win-x64 --self-contained false

dotnet publish src/Cli -c Release -r linux-x64 --self-contained true



\## Порівняння режимів публікації (win-x64)

| RID     | Режим                              | Розмір publish | Потрібен runtime |

|---------|------------------------------------|----------------|-------------------|

| win-x64 | self-contained                     | \~70 МБ         | ні                |

| win-x64 | framework-dependent                | \~0.2 МБ        | так (.NET 8)      |

| win-x64 | self-contained + SingleFile        | 64.4 МБ        | ні                |

| win-x64 | self-contained + Trimmed           | 18.1 МБ        | ні                |



\## Multi-targeting

Core зібрано під net8.0 та net10.0 (встановлені SDK 8.0.424 і 10.0.401).

Cli залишено на net8.0. Умовна компіляція (#if NET10\_0\_OR\_GREATER) перевірена

класом BuildInfo — виводить різний рядок залежно від TFM.



\## Крос-компіляція

Виконано публікацію під linux-x64 з Windows — підтверджує, що збірка під іншу

ОС можлива без наявності цієї ОС на машині розробника.



\## Каталоги Core (домовленість на семестр)

\- Core/Dto/ — DTO (тиждень 3)

\- Core/Domain/ — доменні сутності (тиждень 4)

\- Core/Storage/ — сховища (тиждень 5)



\## Середовище

.NET SDK 8.0.424 / 10.0.401, Windows 11 x64



\## Доменна модель (лаба 4)



\### Сутність Product (src/Core/Domain/Product.cs)

Інваріанти:

1\. Id, Sku, Name — не порожні (ArgumentException)

2\. Початкова кількість не від'ємна (ArgumentOutOfRangeException)

3\. Кількість приходу/видачі > 0 (ArgumentOutOfRangeException)

4\. Видача не може перевищувати залишок (InvalidOperationException)

5\. Дозволені переходи статусу товару перевіряються через switch (InvalidOperationException)



Конструктор приватний, створення — лише через фабричний метод Create.

Стан (\_quantity) інкапсульований, змінюється лише методами RegisterArrival/Issue.



\### ProductStatus (src/Core/Domain/ProductStatus.cs) — додаткове завдання

Enum станів товару (Active, OutOfStock, Discontinued). Метод ChangeStatus у Product

перевіряє допустимі переходи через switch expression з кортеж-патерном.

Discontinued — фінальний стан, з нього перейти нікуди не можна.



\### Warehouse (src/Core/Domain/Warehouse.cs) — додаткове завдання

Інваріант на дві сутності: клієнт не може отримати більше 3 видач (InvalidOperationException).

Винесено в окремий клас, а не в Product, бо стосується взаємодії товару й клієнта —

Product нічого не знає про клієнтів.



\### ProductFactory (src/Core/Domain/ProductFactory.cs) — додаткове завдання

Перетворює ImportResult<ProductDto> (лаба 3) у ImportResult<Product>, додатково

перевіряючи доменні інваріанти при відновленні через FromDto — навіть якщо рядок

пройшов CSV-парсинг, він все одно може порушувати бізнес-правила.



\### DTO vs сутність

ProductDto (лаба 3) — формат даних для передачі/зберігання, без правил.

Product (лаба 4) — сутність з інкапсульованим станом і бізнес-правилами.

Зв'язок через ToDto()/FromDto().

