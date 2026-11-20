using DumaLens.Entities.ValidationConstants;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Models;
using FluentValidation;

namespace DumaLens.Services.Auth;

/// <summary>
/// Валидатор запроса входа
/// </summary>
public class LoginModelValidator : AbstractValidator<LoginModel>
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public LoginModelValidator()
    {
        RuleFor(x => x.Login)
            .NotEmpty()
            .WithMessage("Логин обязателен")
            .MaximumLength(UserValidationConstants.LoginMaxLength)
            .WithMessage($"Длина логина не должна превышать {UserValidationConstants.LoginMaxLength} символов");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Пароль обязателен")
            .MaximumLength(UserValidationConstants.PasswordMaxLength)
            .WithMessage($"Длина пароля не должна превышать {UserValidationConstants.PasswordMaxLength} символов");
    }
}

/// <summary>
/// Валидатор запроса обновления токенов
/// </summary>
public class RefreshRequestModelValidator : AbstractValidator<RefreshRequestModel>
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public RefreshRequestModelValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh-токен обязателен")
            .MaximumLength(UserValidationConstants.RefreshTokenMaxLength)
            .WithMessage($"Длина refresh-токена не должна превышать {UserValidationConstants.RefreshTokenMaxLength} символов");
    }
}

/// <summary>
/// Валидатор запроса смены пароля
/// </summary>
public class ChangePasswordModelValidator : AbstractValidator<ChangePasswordModel>
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public ChangePasswordModelValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("Текущий пароль обязателен")
            .MaximumLength(UserValidationConstants.PasswordMaxLength)
            .WithMessage($"Длина пароля не должна превышать {UserValidationConstants.PasswordMaxLength} символов");

        RuleFor(x => x.NewPassword)
            .MinimumLength(UserValidationConstants.PasswordMinLength)
            .WithMessage($"Длина нового пароля должна быть не менее {UserValidationConstants.PasswordMinLength} символов")
            .MaximumLength(UserValidationConstants.PasswordMaxLength)
            .WithMessage($"Длина нового пароля не должна превышать {UserValidationConstants.PasswordMaxLength} символов");
    }
}

/// <summary>
/// Валидатор запроса создания пользователя
/// </summary>
public class CreateUserModelValidator : AbstractValidator<CreateUserModel>
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public CreateUserModelValidator()
    {
        RuleFor(x => x.Login)
            .NotEmpty()
            .WithMessage("Логин обязателен")
            .MaximumLength(UserValidationConstants.LoginMaxLength)
            .WithMessage($"Длина логина не должна превышать {UserValidationConstants.LoginMaxLength} символов");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Имя обязательно")
            .MaximumLength(UserValidationConstants.NameMaxLength)
            .WithMessage($"Длина имени не должна превышать {UserValidationConstants.NameMaxLength} символов");

        RuleFor(x => x.Email)
            .MaximumLength(UserValidationConstants.EmailMaxLength)
            .WithMessage($"Длина электронной почты не должна превышать {UserValidationConstants.EmailMaxLength} символов")
            .EmailAddress()
            .WithMessage("Некорректный адрес электронной почты")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Role)
            .Must(role => RoleHelper.Parse(role) != null)
            .WithMessage((x, role) => $"Неизвестная роль: «{role}». Допустимы: User, Moderator, Administrator");

        RuleFor(x => x.Password)
            .MinimumLength(UserValidationConstants.PasswordMinLength)
            .WithMessage($"Длина пароля должна быть не менее {UserValidationConstants.PasswordMinLength} символов")
            .MaximumLength(UserValidationConstants.PasswordMaxLength)
            .WithMessage($"Длина пароля не должна превышать {UserValidationConstants.PasswordMaxLength} символов");
    }
}

/// <summary>
/// Валидатор запроса обновления пользователя
/// </summary>
public class UpdateUserModelValidator : AbstractValidator<UpdateUserModel>
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public UpdateUserModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Имя обязательно")
            .MaximumLength(UserValidationConstants.NameMaxLength)
            .WithMessage($"Длина имени не должна превышать {UserValidationConstants.NameMaxLength} символов");

        RuleFor(x => x.Email)
            .MaximumLength(UserValidationConstants.EmailMaxLength)
            .WithMessage($"Длина электронной почты не должна превышать {UserValidationConstants.EmailMaxLength} символов")
            .EmailAddress()
            .WithMessage("Некорректный адрес электронной почты")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Role)
            .Must(role => RoleHelper.Parse(role) != null)
            .WithMessage((x, role) => $"Неизвестная роль: «{role}». Допустимы: User, Moderator, Administrator");

        RuleFor(x => x.Blocked)
            .NotNull()
            .WithMessage("Признак блокировки обязателен");

        RuleFor(x => x.Note)
            .MaximumLength(UserValidationConstants.NoteMaxLength)
            .WithMessage($"Длина заметки не должна превышать {UserValidationConstants.NoteMaxLength} символов")
            .When(x => x.Note != null);
    }
}

/// <summary>
/// Валидатор запроса страницы списка пользователей
/// </summary>
public class UserListQueryModelValidator : AbstractValidator<UserListQueryModel>
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public UserListQueryModelValidator()
    {
        RuleFor(x => x.Page)
            .InclusiveBetween(UserValidationConstants.PageMin, UserValidationConstants.PageMax)
            .WithMessage($"Номер страницы должен быть в диапазоне от {UserValidationConstants.PageMin} до {UserValidationConstants.PageMax}");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(UserValidationConstants.PageSizeMin, UserValidationConstants.PageSizeMax)
            .WithMessage($"Размер страницы должен быть в диапазоне от {UserValidationConstants.PageSizeMin} до {UserValidationConstants.PageSizeMax}");
    }
}