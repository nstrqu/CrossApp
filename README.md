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

