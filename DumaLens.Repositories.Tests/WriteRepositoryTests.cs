using DumaLens.Common.Contracts;
using DumaLens.Context.Contracts;
using DumaLens.Context.Tests;
using DumaLens.Entities;
using DumaLens.Repositories.WriteRepositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DumaLens.Repositories.Tests;

/// <summary>
/// Тесты репозиториев записи (аудит и мягкое удаление) на базе в памяти
/// </summary>
public class WriteRepositoryTests : DumaLensContextInMemory
{
    private static readonly DateTimeOffset FixedTime = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
    private readonly ConvocationWriteRepository repository;
    private readonly FakeDateTimeProvider dateTimeProvider;

    /// <summary>
    /// Конструктор
    /// </summary>
    public WriteRepositoryTests()
    {
        dateTimeProvider = new FakeDateTimeProvider(FixedTime);
        repository = new ConvocationWriteRepository((IWriter)Context, dateTimeProvider);
    }

    /// <summary>
    /// Add должен проставлять аудит создания и изменения из поставщика времени
    /// </summary>
    [Fact]
    public async Task AddShouldSetAuditTimestamps()
    {
        // Arrange
        var convocation = await PrepareConvocation(save: false);

        // Act
        ((IDbWriter<Convocation>)repository).Add(convocation);
        await UnitOfWork.SaveChangesAsync();

        // Assert
        convocation.CreatedAt.Should().Be(FixedTime);
        convocation.UpdatedAt.Should().Be(FixedTime);
        var stored = await Context.Set<Convocation>().AsNoTracking().SingleAsync(x => x.Id == convocation.Id);
        stored.CreatedAt.Should().Be(FixedTime);
    }

    /// <summary>
    /// Edit должен обновлять только время изменения
    /// </summary>
    [Fact]
    public async Task EditShouldUpdateOnlyUpdatedAt()
    {
        // Arrange
        var convocation = await PrepareConvocation();
        var createdAt = convocation.CreatedAt;
        var laterTime = FixedTime.AddHours(1);
        dateTimeProvider.Time = laterTime;

        // Act
        convocation.Title = "Новый заголовок";
        ((IDbWriter<Convocation>)repository).Edit(convocation);
        await UnitOfWork.SaveChangesAsync();

        // Assert
        convocation.CreatedAt.Should().Be(createdAt);
        convocation.UpdatedAt.Should().Be(laterTime);
    }

    /// <summary>
    /// Delete должен мягко удалять сущность, сохраняя запись в базе
    /// </summary>
    [Fact]
    public async Task DeleteShouldSoftDeleteEntity()
    {
        // Arrange
        var convocation = await PrepareConvocation();

        // Act
        ((IDbWriter<Convocation>)repository).Delete(convocation);
        await UnitOfWork.SaveChangesAsync();

        // Assert
        convocation.DeletedAt.Should().Be(FixedTime);
        var stored = await Context.Set<Convocation>().AsNoTracking().SingleAsync(x => x.Id == convocation.Id);
        stored.DeletedAt.Should().Be(FixedTime);
    }

    /// <summary>
    /// Фиксированный поставщик времени для детерминированных тестов аудита
    /// </summary>
    private class FakeDateTimeProvider : IDateTimeProvider
    {
        /// <summary>
        /// Текущее фиксированное время
        /// </summary>
        public DateTimeOffset Time { get; set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        public FakeDateTimeProvider(DateTimeOffset time)
        {
            Time = time;
        }

        /// <inheritdoc cref="IDateTimeProvider.UtcNow"/>
        public DateTimeOffset UtcNow() => Time;

        /// <inheritdoc cref="IDateTimeProvider.Now"/>
        public DateTimeOffset Now() => Time;
    }
}