using DumaLens.Api.Client;

namespace DumaLens.Web.Services;

/// <summary>
/// Восстанавливает состояние пользователя из Api при старте цепи
/// </summary>
/// <remarks>
/// Вызывается из MainLayout после первого интерактивного рендера (ProtectedLocalStorage
/// доступен только в цепи). Запрос профиля идёт через аутентифицированный клиент:
/// при просроченном access-токене обработчик сам обновит пару по refresh-токену.
/// </remarks>
public class UserStateInitializer
{
    private readonly IAuthClient authClient;
    private readonly IClientTokenStore tokenStore;
    private readonly UserState userState;

    /// <summary>
    /// Конструктор
    /// </summary>
    public UserStateInitializer(IAuthClient authClient, IClientTokenStore tokenStore, UserState userState)
    {
        this.authClient = authClient;
        this.tokenStore = tokenStore;
        this.userState = userState;
    }

    /// <summary>
    /// Восстанавливает профиль по сохранённым токенам; при неудаче очищает токены и состояние
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var accessToken = await tokenStore.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                userState.Clear();
                return;
            }
            var profile = await authClient.GetProfileAsync();
            userState.SetFrom(profile.Id, profile.Login, profile.Name, profile.Email, profile.Role, profile.PasswordIsTemporary);
        }
        catch (ApiException)
        {
            await tokenStore.ClearAsync();
            userState.Clear();
        }
        catch (HttpRequestException)
        {
            // Api недоступен: состояние остаётся анонимным, токены не трогаем
            userState.Clear();
        }
        catch (TaskCanceledException)
        {
            // Api недоступен: состояние остаётся анонимным, токены не трогаем
            userState.Clear();
        }
    }
}