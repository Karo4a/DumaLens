using DumaLens.Common.Contracts;
using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Entities.ValidationConstants;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Repositories.Contracts.WriteRepositories;
using Microsoft.EntityFrameworkCore;

namespace DumaLens.Importer.Import;

/// <summary>
/// Каталог справочников импорта: кэширует депутатов, фракции, фракционные периоды, законопроекты
/// и членства во фракциях, создаёт недостающие записи и уточняет границы периодов по данным голосований
/// </summary>
public sealed class ReferenceCatalog
{
    private readonly IReader reader;
    private readonly IDeputyWriteRepository deputyWriter;
    private readonly IBillWriteRepository billWriter;
    private readonly IFactionWriteRepository factionWriter;
    private readonly IFactionPeriodWriteRepository factionPeriodWriter;
    private readonly IDeputyFactionMembershipWriteRepository membershipWriter;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly ImportLog log;

    private readonly List<ConvocationDbModel> convocations = [];
    private readonly Dictionary<string, Faction> factionsByCode = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Faction> factionsByTitle = new(StringComparer.Ordinal);
    private readonly Dictionary<long, Deputy> deputiesBySiteId = [];
    private readonly Dictionary<(long SiteId, Guid ConvocationId), FactionPeriod> periodsByKey = [];
    private readonly Dictionary<string, Guid> billIdsByNumber = new(StringComparer.Ordinal);
    private readonly Dictionary<(Guid DeputyId, Guid PeriodId), DeputyFactionMembership> membershipsByKey = [];
    private readonly HashSet<Deputy> dirtyDeputies = [];
    private readonly HashSet<FactionPeriod> dirtyPeriods = [];
    private readonly HashSet<DeputyFactionMembership> dirtyMemberships = [];

    private FactionCanonResolver canonResolver = null!;

    /// <summary>
    /// Конструктор
    /// </summary>
    public ReferenceCatalog(
        IReader reader,
        IDeputyWriteRepository deputyWriter,
        IBillWriteRepository billWriter,
        IFactionWriteRepository factionWriter,
        IFactionPeriodWriteRepository factionPeriodWriter,
        IDeputyFactionMembershipWriteRepository membershipWriter,
        IDateTimeProvider dateTimeProvider,
        ImportLog log)
    {
        this.reader = reader;
        this.deputyWriter = deputyWriter;
        this.billWriter = billWriter;
        this.factionWriter = factionWriter;
        this.factionPeriodWriter = factionPeriodWriter;
        this.membershipWriter = membershipWriter;
        this.dateTimeProvider = dateTimeProvider;
        this.log = log;
    }

    /// <summary>
    /// Загрузить справочники из базы в память
    /// </summary>
    public async Task Load(CancellationToken cancellationToken)
    {
        convocations.AddRange(await reader.Read<Convocation>()
            .NotDeletedAt()
            .OrderBy(x => x.Number)
            .Select(x => new ConvocationDbModel
            {
                Id = x.Id,
                Number = x.Number,
                SiteCode = x.SiteCode,
                Title = x.Title,
                DateFrom = x.DateFrom,
                DateTo = x.DateTo,
            })
            .ToListAsync(cancellationToken));

        var factions = await reader.Read<Faction>().NotDeletedAt().ToListAsync(cancellationToken);
        foreach (var faction in factions)
        {
            factionsByCode[faction.Code] = faction;
            factionsByTitle[faction.Title] = faction;
        }

        canonResolver = new FactionCanonResolver(factionsByTitle, factionsByCode);

        var periods = await reader.Read<FactionPeriod>().NotDeletedAt().ToListAsync(cancellationToken);
        foreach (var period in periods)
        {
            periodsByKey[(period.SiteId, period.ConvocationId)] = period;
        }

        var deputies = await reader.Read<Deputy>().NotDeletedAt().ToListAsync(cancellationToken);
        foreach (var deputy in deputies)
        {
            deputiesBySiteId[deputy.SiteId] = deputy;
        }

        var bills = await reader.Read<Bill>()
            .NotDeletedAt()
            .Select(x => new { x.Number, x.Id })
            .ToListAsync(cancellationToken);
        foreach (var bill in bills)
        {
            billIdsByNumber[bill.Number] = bill.Id;
        }

        var memberships = await reader.Read<DeputyFactionMembership>().NotDeletedAt().ToListAsync(cancellationToken);
        foreach (var membership in memberships)
        {
            membershipsByKey[(membership.DeputyId, membership.FactionPeriodId)] = membership;
        }

        log.Info($"Справочники загружены: созывов {convocations.Count}, фракций {factions.Count}, периодов {periods.Count}, депутатов {deputies.Count}, законопроектов {bills.Count}, членств {memberships.Count}");
    }

    /// <summary>
    /// Найти созыв по дате голосования; null — дата не входит ни в один созыв
    /// </summary>
    public ConvocationDbModel? FindConvocation(DateOnly date)
    {
        return convocations.FirstOrDefault(x => x.DateFrom <= date && (x.DateTo == null || date <= x.DateTo));
    }

    /// <summary>
    /// Найти или создать законопроект по номеру из заголовка голосования
    /// </summary>
    public Guid? EnsureBill(string? number, DateTimeOffset now)
    {
        if (number == null)
        {
            return null;
        }

        if (billIdsByNumber.TryGetValue(number, out var existingId))
        {
            return existingId;
        }

        var bill = new Bill
        {
            Id = Guid.NewGuid(),
            Number = number,
            CreatedAt = now,
            UpdatedAt = now,
        };
        billIdsByNumber[number] = bill.Id;
        billWriter.Add(bill);
        return bill.Id;
    }

    /// <summary>
    /// Найти или создать депутата по идентификатору сайта; при смене имени — обновить имя
    /// </summary>
    public Guid EnsureDeputy(long siteId, string fullName, DateTimeOffset now)
    {
        if (deputiesBySiteId.TryGetValue(siteId, out var deputy))
        {
            if (!string.Equals(deputy.FullName, fullName, StringComparison.Ordinal))
            {
                deputy.FullName = fullName;
                deputy.SortName = fullName;
                deputy.UpdatedAt = now;
                dirtyDeputies.Add(deputy);
            }

            return deputy.Id;
        }

        var created = new Deputy
        {
            Id = Guid.NewGuid(),
            SiteId = siteId,
            FullName = fullName,
            SortName = fullName,
            CreatedAt = now,
            UpdatedAt = now,
        };
        deputiesBySiteId[siteId] = created;
        deputyWriter.Add(created);
        return created.Id;
    }

    /// <summary>
    /// Найти или создать фракционный период по ключу «идентификатор фракции на сайте + созыв»;
    /// каноническая фракция определяется по названию, границы периода уточняются по датам голосований
    /// </summary>
    public Guid EnsureFactionPeriod(long factionSiteId, string factionTitle, ConvocationDbModel convocation, DateOnly date, DateTimeOffset now)
    {
        var key = (factionSiteId, convocation.Id);
        if (periodsByKey.TryGetValue(key, out var period))
        {
            ExtendPeriod(period, date, now);
            return period.Id;
        }

        var faction = canonResolver.Resolve(factionTitle);
        if (faction == null)
        {
            faction = CreateUnknownFaction(factionSiteId, factionTitle, now);
        }

        period = new FactionPeriod
        {
            Id = Guid.NewGuid(),
            SiteId = factionSiteId,
            FactionId = faction.Id,
            ConvocationId = convocation.Id,
            Title = factionTitle,
            DateFrom = date,
            DateTo = date,
            CreatedAt = now,
            UpdatedAt = now,
        };
        periodsByKey[key] = period;
        factionPeriodWriter.Add(period);
        return period.Id;
    }

    /// <summary>
    /// Учесть членство депутата во фракционном периоде на дату голосования:
    /// создать недостающее членство либо расширить его границы
    /// </summary>
    public void TrackMembership(Guid deputyId, Guid periodId, DateOnly date, DateTimeOffset now)
    {
        var key = (deputyId, periodId);
        if (membershipsByKey.TryGetValue(key, out var membership))
        {
            var changed = false;
            if (date < membership.DateFrom)
            {
                membership.DateFrom = date;
                changed = true;
            }

            if (date > membership.DateTo)
            {
                membership.DateTo = date;
                changed = true;
            }

            if (changed)
            {
                membership.UpdatedAt = now;
                dirtyMemberships.Add(membership);
            }

            return;
        }

        membership = new DeputyFactionMembership
        {
            Id = Guid.NewGuid(),
            DeputyId = deputyId,
            FactionPeriodId = periodId,
            DateFrom = date,
            DateTo = date,
            CreatedAt = now,
            UpdatedAt = now,
        };
        membershipsByKey[key] = membership;
        membershipWriter.Add(membership);
    }

    /// <summary>
    /// Передать накопленные изменения справочников в репозитории записи (перед SaveChanges пачки)
    /// </summary>
    public void FlushUpdates()
    {
        foreach (var deputy in dirtyDeputies)
        {
            deputyWriter.Edit(deputy);
        }

        foreach (var period in dirtyPeriods)
        {
            factionPeriodWriter.Edit(period);
        }

        foreach (var membership in dirtyMemberships)
        {
            membershipWriter.Edit(membership);
        }

        dirtyDeputies.Clear();
        dirtyPeriods.Clear();
        dirtyMemberships.Clear();
    }

    private void ExtendPeriod(FactionPeriod period, DateOnly date, DateTimeOffset now)
    {
        var changed = false;
        if (date < period.DateFrom)
        {
            period.DateFrom = date;
            changed = true;
        }

        if (period.DateTo == null || date > period.DateTo)
        {
            period.DateTo = date;
            changed = true;
        }

        if (changed)
        {
            period.UpdatedAt = now;
            dirtyPeriods.Add(period);
        }
    }

    private Faction CreateUnknownFaction(long factionSiteId, string factionTitle, DateTimeOffset now)
    {
        var code = factionTitle.Length <= FactionValidationConstants.CodeMaxLength
            ? factionTitle
            : $"F{factionSiteId}";
        if (factionsByCode.ContainsKey(code))
        {
            code = $"F{factionSiteId}";
        }

        var faction = new Faction
        {
            Id = Guid.NewGuid(),
            Code = code,
            Title = factionTitle,
            CreatedAt = now,
            UpdatedAt = now,
        };
        factionsByCode[code] = faction;
        factionsByTitle[factionTitle] = faction;
        factionWriter.Add(faction);
        log.Info($"Неизвестное название фракции «{factionTitle}»: создана каноническая фракция с кодом {code}");
        return faction;
    }
}