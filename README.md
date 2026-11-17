# DumaLens

Поисковая система и аналитика голосований Государственной Думы РФ: **102 320 голосований за 1994–2024 годы** (~45,6 млн индивидуальных голосов депутатов) из открытого архива (до 13.03.2024) + пополнение **до наших дней** с живого сайта [vote.duma.gov.ru](http://vote.duma.gov.ru), включая IX созыв (с 30.09.2026).

Ключевая возможность — поиск голосований по **позициям фракций** с настраиваемыми порогами и базой расчёта процента: например, «Новые люди — ЗА ≥ 70 % от состава, КПРФ — ПРОТИВ ≥ 50 % от проголосовавших». Плюс полнотекстовый поиск по заголовкам, фильтры (созыв, даты, номер законопроекта, стадия, итог) и карточки голосований, законопроектов, фракций и депутатов.

## Статус

MVP в разработке. Выполнены этапы **Э0–Э7**: каркас решения, модель данных и справочники, парсеры и нормализация, потоковый импорт архива, фракционные агрегаты, полнотекстовый поиск, REST API с карточками, кроулер живого сайта. Впереди: Blazor-интерфейс (Э8), аутентификация и роли (Э9), производительность и развёртывание (Э10). Web-хост пока заглушка.

План этапов, архитектурные решения (ADR) и отчёты приёмки — в [Docs/](Docs/README.md).

## Стек

- **.NET 8 / C# 12**, два независимых хоста: `DumaLens.Api` (ASP.NET Core REST) и `DumaLens.Web` (Blazor Server); Web обращается к Api только по HTTP через NSwag-клиент `DumaLens.Api.Client`
- **PostgreSQL 16+** + EF Core 8 (Npgsql); полнотекстовый поиск средствами PostgreSQL (русская конфигурация, префиксные лексемы)
- AngleSharp (парсинг HTML), System.CommandLine (CLI импортёра), AutoMapper, FluentValidation
- xUnit + FluentAssertions + Moq; интеграционные тесты на одноразовых PostgreSQL-базах

## Требования

- .NET 8 SDK (`global.json` пинит 8.0.x)
- PostgreSQL 16+ (проверено на 17), учётная запись с правами создания баз
- Windows или Linux; скрипты `Deploy/` — Windows (cmd), CLI импортёра кроссплатформенный

## Данные

Исторический архив — открытые данные репозитория [data-dumaGovRu/vote](https://github.com/data-dumaGovRu/vote) (коммит `b9f6118` от 16.03.2024):

1. Скачать ZIP-архив репозитория: <https://github.com/data-dumaGovRu/vote/archive/refs/heads/master.zip> (~851 МБ; внутри — `vote-master/poll/*.json`).
2. Сохранить как `vote-master.zip` в корень репозитория (файл исключён из git).

Живой сайт `vote.duma.gov.ru` пополняет базу кроулером: листинги созывов I–IX, карточки новых голосований, добор итогов и процента «За» у архивных. Архив обрывается 13.03.2024 и имеет пробелы в концах VI и VII созывов — кроулер закрывает эти пробелы и доводит базу до наших дней; сайт обновляется ежедневно, периодические запуски поддерживают данные актуальными. Кроулер читает `robots.txt` и соблюдает паузы вежливости. Паспорт архива с контрольными суммами — [Docs/02-data-sources.md](Docs/02-data-sources.md).

## Быстрый старт

```bash
git clone https://github.com/Karo4a/DumaLens.git
cd DumaLens
dotnet restore
```

Строка подключения задаётся переменной окружения (в `appsettings.json` — только плейсхолдер `CHANGE_ME`):

```bat
:: Windows (cmd)
set ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=dumalens;Username=postgres;Password=***
```

```bash
# Linux / macOS
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=dumalens;Username=postgres;Password=***"
```

Импорт архива (база `dumalens` создаётся и мигрируется автоматически, psql/dotnet-ef не нужны):

```bat
:: Windows — сборка при необходимости → импорт --bulk → сверка с паспортом
Deploy\import.cmd
```

```bash
# Кроссплатформенно
dotnet run --project DumaLens.Importer -c Release -- import-archive --zip vote-master.zip --bulk
dotnet run --project DumaLens.Importer -c Release -- verify
```

Полный импорт занимает ~6 минут на эталонной машине (8 ядер, 32 ГБ, NVMe); `verify` сверяет 24 контрольные суммы с паспортом архива. Bulk-режим требует владения таблицами и пустых потоковых таблиц; при нехватке прав автоматически включается обычный режим.

Запуск хостов:

```bash
dotnet run --project DumaLens.Api    # http://localhost:5200/swagger
dotnet run --project DumaLens.Web    # http://localhost:5100 (заглушка до Э8)
```

Windows-демо одним скриптом: `Deploy\demo.cmd`. Пополнение с живого сайта:

```bash
dotnet run --project DumaLens.Importer -c Release -- crawl --convocation 0 --parallel 8 --delay-ms 500
```

`--convocation 0` — все созывы I–IX. Замеренная рабочая точка: `--parallel 8 --delay-ms 0` (сайт выдерживает и 16 потоков); значения по умолчанию консервативны (`--parallel 1 --delay-ms 1000`). Если HTTPS сайта недоступен в вашей сети, добавьте `--base-url http://vote.duma.gov.ru`. Остановка безопасна в любой момент; повторный запуск добирает недостающее без дубликатов.

## CLI импортёра

Глобальный флаг `--connection` — строка подключения (по умолчанию берётся из `ConnectionStrings__DefaultConnection`).

| Команда | Назначение | Ключевые флаги |
|---|---|---|
| `import-archive` | Потоковый импорт `vote-master.zip` (без распаковки) | `--zip`, `--bulk`, `--resume`, `--batch N` (500), `--workers N` (ядра−2), `--max-files N`, `--log` |
| `verify` | Сверка контрольных сумм базы с паспортом архива | `--report` |
| `crawl` | Обход живого сайта: листинги, добор итогов, новые карточки | `--convocation N` (0 — все), `--from-page N`, `--delay-ms N` (1000), `--parallel N` (1), `--batch N` (100), `--max-cards N`, `--base-url`, `--log` |
| `recalc-stats` | Полный пересчёт фракционных агрегатов из индивидуальных голосов (ремонтный инструмент) | — |
| `restore-schema` | Довосстановление индексов/FK после аварийного bulk-импорта без реимпорта | — |

## API

Swagger UI: `http://localhost:5200/swagger` (среда Development). Эндпоинты: поиск и карточка голосования (`GET /api/votes`, `GET /api/votes/{id}`), законопроекты, фракции, депутаты, созывы, сводная статистика. Enum-значения передаются строковыми токенами; ошибки валидации — 422.

Пример поиска (проверено на полном архиве: 530 совпадений, включая все чтения законопроекта 500091-8):

```
GET /api/votes?q=алкоголь&pageSize=20
```

Фракционные условия, фильтры и сортировки — в Swagger и [Docs/04-functionality.md](Docs/04-functionality.md). Производительность на полном архиве (замеры приёмки): p95 поиска ~158 мс, карточки голосования ~18–30 мс.

NSwag-клиент `DumaLens.Api.Client` генерируется из спеки запущенного Api (`http://localhost:5200/swagger/v1/swagger.json`): `dotnet tool restore`, затем из каталога `DumaLens.Api.Client` — `dotnet nswag run ClientOptions.nswag`.

## Тесты

```bash
dotnet test DumaLens.sln
```

Интеграционные тесты создают одноразовые PostgreSQL-базы (шаблон `dumalens_{0}`); строка подключения — `ConnectionStrings__IntegrationConnection` (переменная окружения или `appsettings.integration.json`, плейсхолдер `CHANGE_ME`). Без доступного PostgreSQL интеграционные тесты явно пропускаются (ADR-3), юнит-тесты выполняются всегда.

Полный локальный гейт: `build.cmd` (restore → rebuild → test → `dotnet format --verify-no-changes` → build-server shutdown).

## Структура решения

28 проектов, строгие слои без обратных ссылок: `Api → Services → Repositories → Context → Entities`; `Web → Api.Client → Api.Models`; `Importer → Services / Parsing / Context`. Контракты вынесены в `*.Contracts`, константы валидации — в `DumaLens.Entities.ValidationConstants`. Подробно — [Docs/05-architecture.md](Docs/05-architecture.md).

## Документация

[Docs/](Docs/README.md) — замысел и ТЗ, паспорт данных с контрольными суммами, функциональность, архитектура и ADR, план этапов Э0–Э13, отчёты приёмки Э5–Э7, разворачивание без прав администратора. Конвенции для разработчиков и AI-агентов — [AGENTS.md](AGENTS.md).

## Лицензия

Код распространяется по лицензии **MIT** — см. [LICENSE](LICENSE). Данные голосований получены из открытых источников (vote.duma.gov.ru, архив data-dumaGovRu/vote), в состав репозитория не входят и остаются собственностью их правообладателей.