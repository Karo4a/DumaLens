using DumaLens.Context;
using DumaLens.Importer.Crawl;
using DumaLens.Importer.Import;
using DumaLens.Importer.Stats;
using DumaLens.Importer.Verify;
using DumaLens.Services.Auth;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace DumaLens.Importer;

/// <summary>
/// CLI импорта данных: команды import-archive, verify, recalc-stats, restore-schema и crawl
/// </summary>
internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        var connectionOption = new Option<string?>("--connection", "Строка подключения PostgreSQL; по умолчанию — переменная окружения ConnectionStrings__DefaultConnection");

        var root = new RootCommand("DumaLens: импорт архива голосований Госдумы, обход живого сайта, сверка контрольных сумм и пересчёт фракционных агрегатов");
        root.AddGlobalOption(connectionOption);

        var zipOption = new Option<FileInfo>("--zip", "Путь к архиву vote-master.zip")
        {
            IsRequired = true,
        };
        var batchOption = new Option<int>("--batch", () => 500, "Размер пачки голосований в одной транзакции");
        var resumeOption = new Option<bool>("--resume", "Пропускать голосования, уже загруженные в базу (продолжение после сбоя)");
        var logOption = new Option<FileInfo?>("--log", "Файл журнала импорта (дополнительно к stdout)");
        var maxFilesOption = new Option<int>("--max-files", () => 0, "Ограничить число обрабатываемых файлов архива (0 — все); для пробных прогонов");
        var bulkOption = new Option<bool>("--bulk", "Bulk-режим для пустых таблиц: вторичные индексы и FK удаляются на время импорта и восстанавливаются после (существенно быстрее)");
        var workersOption = new Option<int>("--workers", () => 0, "Число параллельных задач разбора JSON (0 — автоматически по числу процессоров)");

        var importCommand = new Command("import-archive", "Потоковый импорт архива vote-master.zip в PostgreSQL");
        importCommand.AddOption(zipOption);
        importCommand.AddOption(batchOption);
        importCommand.AddOption(resumeOption);
        importCommand.AddOption(logOption);
        importCommand.AddOption(maxFilesOption);
        importCommand.AddOption(bulkOption);
        importCommand.AddOption(workersOption);
        importCommand.SetHandler(async invocationContext =>
        {
            var cancellationToken = invocationContext.GetCancellationToken();
            var zip = invocationContext.ParseResult.GetValueForOption(zipOption)!;
            var batch = invocationContext.ParseResult.GetValueForOption(batchOption);
            var resume = invocationContext.ParseResult.GetValueForOption(resumeOption);
            var logFile = invocationContext.ParseResult.GetValueForOption(logOption);
            var maxFiles = invocationContext.ParseResult.GetValueForOption(maxFilesOption);
            var bulk = invocationContext.ParseResult.GetValueForOption(bulkOption);
            var workers = invocationContext.ParseResult.GetValueForOption(workersOption);
            var connection = invocationContext.ParseResult.GetValueForOption(connectionOption);

            if (batch < 1)
            {
                Console.Error.WriteLine("--batch должен быть не меньше 1");
                invocationContext.ExitCode = 1;
                return;
            }

            if (maxFiles < 0)
            {
                Console.Error.WriteLine("--max-files должен быть неотрицательным");
                invocationContext.ExitCode = 1;
                return;
            }

            if (workers < 0)
            {
                Console.Error.WriteLine("--workers должен быть неотрицательным");
                invocationContext.ExitCode = 1;
                return;
            }

            if (!zip.Exists)
            {
                Console.Error.WriteLine($"Архив не найден: {zip.FullName}");
                invocationContext.ExitCode = 1;
                return;
            }

            try
            {
                var connectionString = ImporterHost.ResolveConnectionString(connection);
                await using var provider = ImporterHost.BuildServiceProvider(connectionString, logFile?.FullName);
                var importer = provider.GetRequiredService<ArchiveImporter>();
                var options = new ImportOptions
                {
                    ZipPath = zip.FullName,
                    BatchSize = batch,
                    Resume = resume,
                    MaxFiles = maxFiles,
                    Bulk = bulk,
                    Workers = workers,
                };
                await importer.Import(options, cancellationToken);
                invocationContext.ExitCode = 0;
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("Импорт прерван. Повторный запуск с --resume продолжит с необработанных голосований.");
                invocationContext.ExitCode = 1;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Импорт завершился ошибкой: {exception}");
                invocationContext.ExitCode = 1;
            }
        });
        root.AddCommand(importCommand);

        var reportOption = new Option<FileInfo?>("--report", "Файл отчёта сверки (дополнительно к stdout)");
        var verifyCommand = new Command("verify", "Сверка контрольных сумм базы с паспортом архива (Docs/02-data-sources.md, §2.2)");
        verifyCommand.AddOption(reportOption);
        verifyCommand.SetHandler(async invocationContext =>
        {
            var cancellationToken = invocationContext.GetCancellationToken();
            var reportFile = invocationContext.ParseResult.GetValueForOption(reportOption);
            var connection = invocationContext.ParseResult.GetValueForOption(connectionOption);

            try
            {
                var connectionString = ImporterHost.ResolveConnectionString(connection);
                await using var provider = ImporterHost.BuildServiceProvider(connectionString, logFilePath: null);
                var verifier = provider.GetRequiredService<ArchiveVerifier>();
                var report = await verifier.Verify(cancellationToken);
                var text = report.Render();
                Console.WriteLine(text);
                if (reportFile != null)
                {
                    await File.WriteAllTextAsync(reportFile.FullName, text, cancellationToken);
                }

                invocationContext.ExitCode = report.IsSuccess ? 0 : 1;
            }
            catch (OperationCanceledException)
            {
                invocationContext.ExitCode = 1;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Сверка завершилась ошибкой: {exception}");
                invocationContext.ExitCode = 1;
            }
        });
        root.AddCommand(verifyCommand);

        var recalcCommand = new Command("recalc-stats", "Полный пересчёт фракционных агрегатов (VoteFactionStats) из индивидуальных голосов — после исправления данных или карантина");
        recalcCommand.SetHandler(async invocationContext =>
        {
            var cancellationToken = invocationContext.GetCancellationToken();
            var connection = invocationContext.ParseResult.GetValueForOption(connectionOption);

            try
            {
                var connectionString = ImporterHost.ResolveConnectionString(connection);
                await using var provider = ImporterHost.BuildServiceProvider(connectionString, logFilePath: null);
                var recalculator = provider.GetRequiredService<StatsRecalculator>();
                var summary = await recalculator.Recalc(cancellationToken);
                Console.WriteLine($"Пересчитано строк VoteFactionStats: {summary.RowsRebuilt:N0} за {summary.Elapsed:hh\\:mm\\:ss}");
                invocationContext.ExitCode = 0;
            }
            catch (OperationCanceledException)
            {
                invocationContext.ExitCode = 1;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Пересчёт завершился ошибкой: {exception}");
                invocationContext.ExitCode = 1;
            }
        });
        root.AddCommand(recalcCommand);

        var normalizeTitlesCommand = new Command("normalize-titles", "Очистка ведущих служебных маркеров «*»/«**» в заголовках голосований (идемпотентно; новые импорты уже пишут очищенный заголовок)");
        normalizeTitlesCommand.SetHandler(async invocationContext =>
        {
            var cancellationToken = invocationContext.GetCancellationToken();
            var connection = invocationContext.ParseResult.GetValueForOption(connectionOption);

            try
            {
                var connectionString = ImporterHost.ResolveConnectionString(connection);
                await using var provider = ImporterHost.BuildServiceProvider(connectionString, logFilePath: null);
                var renormalizer = provider.GetRequiredService<TitleRenormalizer>();
                var summary = await renormalizer.Run(cancellationToken);
                Console.WriteLine($"Очищено заголовков: {summary.TitlesUpdated:N0} за {summary.Elapsed.TotalSeconds:F1} с");
                invocationContext.ExitCode = 0;
            }
            catch (OperationCanceledException)
            {
                invocationContext.ExitCode = 1;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Очистка заголовков завершилась ошибкой: {exception}");
                invocationContext.ExitCode = 1;
            }
        });
        root.AddCommand(normalizeTitlesCommand);

        var restoreSchemaCommand = new Command("restore-schema", "Довосстановление схемы потоковых таблиц (индексы, FK, триггеры) после аварийного завершения bulk-импорта — без повторного импорта данных");
        restoreSchemaCommand.SetHandler(async invocationContext =>
        {
            var cancellationToken = invocationContext.GetCancellationToken();
            var connection = invocationContext.ParseResult.GetValueForOption(connectionOption);

            try
            {
                var connectionString = ImporterHost.ResolveConnectionString(connection);
                await using var provider = ImporterHost.BuildServiceProvider(connectionString, logFilePath: null);
                var schema = provider.GetRequiredService<BulkLoadSchema>();
                await schema.PrepareAsync(bulk: false, cancellationToken);
                await schema.RestoreAsync(cancellationToken);
                Console.WriteLine("Схема потоковых таблиц восстановлена: отсутствующие индексы и FK созданы, триггеры включены.");
                invocationContext.ExitCode = 0;
            }
            catch (OperationCanceledException)
            {
                invocationContext.ExitCode = 1;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Восстановление схемы завершилось ошибкой: {exception}");
                invocationContext.ExitCode = 1;
            }
        });
        root.AddCommand(restoreSchemaCommand);

        var convocationOption = new Option<int>("--convocation", () => 0, "Номер созыва 1–9 (0 — все созывы)");
        var fromPageOption = new Option<int>("--from-page", () => 1, "Начальная страница листинга (возобновление обхода)");
        var delayOption = new Option<int>("--delay-ms", () => 1000, "Пауза между запросами к сайту, мс (вежливость; рекомендуется >= 500)");
        var crawlBatchOption = new Option<int>("--batch", () => 100, "Размер пачки карточек в одной транзакции");
        var maxCardsOption = new Option<int>("--max-cards", () => 0, "Ограничить число обрабатываемых карточек (0 — все); для пробных прогонов");
        var parallelOption = new Option<int>("--parallel", () => 1, "Число одновременных запросов карточек (1 — последовательно; рекомендуется <= 8)");
        var baseUrlOption = new Option<string>("--base-url", () => CrawlOptions.DefaultBaseUrl, "Базовый URL сайта (для локальных фикстур)");
        var crawlLogOption = new Option<FileInfo?>("--log", "Файл журнала обхода (дополнительно к stdout)");

        var crawlCommand = new Command("crawl", "Обход живого сайта: листинги созывов, добор итога/процента, загрузка новых карточек (Э7)");
        crawlCommand.AddOption(convocationOption);
        crawlCommand.AddOption(fromPageOption);
        crawlCommand.AddOption(delayOption);
        crawlCommand.AddOption(crawlBatchOption);
        crawlCommand.AddOption(maxCardsOption);
        crawlCommand.AddOption(parallelOption);
        crawlCommand.AddOption(baseUrlOption);
        crawlCommand.AddOption(crawlLogOption);
        crawlCommand.SetHandler(async invocationContext =>
        {
            var cancellationToken = invocationContext.GetCancellationToken();
            var convocation = invocationContext.ParseResult.GetValueForOption(convocationOption);
            var fromPage = invocationContext.ParseResult.GetValueForOption(fromPageOption);
            var delayMs = invocationContext.ParseResult.GetValueForOption(delayOption);
            var batch = invocationContext.ParseResult.GetValueForOption(crawlBatchOption);
            var maxCards = invocationContext.ParseResult.GetValueForOption(maxCardsOption);
            var parallel = invocationContext.ParseResult.GetValueForOption(parallelOption);
            var baseUrl = invocationContext.ParseResult.GetValueForOption(baseUrlOption) ?? CrawlOptions.DefaultBaseUrl;
            var logFile = invocationContext.ParseResult.GetValueForOption(crawlLogOption);
            var connection = invocationContext.ParseResult.GetValueForOption(connectionOption);

            if (convocation is < 0 or > 9)
            {
                Console.Error.WriteLine("--convocation должен быть 0 (все созывы) или 1–9");
                invocationContext.ExitCode = 1;
                return;
            }

            if (fromPage < 1)
            {
                Console.Error.WriteLine("--from-page должен быть не меньше 1");
                invocationContext.ExitCode = 1;
                return;
            }

            if (delayMs < 0)
            {
                Console.Error.WriteLine("--delay-ms должен быть неотрицательным");
                invocationContext.ExitCode = 1;
                return;
            }

            if (batch < 1)
            {
                Console.Error.WriteLine("--batch должен быть не меньше 1");
                invocationContext.ExitCode = 1;
                return;
            }

            if (maxCards < 0)
            {
                Console.Error.WriteLine("--max-cards должен быть неотрицательным");
                invocationContext.ExitCode = 1;
                return;
            }

            if (parallel < 1)
            {
                Console.Error.WriteLine("--parallel должен быть не меньше 1");
                invocationContext.ExitCode = 1;
                return;
            }

            try
            {
                var connectionString = ImporterHost.ResolveConnectionString(connection);
                await using var provider = ImporterHost.BuildServiceProvider(connectionString, logFile?.FullName);
                var crawler = provider.GetRequiredService<VoteCrawler>();
                var options = new CrawlOptions
                {
                    BaseUrl = baseUrl,
                    Convocation = convocation,
                    FromPage = fromPage,
                    DelayMs = delayMs,
                    BatchSize = batch,
                    MaxCards = maxCards,
                    Parallel = parallel,
                };
                var summary = await crawler.Crawl(options, cancellationToken);
                Console.WriteLine($"Обход завершён: новых голосований {summary.NewVotes}, добор итога {summary.OutcomeBackfilled}, пропущено {summary.CardsSkipped}, отсутствует {summary.MissingIds}, карантин {summary.Quarantined}; индивидуальных голосов {summary.DeputyVotesImported}, агрегатов {summary.FactionStatsImported}; длительность {summary.Elapsed.TotalSeconds:F0} с.");
                invocationContext.ExitCode = 0;
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("Обход прерван. Повторный запуск пропустит уже загруженные карточки; для листингов используйте --from-page.");
                invocationContext.ExitCode = 1;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Обход завершился ошибкой: {exception}");
                invocationContext.ExitCode = 1;
            }
        });
        root.AddCommand(crawlCommand);

        var adminLoginOption = new Option<string>("--login", "Логин администратора") { IsRequired = true };
        var adminNameOption = new Option<string>("--name", "Отображаемое имя") { IsRequired = true };
        var adminEmailOption = new Option<string?>("--email", "Электронная почта (необязательно)");
        var adminPasswordOption = new Option<string?>("--password", "Временный пароль (по умолчанию генерируется случайно)");
        var createAdminCommand = new Command("create-admin", "Создать администратора: временный пароль, смена обязательна при первом входе (Э9)");
        createAdminCommand.AddOption(adminLoginOption);
        createAdminCommand.AddOption(adminNameOption);
        createAdminCommand.AddOption(adminEmailOption);
        createAdminCommand.AddOption(adminPasswordOption);
        createAdminCommand.SetHandler(async invocationContext =>
        {
            var cancellationToken = invocationContext.GetCancellationToken();
            var login = invocationContext.ParseResult.GetValueForOption(adminLoginOption)!;
            var name = invocationContext.ParseResult.GetValueForOption(adminNameOption)!;
            var email = invocationContext.ParseResult.GetValueForOption(adminEmailOption);
            var password = invocationContext.ParseResult.GetValueForOption(adminPasswordOption);
            var connection = invocationContext.ParseResult.GetValueForOption(connectionOption);

            try
            {
                var connectionString = ImporterHost.ResolveConnectionString(connection);
                await using var provider = ImporterHost.BuildServiceProvider(connectionString, logFilePath: null);
                await using var scope = provider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<DumaLensContext>();
                await context.Database.MigrateAsync(cancellationToken);
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var temporaryPassword = password ?? SecurityHelper.GenerateTemporaryPassword();
                var user = await userService.Create(new CreateUserModel
                {
                    Login = login,
                    Name = name,
                    Email = email,
                    Role = "Administrator",
                    Password = temporaryPassword,
                }, cancellationToken);
                Console.WriteLine($"Администратор создан: {user.Login} ({user.Name})");
                Console.WriteLine($"Временный пароль: {temporaryPassword}");
                Console.WriteLine("Смена пароля обязательна при первом входе.");
                invocationContext.ExitCode = 0;
            }
            catch (OperationCanceledException)
            {
                invocationContext.ExitCode = 1;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"create-admin: {exception.Message}");
                invocationContext.ExitCode = 1;
            }
        });
        root.AddCommand(createAdminCommand);

        return await root.InvokeAsync(args);
    }
}