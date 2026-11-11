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
## Сервісний шар (лаба 5)

### Інтерфейс ICatalogStore (src/Core/Abstractions/ICatalogStore.cs)
Контракт сховища каталогу: List, GetById, Add, Update, Remove.
CatalogService залежить лише від цього інтерфейсу, не від конкретної реалізації.

### Реалізації (src/Core/Storage/)
- InMemoryCatalogStore — зберігає дані в Dictionary в пам'яті, дані зникають після виходу з програми.
- FileCatalogStore — кешує дані в пам'яті, довантажує з JSON-файлу при першому зверненні,
  записує на диск (Flush) після кожної зміни. Формат файлу — список ProductDto.
- CachingCatalogStore (додаткове завдання) — декоратор, обгортає будь-яке інше сховище
  і кешує результат List(), скидаючи кеш при Add/Update/Remove.

### CatalogService (src/Core/Services/CatalogService.cs)
Бізнес-операції: Add (створення товару), Receive (прихід), All, Find(id), Find(predicate).
Find(predicate) — додаткове завдання, пошук за довільною умовою через Func<Product, bool>,
підготовка до LINQ-звітів тижнів 6-7.
Залежність від ICatalogStore передається через конструктор (ручний Dependency Injection).

### StoreFactory (src/Core/StoreFactory.cs) — додаткове завдання
Виносить вибір і збірку конкретних реалізацій (InMemory/File + Caching) з Program.cs
в окремий метод Create(args, dataPath).

### Composition root (src/Cli/Program.cs)
Єдине місце, де фактично визначається, які конкретні класи використовуються
(через StoreFactory). Перемикання реалізації — аргументом командного рядка --file:

dotnet run --project src/Cli            (InMemoryCatalogStore, дані не зберігаються)
dotnet run --project src/Cli -- --file  (FileCatalogStore, дані зберігаються в data/catalog.json)

Обидва варіанти додатково обгорнуті в CachingCatalogStore.

### Схема залежностей
Cli (Program.cs) → StoreFactory → CachingCatalogStore → InMemoryCatalogStore | FileCatalogStore
Cli (Program.cs) → CatalogService → ICatalogStore (інтерфейс, конкретний тип невідомий сервісу)

### Що зміниться на тижні 11 (БД)
Коли сховищем стане база даних через Entity Framework Core, з'явиться нова реалізація
ICatalogStore (наприклад EfCatalogStore). CatalogService і доменна модель Product
не зміняться — зміниться лише вибір конкретного класу в StoreFactory/Program.cs.

## Лабораторна робота 7 — звіти через LINQ

Реалізовано `ReportService` з запитами: LowStock, TopByQuantity, ByUnit (групування), Totals (агрегати),
а також додаткові: WithMovements (GroupJoin), TopGroupsByQuantity (власний TopBy<T>), ByUnitQuerySyntax,
report-bench (Stopwatch).

**Query-синтаксис vs методний синтаксис.** Звіт `ByUnitQuerySyntax` (from...group...into...) дає ідентичний
результат методному `ByUnit` — обидва транслюються компілятором в однакові виклики GroupBy/Select.
Query-синтаксис читається природніше для групування з сортуванням (orderby одразу після group), але не
підтримує деякі перевантаження напряму (наприклад, StringComparer для GroupBy), тому для таких випадків
методний синтаксис лишається зручнішим.

**Stopwatch-бенчмарк.** На 100 000 згенерованих записів порівняли ланцюжок з проміжним ToList() між Where
і OrderBy та без нього: "без ToList" — 8 мс, "з ToList" — 5 мс, обидва варіанти знайшли однакову кількість
елементів (10034). На одноразовому проході різниця переважно в межах похибки вимірювання — реальна перевага
ToList() у середині ланцюжка з'являється, коли проміжний результат використовується повторно, а не коли він
одразу йде далі по ланцюжку.

**Відкладене виконання.** Запит `source.Where(...)` будується, але не виконується одразу. Реальний перебір
стається лише при виклику Count()/ToList()/foreach. Тому перший Count() порахував дефіцитні товари з
початкового source, а після source.Add(...) другий Count() перерахував запит заново і побачив доданий
елемент — число змінилось. Після query.ToList() результат "заморожено" у звичайний список (snapshot), тому
подальше source.Clear() на нього вже не впливає.
Додаткові (лаба 8): потоковий імпорт (--stream, IAsyncEnumerable + [EnumeratorCancellation]),
зв'язані токени (--linked-timeout-ms=N: таймаут відрізняється від скасування користувачем),
IProgress<int> (відсотки в консолі), покриття: dotnet test --collect:"XPlat Code Coverage".

## Лабораторна робота 9 — Шляхи, JSON, конфігурація, Secrets

### Мета
Забезпечити кросплатформність застосунку: усунути абсолютні шляхи з коду, винести налаштування в ієрархічну конфігурацію (`appsettings.json`, змінні середовища, аргументи CLI), захистити чутливі дані за допомогою `user-secrets`, реалізувати імпорт та атомарний експорт колекції в JSON та CSV.

### Профілі конфігурації
- **Production (`appsettings.json`)**: базовий профіль за замовчуванням.
  - `EnvironmentName`: `"Production"`
  - `Logging:MinimumLevel`: `"Information"`
  - `Storage:DataPath`: `"data"`
  - `Storage:FileName`: `"items.json"`
  - `Storage:Format`: `"json"`
  - `Warehouse:DefaultUnit`: `"шт"`
- **Development (`appsettings.Development.json`)**: профіль для локальної розробки (активується через `CROSSAPP_ENVIRONMENT=Development`).
  - `EnvironmentName`: `"Development"`
  - `Logging:MinimumLevel`: `"Debug"` (детальніше логування)

### Порядок джерел конфігурації (хто кого перекриває)
1. `appsettings.json` (базові налаштування)
2. `appsettings.{Environment}.json` (перекриває базовий конфіг для середовища)
3. `UserSecrets` (`secrets.json`, читається в середовищі розробки поза git)
4. Змінні середовища з префіксом `CROSSAPP_` (наприклад, `CROSSAPP_Storage__DataPath`)
5. Спрощена змінна середовища `CROSSAPP_DATA_PATH` (має вищий пріоритет над секцією Storage)
6. Аргументи командного рядка CLI (`--data-path=...`, `--Storage:DataPath=...` — найвищий пріоритет)

### Таблиця ключів конфігурації

| Ключ конфігурації | Опис | Значення за замовчуванням | Змінна середовища (prefix `CROSSAPP_`) | Аргумент командного рядка |
|---|---|---|---|---|
| `EnvironmentName` | Назва середовища | `Development` / `Production` | `CROSSAPP_ENVIRONMENT` | `--EnvironmentName` |
| `Logging:MinimumLevel` | Мінімальний рівень логування | `Information` (Prod) / `Debug` (Dev) | `CROSSAPP_Logging__MinimumLevel` | `--Logging:MinimumLevel` |
| `Storage:DataPath` | Каталог для збереження даних | `data` | `CROSSAPP_Storage__DataPath` або `CROSSAPP_DATA_PATH` | `--data-path` / `-d` / `--Storage:DataPath` |
| `Storage:FileName` | Ім'я файлу даних | `items.json` | `CROSSAPP_Storage__FileName` | `--file-name` / `-f` / `--Storage:FileName` |
| `Storage:Format` | Формат експорту (`json` \| `csv`) | `json` | `CROSSAPP_Storage__Format` | `--format` / `--Storage:Format` |
| `Warehouse:DefaultUnit` | Одиниця виміру за замовчуванням | `шт` | `CROSSAPP_Warehouse__DefaultUnit` | `--Warehouse:DefaultUnit` |
| `Integration:ApiKey` | Секретний ключ інтеграції | читається з user-secrets | `CROSSAPP_Integration__ApiKey` | `--Integration:ApiKey` |

### Приклади встановлення змінних середовища

#### PowerShell (Windows):
```powershell
# Зміна каталогу даних
$env:CROSSAPP_DATA_PATH = "C:\Users\student\custom_data"

# Зміна формату експорту на CSV
$env:CROSSAPP_Storage__Format = "csv"
$env:CROSSAPP_Storage__FileName = "items.csv"

# Перевизначення ApiKey через середовище
$env:CROSSAPP_Integration__ApiKey = "my-secret-token"

# Запуск
dotnet run --project src/Cli -- export

# Очищення змінних
$env:CROSSAPP_DATA_PATH = ""
$env:CROSSAPP_Storage__Format = ""
$env:CROSSAPP_Storage__FileName = ""
$env:CROSSAPP_Integration__ApiKey = ""
```

#### Bash (Linux / macOS):
```bash
# Зміна каталогу даних перед запуском
CROSSAPP_DATA_PATH=/tmp/data dotnet run --project src/Cli -- export

# Перевизначення ключа і запуск експорту в CSV
CROSSAPP_Storage__Format=csv CROSSAPP_Storage__FileName=items.csv dotnet run --project src/Cli -- export

# Змінна середовища для секрету
export CROSSAPP_Integration__ApiKey=my-linux-secret
dotnet run --project src/Cli -- export
```

### Секрети (User Secrets)
Для захисту чутливих даних (паролі, токени, ключі API) від випадкового потрапляння в Git:
- У проекті `Cli.csproj` додано ідентифікатор `<UserSecretsId>`.
- Секрети зберігаються поза каталогом репозиторію у профілі користувача ОС:
  - **Windows**: `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`
    (`C:\Users\<username>\AppData\Roaming\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`)
  - **Linux / macOS**: `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`
- Керування через CLI:
  ```bash
  dotnet user-secrets init --project src/Cli
  dotnet user-secrets set "Integration:ApiKey" "local-dev-key" --project src/Cli
  dotnet user-secrets list --project src/Cli
  ```
- У коді читається через `config["Integration:ApiKey"]`. У консолі виводиться виключно факт наявності та довжина ключа.

### Атомарний запис і робота з JSON
- **Атомарний запис**: збереження виконується спочатку в тимчасовий файл `_path + ".tmp"`, після чого здійснюється атомарна заміна через `File.Move(tmp, _path, overwrite: true)`. Це гарантує цілісність даних у разі раптового збою живлення або аварійного завершення процесу.
- **Підтримка кирилиці**: `Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping` запобігає зайвому екрануванню символів українського алфавіту у послідовності на кшталт `\u0426`.
- **CamelCase**: `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` автоматично транслює властивості в стандартний JSON camelCase (`id`, `sku`, `name`, `unit`, `quantity`).
- **Стійкість до помилок**: при пошкодженні JSON користувач отримує чітке інформативне повідомлення без необробленого stack trace.

### Реалізація додаткових завдань
1. **Валідація налаштувань на старті**:
   - При порожньому `Storage:DataPath` або `Storage:FileName` без `.json` (для JSON формату) застосунок виводить зрозуміле повідомлення про помилку та завершується з кодом повернення `2` без викидання винятків.
2. **Аргумент `--data-path=...` через `AddCommandLine`**:
   - Завдяки switch mappings аргументи командного рядка (`--data-path` / `-d`) перекривають як значення з файлу, так і змінну середовища `CROSSAPP_DATA_PATH`.
3. **Формат експорту через `Storage:Format` ("json" | "csv") за спільним інтерфейсом**:
   - Реалізовано інтерфейс `ICatalogExporter` та реалізації `JsonCatalogExporter` і `CsvCatalogExporter`.
   - Вибір формату здійснюється фабрикою `CatalogExporterFactory` на основі конфігурації або параметра `--format=csv`.
4. **Тест `FileCatalogStore` у тимчасовому каталозі**:
   - Клас `FileCatalogStoreTests` створює унікальний каталог у `Path.GetTempPath()`.
   - Тестує збереження, повторне завантаження через новий екземпляр сховища, атомарність запису, автостворення відсутніх підкаталогів.
   - Повністю очищує тимчасовий каталог у методі `Dispose()`.
