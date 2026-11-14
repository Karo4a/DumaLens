using Xunit;

namespace DumaLens.Api.Tests.Infrastructure;

/// <summary>
/// Коллекция интеграционных тестов с общей фикстурой API
/// </summary>
[CollectionDefinition(nameof(DumaLensCollection))]
public class DumaLensCollection : ICollectionFixture<DumaLensApiFixture>
{
}