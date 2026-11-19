using DumaLens.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace DumaLens.Web.Infrastructure;

/// <summary>
/// Поставщик состояния аутентификации для AuthorizeView и [Authorize] на основе UserState
/// </summary>
/// <remarks>
/// Принципал строится из профиля, полученного от Api (не разбором токена на клиенте);
/// изменение UserState уведомляет каскад состояния аутентификации.
/// </remarks>
public class UserStateAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private static readonly AuthenticationState AnonymousState = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly UserState userState;

    /// <summary>
    /// Конструктор
    /// </summary>
    public UserStateAuthenticationStateProvider(UserState userState)
    {
        this.userState = userState;
        userState.OnChange += OnUserStateChanged;
    }

    /// <inheritdoc cref="AuthenticationStateProvider.GetAuthenticationStateAsync"/>
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!userState.IsAuthenticated)
        {
            return Task.FromResult(AnonymousState);
        }
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userState.Id!.Value.ToString()),
            new(ClaimTypes.Name, userState.Login ?? string.Empty),
            new(ClaimTypes.Role, userState.Role ?? string.Empty),
        };
        var identity = new ClaimsIdentity(claims, authenticationType: "DumaLens");
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
        => userState.OnChange -= OnUserStateChanged;

    private void OnUserStateChanged()
        => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}