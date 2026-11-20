using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DumaLens.Services.Infrastructure;

/// <summary>
/// Классификация ошибок PostgreSQL для повторов конкурентных операций
/// </summary>
internal static class PostgresErrors
{
    private const string UniqueViolation = "23505";
    private const string SerializationFailure = "40001";

    /// <summary>
    /// Нарушение уникального индекса (конкурентная вставка)
    /// </summary>
    internal static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException { SqlState: UniqueViolation };

    /// <summary>
    /// Конфликт сериализации (конкурентная транзакция уровня Serializable)
    /// </summary>
    internal static bool IsSerializationFailure(Exception exception)
        => exception is PostgresException { SqlState: SerializationFailure }
            || exception is DbUpdateException { InnerException: PostgresException { SqlState: SerializationFailure } };
}