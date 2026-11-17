# 05 — Архитектура и стиль кода DumaLens

Версия 2 (13.11.2026): разделение на **Web (фронтенд) и Api (REST)** — отдельными проектами; заложена база ролей и аутентификации ([07-security.md](07-security.md)).

Версия 3 (14.11.2026): актуализация после Э2 — enum'ы голосований в `Entities.Contracts`, уточнён состав `Parsing`/`Parsing.Contracts`, исправлено обоснование ADR-1 (DG master на SQL Server), PostgreSQL 16+.

Основание — два проекта-ориентира:

- **Specular** — **приоритетный ориентир** по архитектуре: модульный монолит `Specular.Api` (REST) + `Specular.Portal` (Blazor WASM), централизованные версии пакетов (`Directory.Packages.props`), NSwag-клиент API в репозитории, JWT + refresh с HttpOnly-cookie, роли, слои `DataAccess`/`Common.Repositories`/UnitOfWork, `BaseAuditEntity`, единый фильтр исключений. Внимание: Specular собран под **net10** — переносим решения, но не пины версий (О-1: строго .NET 8).
- **DocumentGenerator master** (GitHub `Karo4a/DocumentGenerator`, ветка master) — ориентир по стилю кода, плоским слоям и механике auth для Blazor Server: `Api` + `Web`, `AuthController`/`UserController`, `AuthService`/`TokenService`, `Role`/`UserRole`/`RefreshToken`, seed ролей, `AuthDelegatingHandler` + `ClientTokenStore` (ProtectedLocalStorage). Ветки `main` в репозитории нет — актуальная `master`.

Архитектура повторяет слоистую структуру эталонов; отличия продиктованы спецификой данных (массовый импорт, аналитические агрегаты) и зафиксированы как решения (ADR) в §6.

## 1. Состав решения

Все проекты — `net8.0`, C# 12, `Nullable` enable, `ImplicitUsings` enable. Решение `DumaLens.sln` открывается в Visual Studio 2022.

```text
DumaLens.sln
│
├─ DumaLens.Common.Contracts        — IDateTimeProvider, общие контракты (DI-анкоры — в слоях, как в эталоне)
├─ DumaLens.Common                  — DateTimeProvider, DI-расширения (RegisterAssemblyInterfacesAssignableTo)
│
├─ DumaLens.Entities.Contracts      — DbBaseEntity, IEntityWithId/Audit/SoftDeleted, Enums (VoteResult, VoteStage, VoteOutcome, VoteSource)
├─ DumaLens.Entities                — сущности + Enums/Role (enum'ы голосований — в Entities.Contracts)
├─ DumaLens.Entities.Configurations — EF-конфигурации (IEntityConfiguration-маркер)
├─ DumaLens.Entities.ValidationConstants — длины/форматы/диапазоны (единый источник с валидаторами)
├─ DumaLens.Entities.Default        — seed: созывы, канонические фракции + периоды, роли
│
├─ DumaLens.Context.Contracts       — IReader, IWriter, IDbWriter<T>, IUnitOfWork, BaseWriteRepository<T>, CommonSpecs
├─ DumaLens.Context                 — DumaLensContext : DbContext, IReader, IWriter, IUnitOfWork; Migrations; DesignTime-фабрика
│
├─ DumaLens.Repositories.Contracts  — I*ReadRepository / I*WriteRepository, *DbModel-проекции
├─ DumaLens.Repositories            — ReadRepositories/, WriteRepositories/, IRepositoryAnchor
│
├─ DumaLens.Services.Contracts      — IServices/, Models/{Домен}/, Exceptions/, IValidateService
├─ DumaLens.Services.Contracts.Models — модели сервисного слоя (в DG — внутри Services.Contracts; проект пока пуст, заполняется на Э5)
├─ DumaLens.Services                — сервисы, Validators/, ServiceProfile (AutoMapper), IServiceAnchor
│
├─ DumaLens.Parsing.Contracts       — контракты парсеров (IVotePageParser, IVoteListingParser, IVoteArchiveParser, ITitleNormalizer, IVoteParseValidator), модели разбора, Enums/ParseIssueCode
├─ DumaLens.Parsing                 — HTML-парсеры карточки/листинга, парсер JSON архива, нормализатор заголовков, валидатор разбора
│
├─ DumaLens.Importer                — CLI (System.CommandLine): import-archive, crawl, verify, recalc-stats
│
├─ DumaLens.Api                     — REST-хост: Program.cs, Controllers/ (Votes, Bills, Factions, Deputies,
│                                      Convocations, Stats, Auth, Users), Infrastructure/ (ApiMapper,
│                                      DumaLensExceptionFilter, EnumNameSchemaFilter), EnvironmentProvider
├─ DumaLens.Api.Models              — *ApiModel / *RequestApiModel, Enums/, ApiExceptionDetail
├─ DumaLens.Api.Client              — NSwag-клиент REST API (генерируется из OpenAPI-спеки, хранится в репозитории;
│                                      паттерн Specular.Api.Client / DocumentGenerator.Api.Client)
│
├─ DumaLens.Web                     — фронтенд: Blazor Server (интерактивный серверный рендеринг, как
│                                      DocumentGenerator.Web), Components/ (Pages, Layout), Services/
│                                      (ClientTokenStore, UserState, AuthDelegatingHandler), wwwroot/;
│                                      обращается к Api только по HTTP через DumaLens.Api.Client
│
├─ DumaLens.Context.Tests           — база тестов: DumaLensContextInMemory + Prepare*-хелперы
├─ DumaLens.Repositories.Tests      — репозитории (InMemory)
├─ DumaLens.Services.Tests          — сервисы, нормализация, агрегаты, валидаторы (InMemory)
├─ DumaLens.Parsing.Tests           — парсеры и нормализаторы на HTML/JSON-фикстурах (без БД)
├─ DumaLens.Api.Tests               — интеграционные тесты API (WebApplicationFactory + PostgreSQL)
└─ DumaLens.Database.Tests          — структура БД против модели EF (Npgsql)
```

Направление зависимостей (строго сверху вниз, обратных ссылок нет):

```text
Api → Services → Repositories → Context → Entities
Web → Api.Client → Api.Models        (фронтенд НЕ ссылается на Services/Repositories/Context)
Importer → Services / Parsing / Context
Parsing → Entities.Contracts / Common.Contracts / Entities.ValidationConstants / Parsing.Contracts (без БД)
Api.Models, *.Contracts — листовые проекты без зависимостей на реализацию
```

**Web и Api — разные хосты и разные процессы.** Web общается с Api только по HTTP через типизированный NSwag-клиент (даже когда оба стоят на одной машине). Это жёсткая граница фронтенда и бэкенда: UI можно заменить (например, на WASM по образцу Specular.Portal), не трогая Api.

Отличия от эталонов и их причины — §6 (ADR-1…ADR-13).

## 2. Стиль кода (DocumentGenerator master + Specular)

### 2.1. Файл `.editorconfig`

Копируется из DocumentGenerator **master** (GitHub `Karo4a/DocumentGenerator`; он там полный, ~10 КБ правил). Ключевые правила, которые соблюдаются:

- `indent_style = space`, `indent_size = 4`; `end_of_line = crlf`; `insert_final_newline = false` (как в эталоне).
- `csharp_style_namespace_declarations = file_scoped` — **file-scoped namespace** (`namespace DumaLens.Services;`). Проверено по коду master и его `.editorconfig` (`file_scoped:suggestion`); Specular также использует file-scoped. Прежняя редакция этого документа утверждала block_scoped по устаревшей локальной ветке `initial` — исправлено (ADR-6).
- `csharp_new_line_before_open_brace = all` (Allman), `csharp_prefer_braces = true` — скобки всегда.
- `csharp_using_directive_placement = outside_namespace`; `dotnet_sort_system_directives_first = false`; `dotnet_separate_import_directive_groups = false`.
- `dotnet_style_namespace_match_folder = true` — namespace = путь файла.
- `csharp_style_var_*` — в эталоне `false` (явные типы), но в коде эталона локальные переменные пишутся через `var` (`var item = await service.GetById(...)`). Принимаем как в коде эталона: **локальные — `var`**, поля/параметры/возвраты — явные типы.
- `dotnet_style_readonly_field = true`, `dotnet_style_require_accessibility_modifiers = for_non_interface_members`.
- Из Specular дополнительно перенимаем: централизованные версии пакетов (`Directory.Packages.props` + `Directory.Build.props`), `AGENTS.md` с конвенциями репозитория, file-scoped namespaces, единый `ApiExceptionDetail`/фильтр исключений. Пины Specular (net10: EF Core 10, AutoMapper 14, IdentityModel 8.14) напрямую **не переносим** — проект строго net8 (О-1).

### 2.2. Именование и оформление

- Поля `camelCase` **без** подчёркивания; в конструкторе `this.service = service;`.
- Типы и члены `PascalCase`; интерфейсы с `I`; репозитории **без суффикса `Async`** (`GetById`, `GetAll`, `Any` — конвенция эталона).
- XML `<summary>` **по-русски** на каждом public-типе и члене; реализация интерфейса — `/// <inheritdoc cref="IXxx"/>`; ссылки — `<see cref="..."/>`. `GenerateDocumentationFile` включён в `DumaLens.Api` (как `*.Api` в эталоне); остальные слои проверяются глазами/ревью.
- Исключения домена: `DumaLensNotFoundException`, `DumaLensDuplicateException`, `DumaLensValidationException` (аналог `DocumentGenerator*`).
- Константы длин/форматов — только в `DumaLens.Entities.ValidationConstants` (одно число на длину, общее для `HasMaxLength` и FluentValidation). Магические числа в конфигурациях/валидаторах — блокирующее замечание ревью.

### 2.3. Слои: правила (жёсткие)

| Слой | Правила |
|---|---|
| Entities | наследование `DbBaseEntity`; `= null!` / `= []` / `= string.Empty` для non-nullable; без логики |
| Configurations | `ToTable`, длины из `*ValidationConstants`, `HasIndex` с именем, `DeleteBehavior`; уникальные индексы — с `HasFilter` там, где есть soft-delete |
| Context | `DumaLensContext : DbContext, IReader, IWriter, IUnitOfWork`; `AsNoTracking` на чтении; миграции в этом же проекте |
| Repositories | чтение — `Select(... => new XxxDbModel {...})` вместо `Include`/`ThenInclude`; проверки существования — `Any`, не `GetAll`; запись — через `IDbWriter<T>` / `BaseWriteRepository<T>` (аудит и soft-delete заполняет он); `EntityState.Modified` вне контекста запрещён |
| Services | один `SaveChangesAsync` через `IUnitOfWork`; время — только `IDateTimeProvider`; без `try/catch` (доменные исключения); маппинг — AutoMapper-профили (`ServiceProfile`, `ApiMapper`), не руками; валидация — FluentValidation через `IValidateService` |
| Api | `[ApiController]`, `[Route("api/[controller]")]`, `[ProducesResponseType]` на каждый реальный код (200/400/401/403/404/409/422), `CancellationToken` в параметрах, `mapper.Map<...ApiModel>` на выход; логики в контроллере нет — маппинг + вызов сервиса + валидация; `[Authorize(Roles = ...)]`/политики на защищённых эндпоинтах |
| Web | только компоненты + вызовы `DumaLens.Api.Client`; никакой бизнес-логики и прямого доступа к БД; состояние авторизации — `AuthenticationStateProvider` поверх `UserState`; защищённые страницы — `AuthorizeView`/`[Authorize]` |
| DI | сборка по анкерам `IServiceAnchor` / `IRepositoryAnchor` + `RegisterAssemblyInterfacesAssignableTo<T>` (как в эталоне), а не ручная регистрация каждого сервиса |

### 2.4. Тесты (конвенции)

- Стек: **xunit 2.9.3 + Moq 4.20.72 + FluentAssertions 7.2.0 + Ahatornn.TestGenerator** (пины как в `Directory.Build.props` эталона; FluentAssertions 8 — другая лицензия, не использовать).
- `DumaLens.Context.Tests` — не тесты, а база: `DumaLensContextInMemory : IAsyncDisposable` (InMemory-провайдер, Guid в имени базы, `ConfigureWarnings(Ignore(TransactionIgnoredWarning))`, `EnsureDeletedAsync` только в `DisposeAsync`), `PrepareVote/PrepareDeputy/PrepareFaction/PrepareBill(...)`-хелперы.
- Сервисные тесты: цепочка сервис → реальный репозиторий → InMemory-контекст; **репозитории не мокать**; Mock — только внешние провайдеры (`IDateTimeProvider`) и HTTP.
- Имена `{Метод}Should{Ожидание}`, AAA-комментарии, summary над тестом; исключения — `var result = () => ...; await result.Should().ThrowAsync<T>().WithMessage(...)`.
- Данные — `TestEntityProvider.Shared.Create<T>(x => {...})`.
- Интеграционные (`DumaLens.Api.Tests`): `TestWebApplicationFactory` + `appsettings.integration.json`, уникальное имя базы `dumalens_{Guid}`, окружение через `EnvironmentProvider.IntegrationEnvironment` (без опечатки эталона), `AddControllersAsServices()`, `DependenciesTests` ([Theory] по всем контроллерам). Отличие от эталона: PostgreSQL вместо LocalDB (ADR-3). Тесты auth: логин/refresh/logout, ролевой доступ (401/403) — с реальными сервисами и InMemory/PostgreSQL, без моков репозиториев.
- `DumaLens.Parsing.Tests` — чистые юнит-тесты на фикстурах: сохранённые HTML (`vote_126282.html`, листинги) и JSON-файлы архива (включая пустое голосование и +0400).
- Запуск: `dotnet test DumaLens.sln`; предупреждения ловить с `-t:Rebuild` (инкрементальная сборка прячет CS8618/CS1573).

### 2.5. Пакеты (пины)

| Пакет | Версия | Примечание |
|---|---|---|
| Microsoft.EntityFrameworkCore / Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.x | строго EF Core 8 (net8) |
| AutoMapper | 12.0.1 | как эталон; учитывать NU1903 (известная уязвимость) — на этапе каркаса проверить актуальный патч 12.x и зафиксировать решение |
| FluentValidation.AspNetCore | 11.x | валидация запросов |
| Swashbuckle.AspNetCore | 6.6.2 | Swagger + XML-комментарии |
| System.CommandLine | 2.0.0-beta4 | CLI импортера |
| AngleSharp | 1.x | разбор HTML (листинг; `deputiesData` — regex + System.Text.Json) |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.x | JWT-аутентификация Api |
| Microsoft.IdentityModel.JsonWebTokens / Tokens | 7.x (совместимые с net8) | выпуск/валидация JWT (Specular использует 8.14 под net10 — для net8 взять совместимую линейку и зафиксировать пин на Э0) |
| NSwag.MSBuild / NSwag.CodeGeneration.CSharp | актуальный 14.x | генерация `DumaLens.Api.Client` из OpenAPI (паттерн обоих эталонов) |
| Blazor.Bootstrap | 3.5.0 | UI-компоненты Web (как DocumentGenerator.Web); опционально, можно свой CSS |
| xunit / Moq / FluentAssertions / Ahatornn.TestGenerator | 2.9.3 / 4.20.72 / [7.2.0] / 1.1.29 | как `Directory.Build.props` эталона |

`Directory.Build.props` — по образцу эталона: общие свойства + `ItemGroup Condition="$(MSBuildProjectName.EndsWith('.Tests'))"` с тестовыми пакетами. Версии пакетов централизуем в `Directory.Packages.props` (паттерн Specular). `global.json` — пин SDK `8.0.x` с `rollForward: latestFeature` (на машине разработчика установлены SDK 9/10; пин гарантирует сборку именно под net8-инструментарий; runtime 8.0.x установлен). В корне — `AGENTS.md` с конвенциями (паттерн Specular): стек, стиль, слои, команды сборки/тестов, ссылка на Docs/.

## 3. Модель данных

### 3.1. ER-обзор

```text
Convocation 1──* Vote *──1 Bill (nullable)
Faction 1──* FactionPeriod *──1 Convocation
Deputy 1──* DeputyFactionMembership *──1 FactionPeriod
Vote 1──* DeputyVote *──1 Deputy
Vote 1──* VoteFactionStats *──1 FactionPeriod
Vote 1──* VoteTag *──1 Tag            (v1.1)
Bill 1──* BillTag *──1 Tag            (v1.1)
Bill 1──1 BillImportance              (v1.2)
BillImportance 1──* ImportanceVote    (v1.2)
User *──1 Role                        (база auth: одна роль на пользователя, как DG master)
User 1──* RefreshToken                (ротация refresh-токенов)
ImportQuarantine — журнал аномалий импорта
```

### 3.2. Таблицы (MVP)

Общие поля аудита (`DbBaseEntity`): `Id` (GUID для справочников/пользовательских сущностей), `CreatedAt`, `UpdatedAt`, `DeletedAt` (soft-delete). Для потоковых данных (`DeputyVote`, `VoteFactionStats`) аудита/soft-delete нет (ADR-2: 45,6 млн строк, данные иммутабельны). `DeputyVotes` — составной PK (`VoteId`, `DeputyId`) из узких int-ключей, ссылающихся на `RowId` справочников (ADR-14); `VoteFactionStats` — суррогатный `bigint` identity, FK остаются GUID.

| Таблица | Ключевые поля | Индексы |
|---|---|---|
| `Convocations` | `Number` (1–9), `SiteCode` (`AAAAAAA8`), `DateFrom`, `DateTo`, `Title` | unique `Number`, unique `SiteCode` |
| `Factions` (канон) | `Code` (`ER`, `KPRF`, `LDPR`, `SR`, `NL`, `INDEPENDENT`, …), `Title` | unique `Code` |
| `FactionPeriods` | `FactionId`, `SiteId` (bigint, `72100024`), `RowId` (smallint identity; в сиде фиксирован 1..N, ADR-14), `Title` («ЕР», «СР-Р»), `ConvocationId`, `DateFrom`, `DateTo` | unique (`SiteId`, `ConvocationId`); unique `RowId`; индекс `FactionId` |
| `Deputies` | `SiteId` (bigint), `RowId` (int identity, ADR-14), `FullName`, `SortName` | unique `SiteId`; unique `RowId`; индекс `FullName` |
| `DeputyFactionMemberships` | `DeputyId`, `FactionPeriodId`, `DateFrom`, `DateTo` (по данным голосований) | индекс (`DeputyId`, `FactionPeriodId`) |
| `Bills` | `Number` (`500091-8`), `SozdUrl` (вычисляемая ссылка) | unique `Number` |
| `Votes` | `SiteId` (bigint, id сайта), `RowId` (int identity, ADR-14), `Title`, `TitleSearch` (tsvector, generated), `VotedAt` (timestamptz), `UtcOffsetMinutes`, `ConvocationId`, `BillId` (null), `Stage` (enum), `StageRaw`, `Outcome` (enum), `AcceptCountTotal`, `DeclineCountTotal`, `AbstainCountTotal`, `NoneCountTotal`, `AcceptPercentSite` (null), `HasResults`, `Source` (archive/site) | unique `SiteId`; unique `RowId`; GIN `TitleSearch`; btree `VotedAt`, `ConvocationId`, `BillId`, `Stage`; составной (`ConvocationId`, `VotedAt`) |
| `DeputyVotes` | `VoteId` (int → `Votes.RowId`), `DeputyId` (int → `Deputies.RowId`), `FactionPeriodId` (smallint? → `FactionPeriods.RowId`), `Result` (enum: accept/decline/abstain/none) — ADR-14 | PK (`VoteId`, `DeputyId`); индекс `DeputyId`; индекс (`VoteId`, `FactionPeriodId`); индекс `FactionPeriodId` |
| `VoteFactionStats` | `VoteId`, `FactionPeriodId`, `FactionId` (денормализация для фильтра по канону), `AcceptCount`, `DeclineCount`, `AbstainCount`, `NoneCount`, `MemberCount`, `VotedCount`, `AcceptPctMembers`, `DeclinePctMembers`, `AbstainPctMembers`, `NonePctMembers`, `AcceptPctVoted`, `DeclinePctVoted`, `AbstainPctVoted` (numeric(5,2), null при нулевом знаменателе; «не голосовал» — только база members) | PK (`VoteId`, `FactionPeriodId`); 7 индексов (`FactionId`, <процент>) — по одному на каждый результат/базу, используемый в фильтрах |
| `ImportQuarantine` | `SourceFile`/`SourceUrl`, `SiteId`, `Reason`, `RawJson`, `CreatedAt` | индекс `CreatedAt` |

Теги/рейтинги (v1.1/v1.2): `Tags` (slug unique, title, category, sentiment, description), `VoteTags`/`BillTags` (PK vote/bill+tag, source, status, assignedBy, createdAt), `TagRules` (tagId, pattern, field, enabled), `BillImportances` (billId PK, expertScore, publicScore, finalScore, votesCount), `ImportanceVotes` (PK bill+user, score 1–5).

Пользователи и роли (база закладывается на Э1, включается на Э9; подробно — [07-security.md](07-security.md)):

| Таблица | Ключевые поля | Индексы |
|---|---|---|
| `Users` | `Login` (+ `LoginLowerCase`), `Email` (+ `EmailLowerCase`), `Name`, `PasswordHash`, `PasswordSalt`, `PasswordIsTemporary`, `SecurityStamp`, `RoleId`, `Blocked`, `LoginAttemptFailedCount`, `Note`, аудит | unique `LoginLowerCase`, unique `EmailLowerCase`; индекс `RoleId` |
| `Roles` | `Name` (User/Moderator/Administrator), `Description` | unique `Name` |
| `RefreshTokens` | `UserId`, `TokenHash`, `ExpiresAt`, `CreatedAt`, `RevokedAt` (ротация: старый отзывается при выпуске нового) | индекс `UserId`; unique `TokenHash` |

Модель ролей — **одна роль на пользователя** (FK `Users.RoleId`), как в DocumentGenerator master (`UserRole`-справочник + seed `UserRolesDefault` с фиксированными GUID). Иерархия прав: Пользователь ⊂ Модератор ⊂ Администратор; проверяется политиками/`RoleHelper`, не размазанными `if`. Множественные роли на пользователя — отложено (при необходимости заменяется на связь `*──*` без слома API).

### 3.3. Объёмы и сайзинг

- `Votes`: ~120 тыс. строк после добора сайта (102 320 архив + ~17 тыс. сайт).
- `DeputyVotes`: ~53 млн строк (45,6 млн архив + добор). При ~40 байт/строку + индексы — порядка 4–6 ГБ. PostgreSQL на типовой машине (16 ГБ RAM) справляется; горячий путь поиска таблицу не трогает.
- `VoteFactionStats`: ~600 тыс. строк (≤ 6 фракций × 120 тыс.) — горячая таблица фильтров, целиком помещается в память.
- Пакетная вставка: `NpgsqlBinaryImporter` (COPY) для `DeputyVotes`/`VoteFactionStats` при первичном импорте; обычные транзакции по 500 голосований для справочников и upsert `Votes`.

### 3.4. Поиск: реализация фильтров

- FTS: `TitleSearch` — генерируемая колонка `to_tsvector('russian', "Title")`, GIN-индекс; запрос — `websearch_to_tsquery('russian', @q)`, ранжирование `ts_rank`.
- Фракционное условие `(faction, result, op, threshold, basis)` → EXISTS-подзапрос к `VoteFactionStats` по `FactionId` и соответствующей колонке процента:

```sql
AND EXISTS (SELECT 1 FROM "VoteFactionStats" s
            WHERE s."VoteId" = v."Id" AND s."FactionId" = @f
              AND s."AcceptPctMembers" >= @threshold)
```

- Несколько условий — несколько EXISTS (И). Проценты предпосчитаны → фильтр не сканирует `DeputyVotes`.
- Пагинация — `ORDER BY … LIMIT/OFFSET` с составным индексом под сортировку; при глубоких страницах — keyset (на этапе оптимизации, если p95 не достигает цели).

## 4. Парсер и импорт (компоненты)

### 4.1. `DumaLens.Parsing`

- `VotePageParser`: HTML → `ParsedVote` (id, title, datetime+смещение, список `ParsedDeputyVote`). Извлечение `deputiesData` — regex `deputiesData\s*=\s*(\[.*?\]);` (DOTALL) + `System.Text.Json`; ФИО — `sortName` (приоритет) либо очистка `name` (AngleSharp); id депутата — из `url` (`deputy=(\d+)`); маппинг `for/against/abstain/absent → accept/decline/abstain/none`.
- `VoteListingParser`: HTML листинга → элементы (id, title, дата группы, время, итог «Принят/Отклонён», счёт За) + ссылки пагинации. AngleSharp.
- `TitleNormalizer`: номер законопроекта (regex `№\s*(\d{4,7}-\d)`), стадия (словарь вариантов, §04), очистка HTML-сущностей/пробелов. Чистая функция, полностью покрыта тестами на реальных заголовках архива.
- Валидация разбора: непустой title, дата в диапазоне созыва, известные коды результатов, число депутатов 0 или 430–460; нарушения → `ImportQuarantine`.

### 4.2. `DumaLens.Importer` (CLI)

- `import-archive`: потоковое чтение ZIP (`ZipArchive`, последовательно), батчи по 500 голосований, upsert по `SiteId`, COPY для дочерних строк, журнал прогресса (файл + таблица), `--resume` (пропуск обработанных id), построение справочников (депутаты, фракционные периоды, законопроекты) и агрегатов на лету. Финал — `verify`.
- `crawl`: обход листингов созыва (`?convocation=…&page=N`, 20/стр), сбор итогов, очередь карточек (пропуск уже импортированных с `Source=site` и актуальным составом), пауза ≥ 0,5–1 с, backoff при 5xx (3 ретрая, затем классификация «id отсутствует» для устойчивых 500), журнал прогресса, возобновление. Порядок: сначала листинги всех созывов (итоги + обнаружение пробелов), затем карточки недостающих.
- `verify`: пересчёт контрольных сумм (голосования/голоса депутатов по созывам, коды результатов) и сравнение с паспортом данных (02, §2.2) + отчёт о расхождениях и карантине.
- `recalc-stats`: пересчёт `VoteFactionStats` из `DeputyVotes` (после карантина/исправлений).

### 4.3. Хосты

- **Api** (`DumaLens.Api`) — REST, только чтение данных голосований (MVP) + auth-эндпоинты и админ-операции (Э9+). Импорт в хосте не выполняется (НФТ-09): CLI запускается на сервере отдельно (вручную/планировщиком). Фоновый `IHostedService`-кроулер — опция v1.1 (конфигурируемое расписание), не MVP.
- **Web** (`DumaLens.Web`) — Blazor Server-фронтенд, ходит в Api по HTTP через NSwag-клиент. Состояние авторизации — `ClientTokenStore` (ProtectedLocalStorage) + `UserState` + `AuthDelegatingHandler` (проброс access-токена, авто-refresh при 401) — механика DocumentGenerator.Web.

### 4.4. Аутентификация, авторизация и роли (база)

Полная спецификация — [07-security.md](07-security.md). Кратко:

- **Роли**: Пользователь / Модератор / Администратор (`Role` enum + справочник `Roles`, seed). Иерархия прав; одна роль на пользователя (как DG master).
- **Аутентификация**: JWT access + refresh (HttpOnly-cookie для браузерного потока по образцу Specular; для Blazor Server — токен-стор на сервере, как DG). Пароли — **PBKDF2 + соль** (как оба эталона, но ≥ 100 000 итераций: в эталонах `MaximumIterations = 6` — это слабо, осознанное отступление, ADR-12).
- **Отзыв сессий**: `SecurityStamp` пользователя в клеймах + проверка при refresh (Specular); refresh-токены хранятся в БД с ротацией.
- **Авторизация**: `[Authorize(Roles = "Administrator")]` / политики иерархии на эндпоинтах Api; `AuthorizeView` в Web. Публичные эндпоинты поиска/карточек — `[AllowAnonymous]`.
- **ASP.NET Core Identity не используется** — своя схема по эталонам (ADR-11). База закладывается на Э1 (сущности+seed), включается на Э9 до тегов (v1.1), чтобы ФТ-41/ФТ-51 опирались на готовую систему.

## 5. Развёртывание

- Целевая среда: Windows + PostgreSQL 16+ (проверено на 17; строка подключения — через переменную окружения, в репозиторий не попадает — О-7) + .NET 8 Runtime (ASP.NET Core Runtime). Публикация двух хостов: `dotnet publish DumaLens.Api -c Release` и `dotnet publish DumaLens.Web -c Release` (или профили VS 2022), запуск как консоль/служба на разных портах; строка подключения — `appsettings.json` + переменная окружения `ConnectionStrings__DefaultConnection`. Web знает базовый URL Api через конфигурацию (`ApiBaseUrl`).
- Миграции: `dotnet ef database update` (инструмент `dotnet-ef` 8.x; на машине разработчика стоит 10.x — для проекта использовать локальный инструмент 8.x в `.config/dotnet-tools.json`, чтобы не тянуть EF10-дизайнер к net8).
- Первичное наполнение: `dumalens import-archive --zip vote-master.zip`, затем `dumalens crawl --convocation 1…9`, затем `dumalens verify`.
- Инструкция развёртывания — отдельный документ `Docs/08-deployment.md` (номер сдвинут: 07 занят спецификацией безопасности).

## 6. Архитектурные решения (ADR)

| № | Решение | Обоснование / альтернативы |
|---|---|---|
| ADR-1 | **PostgreSQL 16+ вместо SQL Server** эталона; FTS средствами PostgreSQL, без Meilisearch/OpenSearch на MVP | Эталон (DG master) остаётся на SQL Server (`UseSqlServer` в `Program.cs`); Npgsql есть только в его ветке `postgres_old` — переход на PostgreSQL является самостоятельным решением проекта. 120 тыс. документов с русским FTS — комфортный объём для PostgreSQL; отдельный движок = лишний сервис в целевой среде (О-5). Альтернативы (Meilisearch, OpenSearch) отложены; абстракция поиска в сервисе позволит заменить без переписывания API |
| ADR-2 | **Потоковые таблицы без аудита/soft-delete** (`DeputyVotes` — составной int-PK по ADR-14, `VoteFactionStats` — bigint identity) | 45,6+ млн иммутабельных строк; аудит и soft-delete здесь — мёртвый вес. Справочники и пользовательские сущности — с аудитом, как эталон. Отступление от `DbBaseEntity` осознанное, фиксируется в ревью как принятое |
| ADR-3 | **Интеграционные тесты на PostgreSQL** (одноразовая база `dumalens_{Guid}`), не LocalDB | LocalDB — Windows-only и SQL Server; проект на Npgsql. Требуется доступный PostgreSQL на машине разработки/CI; при отсутствии — тесты помечаются пропущенными явно, не «зелёными» |
| ADR-4 | **Два хоста: `DumaLens.Api` (REST) + `DumaLens.Web` (Blazor Server)**, без Node.js; Web ходит в Api только по HTTP через NSwag-клиент | Требование проекта: Web — фронтенд, Api — REST, «Web как API» некорректно. Blazor Server — как DocumentGenerator.Web (net8, без WASM-загрузки и Node); Specular.Portal (Blazor WASM) — ориентир на будущее. Жёсткая граница позволяет заменить UI без изменения Api |
| ADR-5 | **Импорт — отдельный CLI-проект**, не фоновая задача веб-хоста | Разделение ответственности: веб — только чтение; импорт запускается вручную/планировщиком, не блокирует запросы, не требует авторизации в API. `IHostedService`-кроулер — опция v1.1 |
| ADR-6 | **File-scoped namespaces и CRLF** — по `.editorconfig` DocumentGenerator **master** (`file_scoped:suggestion`) и Specular | Прежняя редакция ссылалась на block_scoped устаревшей локальной ветки `initial`; актуальный master — file-scoped. Правило: стиль читать из актуальной ветки эталона (master), не из локальной копии |
| ADR-7 | **Агрегаты предпосчитаны при импорте** (`VoteFactionStats`), а не считаются на лету | Требование p95 < 1 с по 120 тыс. голосований; пересчёт на лету сканировал бы 53 млн строк. Пересчёт — команда `recalc-stats` |
| ADR-8 | **Канонические фракции + исторические периоды** вместо «фракция = site-id» | Site-id фракции меняется между созывами (СР: …25 → …27 → …29), названия меняются; фильтр «КПРФ» должен работать сквозь всю историю. Аналогично депутаты: один SiteId на человека между созывами |
| ADR-9 | **Итог голосования — только факт с сайта**; расчётный итог не вычисляется на MVP | Требуемое большинство зависит от типа вопроса (226/300/450 голосов); без справочника типов расчёт был бы догадкой. Поле `ComputedOutcome` зарезервировано |
| ADR-10 | **Теги/рейтинги — после MVP** (v1.1/v1.2); **база ролей и аутентификации — в MVP** (Э9), до тегов | Теги требуют модерации (роль Модератор), рейтинги — пользователей (роль Пользователь); поэтому auth-база вводится раньше, на Э9. Детальная проработка ролей — по мере появления функций (база закладывается заранее) |
| ADR-11 | **Своя схема пользователей/ролей вместо ASP.NET Core Identity** — по обоим эталонам | Оба эталона используют собственные `User`/`Role`/refresh-сущности и PBKDF2; Identity тянет свою схему таблиц и cookie-конвейер, не нужный при JWT + NSwag-клиенте. База: одна роль на пользователя (DG master), иерархия прав — политиками |
| ADR-12 | **PBKDF2-SHA256 + соль 32 байта, ≥ 100 000 итераций** | Алгоритм как в обоих эталонах (`SecurityHelper`, `Rfc2898DeriveBytes.Pbkdf2`), но в эталонах `MaximumIterations = 6` — криптографически слабо; итерации поднимаем до ≥ 100 тыс. (рекомендация OWASP). Осознанное отступление от эталона, фиксируется в ревью как принятое |
| ADR-13 | **JWT access (JWS) + refresh с ротацией и SecurityStamp**; HttpOnly-cookie — для браузерных клиентов | Specular: refresh — JWT, cookie HttpOnly/Secure/SameSite, отзыв через SecurityStamp, JWS без шифрования (клиент читает клеймы). DG: refresh — запись в БД, защищённая DataProtection, один активный на пользователя, JWE-шифрование access. Берём гибрид: refresh-запись в БД (отзыв/ротация) + JWS-подпись access (без шифрования — Web читает клеймы); Blazor Server хранит токены в ProtectedLocalStorage (DG) |
| ADR-14 | **Узкие int-ключи в `DeputyVotes` вместо GUID FK**: `VoteId`/`DeputyId` int, `FactionPeriodId` smallint?; `RowId` identity на `Votes`/`Deputies` (int) и `FactionPeriods` (smallint, в сиде фиксирован 1..N); GUID справочников остаются PK, FK через `HasPrincipalKey(RowId)` | Замер прототипа на полном архиве (45,6 млн голосов, verify 24/24): БД 8734→4277 MB (−51%), `DeputyVotes` 8229→3818 MB (heap −47,5%, PK −72,9%); вторичные индексы почти не уменьшаются — B-tree deduplication при малом числе уникальных ключей (2321 депутат / 60 периодов). COPY голосов 2,3× быстрее (107 с против 244 с). Цена: миграция `IntKeysDeputyVotes` пересоздаёт `DeputyVotes` (данные восстанавливаются реимпортом архива, `Deploy\transition-int-keys.cmd`); сид периодов дополнительно фиксирует `RowId`. Уникальность `RowId` обеспечивается только альтернативным ключом (AK от `HasPrincipalKey`), без дублирующего уникального индекса: миграция `DropRowIdDuplicateIndexes` удаляет `IX_*_RowId`, FK опирается на индекс AK. Альтернативы (оставить GUID, сжатие `Votes`, native ENUM) отклонены |

## 7. Ревью и контроль качества

Каждый этап плана завершается:

1. `dotnet build DumaLens.sln -t:Rebuild -clp:NoSummary` — 0 ошибок, новые warning'и разобраны; `dotnet list package --vulnerable --include-transitive`.
2. `dotnet test DumaLens.sln` — все тесты зелёные (с учётом ADR-3: недоступный PostgreSQL = явный skip, не молчаливый успех).
3. Ревью кода: механический скан + слой за слоем + тесты + стиль; блокирующие замечания устраняются до перехода к следующему этапу.
4. `dotnet format --verify-no-changes` по `.editorconfig`.
5. После сборки — `dotnet build-server shutdown` (висящие MSBuild-ноды).