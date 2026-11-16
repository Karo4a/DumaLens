using DumaLens.Common.Contracts;
using System.Text;

namespace DumaLens.Importer.Import;

/// <summary>
/// Журнал импорта: пишет строки одновременно в stdout и в файл (если задан)
/// </summary>
public sealed class ImportLog : IDisposable
{
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly StreamWriter? writer;
    private readonly object writeLock = new();

    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="dateTimeProvider">Поставщик времени</param>
    /// <param name="filePath">Путь к файлу журнала; null — только stdout</param>
    public ImportLog(IDateTimeProvider dateTimeProvider, string? filePath)
    {
        this.dateTimeProvider = dateTimeProvider;
        if (filePath != null)
        {
            writer = new StreamWriter(filePath, append: false, Encoding.UTF8)
            {
                AutoFlush = true,
            };
        }
    }

    /// <summary>
    /// Записать информационное сообщение
    /// </summary>
    public void Info(string message)
    {
        Write("INFO", message);
    }

    /// <summary>
    /// Записать сообщение об ошибке
    /// </summary>
    public void Error(string message)
    {
        Write("ERROR", message);
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        writer?.Dispose();
    }

    private void Write(string level, string message)
    {
        var line = $"{dateTimeProvider.UtcNow():yyyy-MM-dd HH:mm:ss} UTC [{level}] {message}";
        lock (writeLock)
        {
            Console.WriteLine(line);
            writer?.WriteLine(line);
        }
    }
}