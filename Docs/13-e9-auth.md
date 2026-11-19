# 13. Аутентификация и роли (Э9)

Реализация базы системы доступа по [07-security.md](07-security.md): JWT + refresh с ротацией, три роли с иерархией, админ-панель пользователей, CLI `create-admin`. Сущности и seed ролей заложены на Э1; Э9 включает их в работу. ASP.NET Core Identity не используется (ADR-11).

## 1. Состав

| Слой | Что добавлено |
|---|---|
| Entities | `User.LastFailedLoginAt` (окно блокировки попыток), `RefreshToken.SecurityStamp` (метка на момент выпуска); миграция `AuthSessionFields` |
| Entities.Configurations | Уникальный индекс `Users.EmailLowerCase` дополнен фильтром `"Email" <> ''` (почта необязательна, пустые значения не конфликтуют) |
| Repositories | `UserReadRepository` (по логину/почте/идентификатору, страница списка, счётчик активных администраторов), `UserWriteRepository`, `RefreshTokenReadRepository` (по хэшу, активный по пользователю), `RefreshTokenWriteRepository` |
| Services | `SecurityHelper` (PBKDF2-SHA256, refresh-токены, временные пароли), `TokenService` (JWT JWS), `AuthService` (login/refresh/logout/change-password/me), `UserService` (CRUD пользователей), валидаторы FluentValidation, `AuthOptions`, `RoleHelper`, `DumaLensUnauthorizedException` (401) |
| Api | `AuthController`, `UsersController`, JwtBearer middleware, политики `Moderator`/`Administrator` (`MinimumRoleRequirement` + `MinimumRoleHandler`), rate limit логина (фиксированное окно на IP) |
| Api.Client | NSwag-клиенты `AuthClient`, `UsersClient` (регенерация из спеки) |
| Web | `ClientTokenStore` (ProtectedLocalStorage), `AuthDelegatingHandler` (Bearer + авто-refresh при 401), `UserState`/`UserStateInitializer`, `UserStateAuthenticationStateProvider` (питает `AuthorizeView`), `ProtectedContent` (защита страниц с gate инициализации), страницы `/login`, `/change-password`, `/profile`, `/admin/users`, слот аутентификации в навигации |
| Importer | CLI `create-admin` (временный пароль → принудительная смена при первом входе) |

## 2. Механика

### 2.1. Пароли (ADR-12)

- PBKDF2-SHA256: соль 32 байта (`RandomNumberGenerator`), хэш 32 байта, **100 000 итераций** (`AuthOptions.PasswordIterations`; в эталонах 6 — не копируем). Сравнение — `CryptographicOperations.FixedTimeEquals`.
- Минимальная длина пароля 8, максимальная 128 (`UserValidationConstants`).
- Временный пароль (`PasswordIsTemporary`): создаётся администратором или CLI; при входе профиль помечается флагом, Web перенаправляет на `/change-password`; обновление токенов с временным паролем запрещено. Смена пароля снимает флаг.
- Неудачные попытки: счётчик `LoginAttemptFailedCount` + `LastFailedLoginAt`; после 5 неудач (`MaxFailedLoginAttempts`) вход блокируется на 15 минут (`LoginLockoutMinutes`) даже с верным паролем; успешный вход сбрасывает счётчик.

### 2.2. Токены (ADR-13)

- **Access** — JWT JWS (HMAC-SHA256), без шифрования: клеймы `sub` (UserId), `name` (логин), `role`, `security_stamp`, `jti`; срок жизни 15 минут (`AccessTokenLifetimeMinutes`). Проверка: подпись, issuer, audience, срок (`ClockSkew` 30 с).
- **Refresh** — случайные 256 бит; в базе хранится только SHA-256-хэш (`RefreshTokens.TokenHash`) и метка безопасности пользователя на момент выпуска (`SecurityStamp`). Срок жизни 30 дней (`RefreshTokenLifetimeDays`). Один активный refresh на пользователя (уникальный индекс `UserId` с фильтром `RevokedAt IS NULL`).
- **Ротация**: каждое обновление отзывает старый токен (`RevokedAt`) и выпускает новую пару. Повторный вход также отзывает предыдущий refresh.
- **Кража**: предъявление отозванного refresh-токена отзывает все сессии пользователя и меняет `SecurityStamp` (инвалидируются и access-токены при следующем обновлении).
- **Отзыв сессий**: смена пароля, смена роли, блокировка и сброс пароля меняют `SecurityStamp` и отзывают активный refresh. Проверка метки выполняется при обновлении; access-токен живёт до истечения срока (короткий срок жизни ограничивает окно).
- Выход (`logout`) отзывает активный refresh-токен.

### 2.3. Роли и авторизация

- Три роли с фиксированными идентификаторами (seed `UserRolesDefault`, миграция `InitialCreate`): User ⊂ Moderator ⊂ Administrator.
- Политики Api: `Moderator` и `Administrator` — `MinimumRoleRequirement` + `MinimumRoleHandler` проверяют клейм `role` по иерархии (`RoleHelper.HasMinimumRole`): Administrator проходит требования Moderator.
- Матрица доступа MVP: публичное чтение данных голосований — анонимно (`[AllowAnonymous]`); профиль/смена пароля/выход — аутентифицирован; `/api/users` — только Administrator. Эндпоинты Moderator появятся на Э11 (теги) и используют готовую политику.
- Коды: 401 — нет/недействителен токен, неверные учётные данные, блокировка; 403 — недостаточная роль; 404 — запись не найдена; 409 — дубликат логина/почты; 422 — валидация; 429 — rate limit логина.

### 2.4. Rate limiting

`ASP.NET Core RateLimiter`, фиксированное окно на IP эндпоинта `POST /api/auth/login`: 20 запросов в 60 секунд (`LoginRateLimitPermitLimit`/`LoginRateLimitWindowSeconds`), сверх лимита — 429. Основная защита от подбора — счётчик попыток и блокировка учётной записи (§2.1). Запросы Web-хоста к Api приходят с одного IP (серверный рендер), поэтому лимит окна щедрый; для публичного Api за прокси лимит применяется к IP прокси.

### 2.5. Web-хост

- Токены хранятся в `ProtectedLocalStorage` браузера (шифрование ключами DataProtection сервера); во время пререндера JS-интероп недоступен — чтение возвращает null.
- `AuthDelegatingHandler` добавляет `Authorization: Bearer` к запросам NSwag-клиентов; при 401 обновляет пару отдельным HttpClient без обработчика (без рекурсии) и повторяет запрос один раз. Обновление защищено семафором: параллельные запросы цепи выполняют ровно один refresh (гонка обновлений отозвала бы все сессии как повторное использование).
- `UserState` (scoped на цепь) заполняется профилем из `GET /api/auth/me` после подключения цепи (`MainLayout.OnAfterRenderAsync` → `UserStateInitializer`) или сразу после входа. `UserStateAuthenticationStateProvider` строит `ClaimsPrincipal` из состояния и питает `AuthorizeView` (механика DocumentGenerator.Web).
- Защищённые страницы обёрнуты в `ProtectedContent` (`AuthorizeView` + gate): во время пререндера и до восстановления состояния показывается загрузка; анонимный пользователь перенаправляется на `/login?returnUrl=…`; аутентифицированный без нужной роли видит отказ. Атрибут `[Authorize]` на страницах не используется: токен хранится в браузере (ProtectedLocalStorage), HTTP-запрос страницы учётных данных не несёт, поэтому endpoint-авторизация middleware неприменима. Админ-панель запрашивает данные только после подтверждения роли Administrator.
- Навигация: анонимно — «Войти»; аутентифицирован — логин (ссылка на профиль), «Пользователи» (только Administrator), «Выйти» (отзыв refresh + очистка токенов).
- Страницы: `/login` (форма, ошибка 401/429, возврат на исходный адрес), `/change-password` (текущий + новый + повтор; после смены все сессии завершаются — вход заново), `/profile` (логин, имя, почта, роль, смена пароля), `/admin/users` (список с пагинацией, создание с временным паролем, изменение имени/почты/роли/блокировки/заметки, сброс пароля с одноразовым показом временного пароля).

### 2.6. CLI create-admin

```bash
dotnet run --project DumaLens.Importer -c Release -- create-admin --login admin --name "Администратор"
# Временный пароль генерируется и печатается один раз; --password задаёт явно, --email необязателен
```

Команда накатывает миграции, создаёт пользователя с ролью Administrator и временным паролем (`PasswordIsTemporary = true`). При первом входе Web требует сменить пароль. Повторный запуск с тем же логином — ошибка (логин занят).

## 3. Конфигурация (О-7)

Секция `Auth` в `appsettings.json` Api — только не-secret значения и пустой `SigningKey`:

| Ключ | По умолчанию | Назначение |
|---|---|---|
| `Auth:Issuer` / `Auth:Audience` | `DumaLens.Api` / `DumaLens.Web` | Проверка издателя и аудитории токена |
| `Auth:SigningKey` | пусто | Ключ подписи JWT, base64, ≥ 32 байт. **Секрет**: переменная окружения `Auth__SigningKey` или user-secrets |
| `Auth:AccessTokenLifetimeMinutes` | 15 | Срок жизни access-токена |
| `Auth:RefreshTokenLifetimeDays` | 30 | Срок жизни refresh-токена |
| `Auth:PasswordIterations` | 100000 | Итерации PBKDF2 (не менее 100 000, ADR-12) |
| `Auth:MaxFailedLoginAttempts` / `Auth:LoginLockoutMinutes` | 5 / 15 | Блокировка после серии неудачных попыток |
| `Auth:LoginRateLimitPermitLimit` / `Auth:LoginRateLimitWindowSeconds` | 20 / 60 | Rate limit логина на IP |

Поведение при пустом `SigningKey`: в Production — отказ запуска с понятной ошибкой; в Development/Integration — временный случайный ключ на время жизни процесса (предупреждение в журнале; сессии не переживают перезапуск). Генерация ключа для развёртывания — [08-deployment.md](08-deployment.md).

## 4. Тесты (07-security.md §5)

- `DumaLens.Services.Tests`: `SecurityHelperTests` (детерминизм, разные соли, независимое вычисление PBKDF2, 100 000 итераций, FixedTimeEquals), `RoleHelperTests` (иерархия), `TokenServiceTests` (клеймы, подпись, срок, чужой ключ), `AuthServiceTests` и `UserServiceTests` — на одноразовой PostgreSQL-базе на каждый тест, реальные репозитории без моков: ротация, просроченный/отозванный refresh, повторное использование → отзыв всех сессий, смена метки, блокировка, счётчик попыток и окно, смена пароля, защита последнего администратора, уникальность логина/почты, пагинация.
- `DumaLens.Api.Tests`: `AuthApiTests` (вход/профиль/выход/смена пароля по HTTP, ротация и отзыв по HTTP, 401 без токена, испорченный токен, публичное чтение анонимно), `UsersApiTests` (401/403 для User и Moderator, CRUD Администратора, 409/422/404), `MinimumRoleHandlerTests` (Administrator проходит Moderator-требование), `LoginRateLimitTests` (429 сверх лимита), `DependenciesTests` (резолв новых контроллеров).
- `DumaLens.Web.Tests`: `AuthPageRenderTests` (форма входа, загрузка защищённых страниц при пререндере, публичные страницы анонимно), `UserStateTests`, `AuthDelegatingHandlerTests` (Bearer, авто-refresh с одним повтором, единый refresh параллельных запросов, очистка при неудаче).
- `DumaLens.Importer.Tests`: `CreateAdminWiringTests` (DI импортёра → создание администратора с временным паролем; вход и токены проверяются в Services.Tests — CLI токенов не выпускает).

## 5. Границы базы

- Cookie-поток для браузерных клиентов вне Web (Specular `SecurityTokenManager`) не реализуется на MVP; контракт допускает добавление (07-security.md §2.3).
- Восстановление пароля по почте, подтверждение почты, двухфакторная аутентификация — вне MVP.
- Модераторские эндпоинты (теги, нормализация) появятся на Э11+ и опираются на готовую политику `Moderator`.