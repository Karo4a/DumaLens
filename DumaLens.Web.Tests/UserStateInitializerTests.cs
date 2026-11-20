using DumaLens.Api.Client;
using DumaLens.Web.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace DumaLens.Web.Tests;

/// <summary>
/// Тесты восстановления состояния пользователя при старте цепи: недоступность и таймаут Api
/// </summary>
public class UserStateInitializerTests
{
    /// <summary>
    /// Таймаут Api: состояние анонимное, сохранённые токены не затираются
    /// </summary>
    [Fact]
    public async Task Initialize_ApiTimeout_KeepsTokensAndStaysAnonymous()
    {
        var authClient = new Mock<IAuthClient>();
        authClient.Setup(a => a.GetProfileAsync()).ThrowsAsync(new TaskCanceledException("таймаут"));
        var tokenStore = new Mock<IClientTokenStore>();
        tokenStore.Setup(t => t.GetAccessTokenAsync()).ReturnsAsync("access-token");
        var state = new UserState();
        var initializer = new UserStateInitializer(authClient.Object, tokenStore.Object, state);

        await initializer.InitializeAsync();

        state.IsAuthenticated.Should().BeFalse();
        state.IsInitialized.Should().BeTrue();
        tokenStore.Verify(t => t.ClearAsync(), Times.Never);
    }

    /// <summary>
    /// Отказ Api по статусу (невалидные токены): состояние анонимное, токены очищаются
    /// </summary>
    [Fact]
    public async Task Initialize_ApiError_ClearsTokens()
    {
        var authClient = new Mock<IAuthClient>();
        authClient.Setup(a => a.GetProfileAsync())
            .ThrowsAsync(new ApiException("unauthorized", 401, string.Empty, new Dictionary<string, IEnumerable<string>>(), null));
        var tokenStore = new Mock<IClientTokenStore>();
        tokenStore.Setup(t => t.GetAccessTokenAsync()).ReturnsAsync("access-token");
        var state = new UserState();
        var initializer = new UserStateInitializer(authClient.Object, tokenStore.Object, state);

        await initializer.InitializeAsync();

        state.IsAuthenticated.Should().BeFalse();
        tokenStore.Verify(t => t.ClearAsync(), Times.Once);
    }
}