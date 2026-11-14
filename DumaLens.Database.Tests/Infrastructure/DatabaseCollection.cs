using Xunit;

namespace DumaLens.Database.Tests.Infrastructure;

/// <summary>
/// Коллекция тестов с общей одноразовой базой PostgreSQL
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}