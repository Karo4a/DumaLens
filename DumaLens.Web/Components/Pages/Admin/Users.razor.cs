using DumaLens.Api.Client;
using DumaLens.Web.Components.Shared;
using DumaLens.Web.Services;
using Microsoft.AspNetCore.Components;

namespace DumaLens.Web.Components.Pages.Admin;

/// <summary>
/// Админ-панель пользователей (Э9): список с пагинацией, создание, роль и блокировка, сброс пароля
/// </summary>
public partial class Users : ComponentBase, IDisposable
{
    private const int PageSize = 20;

    [Inject]
    private IUsersClient UsersClient { get; set; } = default!;

    [Inject]
    private UserState UserState { get; set; } = default!;

    private ICollection<UserApiModel> items = [];
    private int totalCount;
    private int page = 1;
    private int loadSequence;
    private bool loading = true;
    private bool busy;
    private bool loaded;
    private string? error;
    private (string Login, string Password)? resetResult;

    private string createLogin = string.Empty;
    private string createName = string.Empty;
    private string createEmail = string.Empty;
    private string createRole = "User";
    private string createPassword = string.Empty;

    private UserApiModel? editUser;
    private string editName = string.Empty;
    private string editEmail = string.Empty;
    private string editRole = "User";
    private bool editBlocked;
    private string editNote = string.Empty;

    /// <inheritdoc cref="ComponentBase.OnInitializedAsync"/>
    protected override async Task OnInitializedAsync()
    {
        UserState.OnChange += OnUserStateChanged;
        // Запрос выполняется только для администратора: при пререндере состояние ещё не восстановлено
        if (UserState.IsInitialized && UserState.IsAdministrator)
        {
            await LoadAsync();
        }
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
        => UserState.OnChange -= OnUserStateChanged;

    private void OnUserStateChanged()
    {
        if (!loaded && UserState.IsInitialized && UserState.IsAdministrator)
        {
            _ = InvokeAsync(async () =>
            {
                await LoadAsync();
                StateHasChanged();
            });
        }
    }

    private async Task LoadAsync()
    {
        var sequence = ++loadSequence;
        loading = true;
        error = null;
        try
        {
            var result = await UsersClient.GetPageAsync(page, PageSize);
            // Ответ устаревшего запроса (быстрая смена страницы) не перезаписывает свежий
            if (sequence == loadSequence)
            {
                items = result.Items;
                totalCount = result.TotalCount;
                loaded = true;
            }
        }
        catch (ApiException exception)
        {
            if (sequence == loadSequence)
            {
                error = ApiErrorText.Describe(exception);
            }
        }
        catch (HttpRequestException)
        {
            if (sequence == loadSequence)
            {
                error = "API недоступен. Повторите позже.";
            }
        }
        catch (TaskCanceledException)
        {
            if (sequence == loadSequence)
            {
                error = "API недоступен. Повторите позже.";
            }
        }
        finally
        {
            if (sequence == loadSequence)
            {
                loading = false;
            }
        }
    }

    private async Task GoToPageAsync(int newPage)
    {
        page = newPage;
        await LoadAsync();
    }

    private async Task CreateAsync()
    {
        if (busy)
        {
            return;
        }
        busy = true;
        error = null;
        try
        {
            await UsersClient.CreateAsync(new CreateUserRequestApiModel
            {
                Login = createLogin,
                Name = createName,
                Email = string.IsNullOrWhiteSpace(createEmail) ? null : createEmail,
                Role = createRole,
                Password = createPassword,
            });
            createLogin = string.Empty;
            createName = string.Empty;
            createEmail = string.Empty;
            createRole = "User";
            createPassword = string.Empty;
            await LoadAsync();
        }
        catch (ApiException exception)
        {
            error = ApiErrorText.Describe(exception);
        }
        catch (HttpRequestException)
        {
            error = "API недоступен. Повторите позже.";
        }
        catch (TaskCanceledException)
        {
            error = "API недоступен. Повторите позже.";
        }
        finally
        {
            busy = false;
        }
    }

    private void StartEdit(UserApiModel user)
    {
        editUser = user;
        editName = user.Name;
        editEmail = user.Email;
        editRole = user.Role;
        editBlocked = user.Blocked;
        editNote = user.Note ?? string.Empty;
    }

    private void CancelEdit()
        => editUser = null;

    private async Task SaveEditAsync()
    {
        if (busy || editUser == null)
        {
            return;
        }
        busy = true;
        error = null;
        try
        {
            await UsersClient.UpdateAsync(editUser.Id, new UpdateUserRequestApiModel
            {
                Name = editName,
                Email = string.IsNullOrWhiteSpace(editEmail) ? null : editEmail,
                Role = editRole,
                Blocked = editBlocked,
                Note = string.IsNullOrWhiteSpace(editNote) ? null : editNote,
            });
            editUser = null;
            await LoadAsync();
        }
        catch (ApiException exception)
        {
            error = ApiErrorText.Describe(exception);
        }
        catch (HttpRequestException)
        {
            error = "API недоступен. Повторите позже.";
        }
        catch (TaskCanceledException)
        {
            error = "API недоступен. Повторите позже.";
        }
        finally
        {
            busy = false;
        }
    }

    private async Task ResetPasswordAsync(UserApiModel user)
    {
        if (busy)
        {
            return;
        }
        busy = true;
        error = null;
        try
        {
            var result = await UsersClient.ResetPasswordAsync(user.Id);
            resetResult = (user.Login, result.TemporaryPassword);
            await LoadAsync();
        }
        catch (ApiException exception)
        {
            error = ApiErrorText.Describe(exception);
        }
        catch (HttpRequestException)
        {
            error = "API недоступен. Повторите позже.";
        }
        catch (TaskCanceledException)
        {
            error = "API недоступен. Повторите позже.";
        }
        finally
        {
            busy = false;
        }
    }

    private static string RoleTitle(string role)
        => role switch
        {
            "Administrator" => "Администратор",
            "Moderator" => "Модератор",
            "User" => "Пользователь",
            _ => role,
        };
}