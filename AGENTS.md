# AGENTS.md — DumaLens

Конвенции репозитория для агентов и разработчиков. Проектная документация — в `Docs/` (локальная, в репозиторий не входит): `05-architecture.md` (архитектура, стиль, ADR), `06-implementation-plan.md` (этапы Э0–Э13).

## Стек

- .NET 8 (`global.json` — пин SDK 8.0.x, `rollForward: latestFeature`), C# 12, `Nullable` enable, `ImplicitUsings` enable, `TreatWarningsAsErrors` enable.
- PostgreSQL + EF Core 8 (Npgsql). Два хоста: `DumaLens.Api` (REST) и `DumaLens.Web` (Blazor Server); Web обращается к Api только по HTTP через `DumaLens.Api.Client`.
- Централизованные версии пакетов — `Directory.Packages.props`; тестовые пакеты — `Directory.Build.props` по условию `*.Tests`.
- Локальный инструмент `dotnet-ef` 8.x — `.config/dotnet-tools.json` (`dotnet tool restore`).

## Стиль (`.editorconfig` из DocumentGenerator master)

- File-scoped namespaces, Allman, CRLF, `insert_final_newline = false`, отступ 4 пробела.
- Локальные переменные — `var`; поля/параметры/возвраты — явные типы. Поля `camelCase` без подчёркивания, в конструкторе `this.x = x;`.
- XML `<summary>` по-русски на каждом public-типе и члене; `GenerateDocumentationFile` включён в `DumaLens.Api` (CS1591 — ошибка).
- Репозитории без суффикса `Async`; чтение — проекции `*DbModel` вместо `Include`; время — только `IDateTimeProvider`; один `SaveChangesAsync` через `IUnitOfWork`; валидация — FluentValidation через `IValidateService`.
- Константы длин/диапазонов — только в `DumaLens.Entities.ValidationConstants`.

## Слои и DI

`Api → Services → Repositories → Context → Entities`; `Web → Api.Client → Api.Models`; `Importer → Services / Parsing / Context`; `Parsing → Entities.Contracts / Common.Contracts / Entities.ValidationConstants / Parsing.Contracts`. Обратных ссылок нет. Регистрация DI — по анкерам `IServiceAnchor` / `IRepositoryAnchor` + `RegisterAssemblyInterfacesAssignableTo<T>`.

## Команды

- Полная проверка: `build.cmd` (restore → rebuild → test → `dotnet format --verify-no-changes` → `build-server shutdown`).
- Тесты: `dotnet test DumaLens.sln`. Интеграционные — на PostgreSQL (ADR-3): при недоступном сервере — явный skip (`Xunit.SkippableFact`), не молчаливый успех.
- Миграции: `dotnet tool restore`, затем `dotnet ef migrations add <Name> --project DumaLens.Context`.

## Конфигурация и секреты (О-7)

- Строка подключения: `ConnectionStrings:DefaultConnection` в `appsettings.json` — только плейсхолдер. Реальное значение: `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."` (Development) или переменная окружения `ConnectionStrings__DefaultConnection`.
- Интеграционные тесты: `DumaLens.Api.Tests/appsettings.integration.json` — шаблон `dumalens_{0}` (одноразовая база на прогон); переопределение — `ConnectionStrings__IntegrationConnection`.
- Секреты в репозиторий не попадают.

## Пакеты (пины Э0)

- AutoMapper `[12.0.1]` — последний патч линейки 12.x (эталон); известный NU1903 подавлен через `NuGetAuditSuppress` (решение зафиксировано на Э0).
- FluentAssertions `[7.2.0]` — лицензионное ограничение (8.x не использовать).
- Swashbuckle.AspNetCore 6.6.2; System.CommandLine 2.0.0-beta4; AngleSharp 1.x; Blazor.Bootstrap 3.5.0 (опционально, Э8).
- Microsoft.IdentityModel.* 7.7.3 — линейка, совместимая с net8; JwtBearer 8.0.x.
- Тесты: xunit 2.9.3, Moq 4.20.72, Ahatornn.TestGenerator 1.1.29. Легаси-пины эталона System.Net.Http 4.3.4 / System.Text.RegularExpressions 4.3.1 не перенесены (встроены в net8, имеют известные уязвимости).
