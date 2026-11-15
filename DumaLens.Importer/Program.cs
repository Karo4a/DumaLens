using DumaLens.Importer.Import;
using DumaLens.Importer.Stats;
using DumaLens.Importer.Verify;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace DumaLens.Importer;

/// <summary>
/// CLI импорта данных: команды import-archive, verify и recalc-stats
/// </summary>
internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        var connectionOption = new Option<string?>("--connection", "Строка подключения PostgreSQL; по умолчанию — переменная окружения ConnectionStrings__DefaultConnection");

        var root = new RootCommand("DumaLens: импорт архива голосований Госдумы, сверка контрольных сумм и пересчёт фракционных агрегатов");
        root.AddGlobalOption(connectionOption);

        var zipOption = new Option<FileInfo>("--zip", "Путь к архиву vote-master.zip")
        {
            IsRequired = true,
        };
        var batchOption = new Option<int>("--batch", () => 500, "Размер пачки голосований в одной транзакции");
        var resumeOption = new Option<bool>("--resume", "Пропускать голосования, уже загруженные в базу (продолжение после сбоя)");
        var logOption = new Option<FileInfo?>("--log", "Файл журнала импорта (дополнительно к stdout)");

        var importCommand = new Command("import-archive", "Потоковый импорт архива vote-master.zip в PostgreSQL");
        importCommand.AddOption(zipOption);
        importCommand.AddOption(batchOption);
        importCommand.AddOption(resumeOption);
        importCommand.AddOption(logOption);
        importCommand.SetHandler(async invocationContext =>
        {
            var cancellationToken = invocationContext.GetCancellationToken();
            var zip = invocationContext.ParseResult.GetValueForOption(zipOption)!;
            var batch = invocationContext.ParseResult.GetValueForOption(batchOption);
            var resume = invocationContext.ParseResult.GetValueForOption(resumeOption);
            var logFile = invocationContext.ParseResult.GetValueForOption(logOption);
            var connection = invocationContext.ParseResult.GetValueForOption(connectionOption);

            if (batch < 1)
            {
                Console.Error.WriteLine("--batch должен быть не меньше 1");
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

        return await root.InvokeAsync(args);
    }
}