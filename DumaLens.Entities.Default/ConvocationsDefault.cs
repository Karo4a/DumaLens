using DumaLens.Entities;

namespace DumaLens.Entities.Default;

/// <summary>
/// Стандартные созывы Государственной Думы (I–IX)
/// </summary>
/// <remarks>
/// Границы созывов — фактические, по паспорту данных (02-data-sources.md, §3.2).
/// Идентификаторы фиксированы: seed применяется через HasData и не должен меняться между миграциями.
/// </remarks>
public class ConvocationsDefault
{
    /// <summary>
    /// Сущности стандартных созывов
    /// </summary>
    public static List<Convocation> Entities = new List<Convocation>()
    {
        new Convocation()
        {
            Id = Guid.Parse("519949c4-5bb5-4a77-a963-81ba6fa39ab8"),
            Number = 1,
            SiteCode = "AAAAAAA1",
            Title = "I созыв",
            DateFrom = new DateOnly(1994, 1, 11),
            DateTo = new DateOnly(1995, 12, 31),
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Convocation()
        {
            Id = Guid.Parse("64e17e07-f6c0-4479-b5d3-795ebb36d0aa"),
            Number = 2,
            SiteCode = "AAAAAAA2",
            Title = "II созыв",
            DateFrom = new DateOnly(1996, 1, 1),
            DateTo = new DateOnly(1999, 12, 31),
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Convocation()
        {
            Id = Guid.Parse("8be015c9-1873-4013-b78d-958798a9b76f"),
            Number = 3,
            SiteCode = "AAAAAAA3",
            Title = "III созыв",
            DateFrom = new DateOnly(2000, 1, 1),
            DateTo = new DateOnly(2003, 12, 31),
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Convocation()
        {
            Id = Guid.Parse("78952456-8142-4c5a-b63e-b0cb6dc754ea"),
            Number = 4,
            SiteCode = "AAAAAAA4",
            Title = "IV созыв",
            DateFrom = new DateOnly(2004, 1, 1),
            DateTo = new DateOnly(2007, 12, 31),
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Convocation()
        {
            Id = Guid.Parse("c2c92ca1-cb8e-4aed-9e3f-ab00b1b47ae4"),
            Number = 5,
            SiteCode = "AAAAAAA5",
            Title = "V созыв",
            DateFrom = new DateOnly(2008, 1, 1),
            DateTo = new DateOnly(2011, 12, 20),
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Convocation()
        {
            Id = Guid.Parse("56d58d7a-467f-48be-b236-42b4d133fd02"),
            Number = 6,
            SiteCode = "AAAAAAA6",
            Title = "VI созыв",
            DateFrom = new DateOnly(2011, 12, 21),
            DateTo = new DateOnly(2016, 10, 4),
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Convocation()
        {
            Id = Guid.Parse("45af1ff7-eacb-4b06-8529-ff46c1afa5fa"),
            Number = 7,
            SiteCode = "AAAAAAA7",
            Title = "VII созыв",
            DateFrom = new DateOnly(2016, 10, 5),
            DateTo = new DateOnly(2021, 10, 11),
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Convocation()
        {
            Id = Guid.Parse("f93db281-114d-4f32-b4bb-c3394d7423fb"),
            Number = 8,
            SiteCode = "AAAAAAA8",
            Title = "VIII созыв",
            DateFrom = new DateOnly(2021, 10, 12),
            DateTo = new DateOnly(2026, 9, 29),
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Convocation()
        {
            Id = Guid.Parse("f7e48a00-05ce-43d3-b7e4-bf206ff43efa"),
            Number = 9,
            SiteCode = "AAAAAAA9",
            Title = "IX созыв",
            DateFrom = new DateOnly(2026, 9, 30),
            DateTo = null,
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
    };

    /// <summary>
    /// Возвращает фиксированный идентификатор созыва по его номеру
    /// </summary>
    /// <param name="number">Номер созыва (1–9)</param>
    /// <returns>Идентификатор созыва или <see cref="Guid.Empty"/>, если номер неизвестен</returns>
    public static Guid DefaultConvocationToGuid(int number)
    {
        switch (number)
        {
            case 1: return Guid.Parse("519949c4-5bb5-4a77-a963-81ba6fa39ab8");
            case 2: return Guid.Parse("64e17e07-f6c0-4479-b5d3-795ebb36d0aa");
            case 3: return Guid.Parse("8be015c9-1873-4013-b78d-958798a9b76f");
            case 4: return Guid.Parse("78952456-8142-4c5a-b63e-b0cb6dc754ea");
            case 5: return Guid.Parse("c2c92ca1-cb8e-4aed-9e3f-ab00b1b47ae4");
            case 6: return Guid.Parse("56d58d7a-467f-48be-b236-42b4d133fd02");
            case 7: return Guid.Parse("45af1ff7-eacb-4b06-8529-ff46c1afa5fa");
            case 8: return Guid.Parse("f93db281-114d-4f32-b4bb-c3394d7423fb");
            case 9: return Guid.Parse("f7e48a00-05ce-43d3-b7e4-bf206ff43efa");
            default: return Guid.Empty;
        }
    }
}