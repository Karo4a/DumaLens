using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DumaLens.Context;

/// <summary>
/// Фабрика для создания контекста в DesignTime
/// </summary>
public class DumaLensDesignTimeDbContextFactory : IDesignTimeDbContextFactory<DumaLensContext>
{
    /// <summary>
    /// Создаёт экземпляр контекста для команд dotnet ef
    /// </summary>
    /// <remarks>
    /// 1) dotnet tool restore
    /// 2) dotnet ef migrations add [name] --project DumaLens.Context
    /// 3) dotnet ef database update --project DumaLens.Context
    /// Строка подключения берётся из переменной окружения ConnectionStrings__DefaultConnection;
    /// в репозитории хранится только плейсхолдер (О-7).
    /// </remarks>
    public DumaLensContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=dumalens;Username=postgres;Password=CHANGE_ME";
        var options = new DbContextOptionsBuilder<DumaLensContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new DumaLensContext(options);
    }
}