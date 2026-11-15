using DumaLens.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DumaLens.Importer.Import;

/// <summary>
/// Управление схемой потоковых таблиц для ускоренного импорта
/// </summary>
/// <remarks>
/// Замеры на архиве (4,45 млн строк DeputyVotes): COPY с индексами и FK-триггерами — ~24 тыс. строк/с,
/// сортированный COPY с отключёнными триггерами — ~80 тыс. строк/с, COPY без вторичных индексов
/// и FK — ~150 тыс. строк/с. Поэтому в обычном режиме FK-триггеры отключаются (если роль — суперпользователь),
/// а в bulk-режиме (<c>--bulk</c>, только пустые таблицы) дополнительно удаляются вторичные индексы и FK.
/// Первичные ключи не удаляются никогда. Все удалённые объекты восстанавливаются в <see cref="RestoreAsync"/>
/// (в том числе при ошибке импорта); после аварийного завершения процесса схема довосстанавливается
/// при следующем запуске через <see cref="PrepareAsync"/>.
/// Определения индексов и FK выводятся из EF-модели (CodeFirst), включая автоматические индексы
/// внешних ключей: жёстких списков нет, новые миграции подхватываются без правок импортёра.
/// Bulk-режим требует владения потоковыми таблицами, отключение триггеров — суперпользователя;
/// при нехватке прав режим мягко деградирует до обычного (актуально для ограниченных ролей).
/// </remarks>
public sealed class BulkLoadSchema
{
    private static readonly string[] StreamTables = ["DeputyVotes", "VoteFactionStats"];

    private readonly DumaLensContext context;
    private readonly ImportLog log;
    private bool bulkActive;
    private bool triggersDisabled;

    /// <summary>
    /// Создаёт менеджер схемы ускоренного импорта
    /// </summary>
    public BulkLoadSchema(DumaLensContext context, ImportLog log)
    {
        this.context = context;
        this.log = log;
    }

    /// <summary>
    /// Готовит схему к импорту: bulk-режим (пустые таблицы) либо отключение FK-триггеров
    /// </summary>
    /// <remarks>
    /// В обычном режиме сначала довосстанавливает объекты, отсутствующие после аварийного
    /// завершения bulk-импорта. Отключение FK-триггеров безопасно: ссылочную целостность
    /// гарантирует сам импортёр (родительские строки сохраняются до COPY в той же транзакции).
    /// Импорт выполняется при остановленном Api: отключение триггеров действует на все сеансы.
    /// </remarks>
    public async Task PrepareAsync(bool bulk, CancellationToken ct)
    {
        // DDL на десятках миллионов строк (CREATE INDEX, проверка FK) длится минуты — дефолтные 30 с не годятся
        context.Database.SetCommandTimeout(TimeSpan.FromMinutes(30));
        if (bulk)
        {
            await EnsureEmptyStreamTablesAsync(ct);
            if (await HasStreamTableOwnershipAsync(ct))
            {
                foreach (var index in GetModelIndexes())
                {
                    var dropIndexSql = "DROP INDEX IF EXISTS \"" + index.Name + "\"";
                    await context.Database.ExecuteSqlRawAsync(dropIndexSql, ct);
                }

                foreach (var foreignKey in GetModelForeignKeys())
                {
                    var dropConstraintSql = "ALTER TABLE " + foreignKey.QualifiedTable + " DROP CONSTRAINT IF EXISTS \"" + foreignKey.Name + "\"";
                    await context.Database.ExecuteSqlRawAsync(dropConstraintSql, ct);
                }

                bulkActive = true;
                log.Info("Bulk-режим: вторичные индексы и FK потоковых таблиц удалены, будут восстановлены после импорта.");
                return;
            }

            log.Info("Bulk-режим недоступен: роль не владеет потоковыми таблицами — продолжаю в обычном режиме.");
        }

        await CreateMissingSchemaObjectsAsync(ct);
        await DisableForeignKeyTriggersAsync(ct);
    }

    /// <summary>
    /// Восстанавливает схему после импорта (в том числе при ошибке): индексы, FK, триггеры
    /// </summary>
    public async Task RestoreAsync(CancellationToken ct)
    {
        context.Database.SetCommandTimeout(TimeSpan.FromMinutes(30));
        if (bulkActive)
        {
            await CreateMissingSchemaObjectsAsync(ct);
            bulkActive = false;
            log.Info("Bulk-режим: вторичные индексы и FK потоковых таблиц восстановлены.");
        }

        if (triggersDisabled)
        {
            foreach (var table in StreamTables)
            {
                var enableSql = "ALTER TABLE public.\"" + table + "\" ENABLE TRIGGER ALL";
                await context.Database.ExecuteSqlRawAsync(enableSql, ct);
            }

            triggersDisabled = false;
            log.Info("FK-триггеры потоковых таблиц включены.");
        }
    }

    private async Task EnsureEmptyStreamTablesAsync(CancellationToken ct)
    {
        foreach (var table in StreamTables)
        {
            var existsSql = "SELECT EXISTS (SELECT 1 FROM public.\"" + table + "\") AS \"Value\"";
            var hasRows = await context.Database
                .SqlQueryRaw<bool>(existsSql)
                .SingleAsync(ct);
            if (hasRows)
            {
                throw new InvalidOperationException(
                    $"Bulk-режим доступен только для пустых таблиц: \"{table}\" не пуста. " +
                    "Используйте обычный режим (при необходимости с --resume).");
            }
        }
    }

    private async Task<bool> HasStreamTableOwnershipAsync(CancellationToken ct)
    {
        // Владелец таблицы или член владеющей роли; суперпользователь проходит проверку автоматически
        var ownershipSql = "SELECT bool_and(pg_has_role(current_user, c.relowner, 'USAGE')) AS \"Value\" " +
            "FROM pg_class c WHERE c.oid IN (" + RegclassList + ")";
        return await context.Database
            .SqlQueryRaw<bool>(ownershipSql)
            .SingleAsync(ct);
    }

    private async Task CreateMissingSchemaObjectsAsync(CancellationToken ct)
    {
        var existingIndexes = await context.Database
            .SqlQueryRaw<string>("SELECT indexname AS \"Value\" FROM pg_indexes WHERE tablename IN (" + TableList + ")")
            .ToListAsync(ct);
        var missingIndexes = GetModelIndexes().Where(x => !existingIndexes.Contains(x.Name)).ToList();
        var existingConstraints = await context.Database
            .SqlQueryRaw<string>("SELECT conname AS \"Value\" FROM pg_constraint WHERE conrelid IN (" + RegclassList + ") AND contype = 'f'")
            .ToListAsync(ct);
        var missingForeignKeys = GetModelForeignKeys().Where(x => !existingConstraints.Contains(x.Name)).ToList();
        if (missingIndexes.Count == 0 && missingForeignKeys.Count == 0)
        {
            return;
        }

        // Сборка индексов на миллионах строк быстрее с большим буфером сортировки;
        // SET действует на сеанс и не требует прав администратора сервера
        await context.Database.ExecuteSqlRawAsync("SET maintenance_work_mem = '256MB'", ct);
        foreach (var index in missingIndexes)
        {
            await context.Database.ExecuteSqlRawAsync(index.CreateSql, ct);
        }

        foreach (var foreignKey in missingForeignKeys)
        {
            await context.Database.ExecuteSqlRawAsync(foreignKey.AddConstraintSql, ct);
        }
    }

    private async Task DisableForeignKeyTriggersAsync(CancellationToken ct)
    {
        var isSuperuser = await context.Database
            .SqlQueryRaw<bool>("SELECT rolsuper AS \"Value\" FROM pg_roles WHERE rolname = current_user")
            .SingleAsync(ct);
        if (!isSuperuser)
        {
            log.Info("Роль не является суперпользователем: FK-триггеры не отключаются, импорт будет медленнее.");
            return;
        }

        foreach (var table in StreamTables)
        {
            var disableSql = "ALTER TABLE public.\"" + table + "\" DISABLE TRIGGER ALL";
            await context.Database.ExecuteSqlRawAsync(disableSql, ct);
        }

        triggersDisabled = true;
        log.Info("FK-триггеры потоковых таблиц отключены на время импорта (целостность гарантирует импортёр).");
    }

    /// <summary>
    /// Вторичные индексы потоковых таблиц из EF-модели (включая автоматические индексы FK); PK исключаются
    /// </summary>
    private List<IndexDefinition> GetModelIndexes()
    {
        var result = new List<IndexDefinition>();
        foreach (var entityType in GetStreamEntityTypes())
        {
            var primaryKey = entityType.FindPrimaryKey();
            foreach (var index in entityType.GetIndexes())
            {
                if (primaryKey != null && index.Properties.SequenceEqual(primaryKey.Properties))
                {
                    continue;
                }

                var columns = new List<string>();
                for (var i = 0; i < index.Properties.Count; i++)
                {
                    var column = "\"" + index.Properties[i].GetColumnName() + "\"";
                    if (index.IsDescending != null && index.IsDescending[i])
                    {
                        column += " DESC";
                    }

                    columns.Add(column);
                }

                var name = index.GetDatabaseName()!;
                var createSql = "CREATE " + (index.IsUnique ? "UNIQUE " : string.Empty) + "INDEX \"" + name + "\" " +
                    "ON " + QualifiedTable(entityType) + " USING btree (" + string.Join(", ", columns) + ")";
                var filter = index.GetFilter();
                if (!string.IsNullOrEmpty(filter))
                {
                    createSql += " WHERE " + filter;
                }

                result.Add(new IndexDefinition(name, createSql));
            }
        }

        return result;
    }

    /// <summary>
    /// Внешние ключи потоковых таблиц из EF-модели
    /// </summary>
    private List<ForeignKeyDefinition> GetModelForeignKeys()
    {
        var result = new List<ForeignKeyDefinition>();
        foreach (var entityType in GetStreamEntityTypes())
        {
            var qualifiedTable = QualifiedTable(entityType);
            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                var name = foreignKey.GetConstraintName()!;
                var columns = string.Join(", ", foreignKey.Properties.Select(x => "\"" + x.GetColumnName() + "\""));
                var principalColumns = string.Join(", ", foreignKey.PrincipalKey.Properties.Select(x => "\"" + x.GetColumnName() + "\""));
                var onDelete = foreignKey.DeleteBehavior switch
                {
                    DeleteBehavior.Cascade => "CASCADE",
                    DeleteBehavior.Restrict => "RESTRICT",
                    DeleteBehavior.SetNull => "SET NULL",
                    _ => "NO ACTION",
                };
                var addConstraintSql = "ALTER TABLE " + qualifiedTable + " ADD CONSTRAINT \"" + name + "\" " +
                    "FOREIGN KEY (" + columns + ") REFERENCES " + QualifiedTable(foreignKey.PrincipalEntityType) +
                    " (" + principalColumns + ") ON DELETE " + onDelete;
                result.Add(new ForeignKeyDefinition(qualifiedTable, name, addConstraintSql));
            }
        }

        return result;
    }

    private IEnumerable<IEntityType> GetStreamEntityTypes()
    {
        // Design-time модель: аннотации вроде IsDescending/Filter отсутствуют в read-optimized runtime-модели
        return context.GetService<IDesignTimeModel>().Model.GetEntityTypes()
            .Where(x => StreamTables.Contains(x.GetTableName()));
    }

    private static string QualifiedTable(IEntityType entityType)
    {
        return (entityType.GetSchema() ?? "public") + ".\"" + entityType.GetTableName() + "\"";
    }

    private static string TableList => string.Join(", ", StreamTables.Select(x => "'" + x + "'"));

    private static string RegclassList => string.Join(", ", StreamTables.Select(x => "'\"" + x + "\"'::regclass"));

    private sealed record IndexDefinition(string Name, string CreateSql);

    private sealed record ForeignKeyDefinition(string QualifiedTable, string Name, string AddConstraintSql);
}