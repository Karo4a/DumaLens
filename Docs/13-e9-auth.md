# 13. Аутентификация и роли (Э9)

Реализация базы системы доступа по [07-security.md](07-security.md): JWT + refresh с ротацией, три роли с иерархией, админ-панель пользователей, CLI `create-admin`. Сущности и seed ролей заложены на Э1; Э9 включает их в работу. ASP.NET Core Identity не используется (ADR-11).

## 1. Состав

| Слой | Что добавлено |
|---|---|
| Entities | `User.LastFailedLoginAt` (окно блокировки попыток), `RefreshToken.SecurityStamp` (метка на момент выпуска); миграции `AuthSessionFields`, `AuthIntegrityAndHashFormat` (длина хэша под префикс итераций, FK `RefreshTokens.UserId` → `Users.Id` с каскадом) |
| Entities.Configurations | Уникальный индекс `Users.EmailLowerCase` дополнен фильтром `"Email" <> ''` (почта необязательна, пустые значения не конфликтуют) |
| Repositories | `UserReadRepository` (по логину/почте/идентификатору, страница списка, счётчик активных администраторов), `UserWriteRepository`, `RefreshTokenReadRepository` (по хэшу, активный по пользователю), `RefreshTokenWriteRepository` |
| Services | `SecurityHelper` (PBKDF2-SHA256 с итерациями в префиксе хэша, refresh-токены, временные пароли), `TokenService` (JWT JWS), `AuthService` (login/refresh/logout/change-password/me, grace-окно, конкурентные повторы), `UserService` (CRUD пользователей, serializable-защита последнего администратора), `TokenCleanupService`, валидаторы FluentValidation, `AuthOptions` (валидация при старте), `RoleHelper`, `DumaLensUnauthorizedException` (401) |
| Api | `AuthController`, `UsersController`, JwtBearer middleware, политики `Moderator`/`Administrator` (`MinimumRoleRequirement` + `MinimumRoleHandler`), `TemporaryPasswordRequirement` + `TemporaryPasswordHandler` (принуждение смены временного пароля), rate limit логина и смены пароля (фиксированное окно на IP), `TokenCleanupBackgroundService` |
| Api.Client | NSwag-клиенты `AuthClient`, `UsersClient` (регенерация из спеки) |
| Web | `ClientTokenStore` (ProtectedLocalStorage), `AuthDelegatingHandler` (Bearer + авто-refresh при 401), `RefreshLock` (общий семафор обновления на цепь), `UserState`/`UserStateInitializer`, `UserStateAuthenticationStateProvider` (питает `AuthorizeView`), `ProtectedContent` (защита страниц с gate инициализации), страницы `/login`, `/change-password`, `/profile`, `/admin/users`, слот аутентификации в навигации |
| Importer | CLI `create-admin` (временный пароль → принудительная смена при первом входе) |

## 2. Механика

### 2.1. Пароли (ADR-12)

- PBKDF2-SHA256: соль 32 байта (`RandomNumberGenerator`), хэш 32 байта, **100 000 итераций** (`AuthOptions.PasswordIterations`; в эталонах 6 — не копируем). Сравнение — `CryptographicOperations.FixedTimeEquals`. Число итераций хранится в префиксе хэша («{итерации}.{base64}», формат Э9): смена `Auth:PasswordIterations` не ломает существующие пароли — проверка берёт итерации из хэша; legacy-хэши без префикса проверяются конфигурируемым значением.
- Минимальная длина пароля 8, максимальная 128 (`UserValidationConstants`).
- Временный пароль (`PasswordIsTemporary`): создаётся администратором или CLI; при входе профиль помечается флагом, Web перенаправляет на `/change-password`. Обновление токенов с временным паролем **разрешено** (иначе смена после истечения access-токена была бы невозможна); принуждение смены выполняется на уровне Api: клейм `pwd_temp` и `TemporaryPasswordRequirement` ограничивают доступ к не-auth эндпоинтам (403 «Требуется смена временного пароля»), эндпоинты аутентификации освобождены атрибутом `[AllowTemporaryPassword]`, анонимные эндпоинты не затрагиваются. Смена пароля снимает флаг.
- Неудачные попытки: счётчик `LoginAttemptFailedCount` + `LastFailedLoginAt`; после 5 неудач (`MaxFailedLoginAttempts`) вход блокируется на 15 минут (`LoginLockoutMinutes`) даже с верным паролем; успешный вход сбрасывает счётчик. Неудачные попытки смены пароля учитываются в том же счётчике; заблокированный пользователь смену пароля выполнить не может.
- Все отказы входа возвращают единое сообщение «Неверный логин или пароль» (существование и состояние учётной записи не раскрываются); неизвестный логин проверяется против фиктивного хэша — время ответа не выдаёт существование пользователя. Страница входа содержит статическую подсказку о политике блокировки.

### 2.2. Токены (ADR-13)

- **Access** — JWT JWS (HMAC-SHA256), без шифрования: клеймы `sub` (UserId), `name` (логин), `role`, `security_stamp`, `pwd_temp` (временный пароль), `jti`; срок жизни 15 минут (`AccessTokenLifetimeMinutes`). Проверка: подпись, issuer, audience, срок (`ClockSkew` 30 с). Ключ подписи валидируется при старте (base64, ≥ 32 байт; диапазоны настроек — `AuthOptions.Validate`, fail-fast).
- **Refresh** — случайные 256 бит; в базе хранится только SHA-256-хэш (`RefreshTokens.TokenHash`) и метка безопасности пользователя на момент выпуска (`SecurityStamp`). Срок жизни 30 дней (`RefreshTokenLifetimeDays`). Один активный refresh на пользователя (уникальный индекс `UserId` с фильтром `RevokedAt IS NULL`); `UserId` ссылается на `Users.Id` внешним ключом с каскадом (миграция `AuthIntegrityAndHashFormat`).
- **Ротация**: каждое обновление отзывает старый токен (`RevokedAt`) и выпускает новую пару. Повторный вход также отзывает предыдущий refresh. Конкурентные login/refresh одного пользователя сериализуются повтором на нарушении уникального индекса (без 500).
- **Кража и grace-окно**: предъявление отозванного refresh-токена **вне** grace-окна (`RefreshReuseGraceSeconds`, по умолчанию 30 с) отзывает все сессии пользователя и меняет `SecurityStamp`. **Внутри** окна, если метка токена совпадает с актуальной меткой пользователя, предъявление считается гонкой параллельных обновлений (вкладки/устройства с общим хранилищем токенов) и выпускает новую пару без отзыва сессий.
- **Отзыв сессий**: смена пароля, смена роли, блокировка и сброс пароля меняют `SecurityStamp` и отзывают активный refresh. Проверка метки выполняется при обновлении; access-токен живёт до истечения срока (короткий срок жизни ограничивает окно).
- **Очистка**: `TokenCleanupBackgroundService` (Api) раз в `TokenCleanupIntervalHours` (24 ч) удаляет истёкшие и отозванные токены старше `RefreshTokenRetentionDays` (7 дней) пакетами; активные и недавно отозванные сохраняются.
- Выход (`logout`) отзывает активный refresh-токен.

### 2.3. Роли и авторизация

- Три роли с фиксированными идентификаторами (seed `UserRolesDefault`, миграция `InitialCreate`): User ⊂ Moderator ⊂ Administrator.
- Политики Api: `Moderator` и `Administrator` — `MinimumRoleRequirement` + `MinimumRoleHandler` проверяют клейм `role` по иерархии (`RoleHelper.HasMinimumRole`): Administrator проходит требования Moderator. Политика по умолчанию и обе ролевые включают `TemporaryPasswordRequirement` (`TemporaryPasswordHandler`): пока пароль временный, доступны только эндпоинты аутентификации (`[AllowTemporaryPassword]` на `AuthController`); `[AllowAnonymous]`-эндпоинты авторизацию не проходят и не затрагиваются. Признак читается из клейма `pwd_temp` — обращения к базе на каждый запрос нет.
- Матрица доступа MVP: публичное чтение данных голосований — анонимно (`[AllowAnonymous]`); профиль/смена пароля/выход — аутентифицирован; `/api/users` — только Administrator. Эндпоинты Moderator появятся на Э11 (теги) и используют готовую политику.
- Коды: 401 — нет/недействителен токен, неверные учётные данные, блокировка (единое сообщение логина); 403 — недостаточная роль либо временный пароль (тело с сообщением); 404 — запись не найдена; 409 — дубликат логина/почты; 422 — валидация (включая пропуск обязательного `blocked` в PUT и номер страницы сверх `PageMax`); 429 — rate limit логина/смены пароля.

### 2.4. Rate limiting

`ASP.NET Core RateLimiter`, фиксированное окно на IP: `POST /api/auth/login` и `POST /api/auth/change-password` — раздельные бюджеты по 20 запросов в 60 секунд (`LoginRateLimitPermitLimit`/`LoginRateLimitWindowSeconds`), сверх лимита — 429. Смена пароля дополнительно защищена счётчиком попыток и блокировкой (§2.1) — брутфорс текущего пароля украденным access-токеном ограничен. Основная защита от подбора при входе — счётчик попыток и блокировка учётной записи. Запросы Web-хоста к Api приходят с одного IP (серверный рендер), поэтому лимит окна щедрый; для публичного Api за прокси лимит применяется к IP прокси.

### 2.5. Web-хост

- Токены хранятся в `ProtectedLocalStorage` браузера (шифрование ключами DataProtection сервера); во время пререндера JS-интероп недоступен — чтение возвращает null.
- `AuthDelegatingHandler` добавляет `Authorization: *** к запросам NSwag-клиентов; при 401 обновляет пару отдельным HttpClient без обработчика (без рекурсии) и повторяет запрос один раз. Обновление защищено **общим семафором цепи** (`RefreshLock`, scoped): все NSwag-клиенты цепи используют один экземпляр, поэтому параллельные запросы разных клиентов выполняют ровно один refresh. Запросы `api/auth/login` и `api/auth/refresh` идут без Bearer и без 401-повтора (их 401 — неверные учётные данные/недействительный refresh, а не протухший access). Токены очищаются только при явном отказе refresh (401/400); временные ошибки Api (5xx/429, сеть, таймаут) валидную сессию не разрушают.
- `UserState` (scoped на цепь) заполняется профилем из `GET /api/auth/me` после подключения цепи (`MainLayout.OnAfterRenderAsync` → `UserStateInitializer`) или сразу после входа. `UserStateAuthenticationStateProvider` строит `ClaimsPrincipal` из состояния и питает `AuthorizeView` (механика DocumentGenerator.Web).
- Защищённые страницы обёрнуты в `ProtectedContent` (`AuthorizeView` + gate): во время пререндера и до восстановления состояния показывается загрузка; анонимный пользователь перенаправляется на `/login?returnUrl=…`; аутентифицированный без нужной роли видит отказ. Атрибут `[Authorize]` на страницах не используется: токен хранится в браузере (ProtectedLocalStorage), HTTP-запрос страницы учётных данных не несёт, поэтому endpoint-авторизация middleware неприменима. Админ-панель запрашивает данные только после подтверждения роли Administrator.
- Навигация: анонимно — «Войти»; аутентифицирован — логин (ссылка на профиль), «Пользователи» (только Administrator), «Выйти» (отзыв refresh + очистка токенов).
- Страницы: `/login` (форма, ошибка 401/429, возврат на исходный адрес; уже вошедший пользователь уводится при свежей загрузке — подписка на `UserState.OnChange` с guard отправки формы, временный пароль — на `/change-password`), `/change-password` (текущий + новый + повтор; после смены все сессии завершаются — вход заново; ошибки показывают сообщение сервера), `/profile` (логин, имя, почта, роль, смена пароля), `/admin/users` (список с пагинацией и защитой от гонки ответов, создание с временным паролем, изменение имени/почты/роли/блокировки/заметки, сброс пароля с одноразовым показом временного пароля).

### 2.6. CLI create-admin

```bash
dotnet run --project DumaLens.Importer -c Release -- create-admin --login admin --name "Администратор"
# Временный пароль генерируется и печатается один раз; --password задаёт явно, --email необязателен
```

Команда накатывает миграции (таймаут команд 30 минут — наполненная база), создаёт пользователя с ролью Administrator и временным паролем (`PasswordIsTemporary = true`). При первом входе Web требует сменить пароль. Повторный запуск с тем же логином — ошибка (логин занят). Ошибки печатаются в stderr целиком (`ToString()`).

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
| `Auth:LoginRateLimitPermitLimit` / `Auth:LoginRateLimitWindowSeconds` | 20 / 60 | Rate limit логина и смены пароля на IP (раздельные бюджеты) |
| `Auth:RefreshReuseGraceSeconds` | 30 | Grace-окно повторного использования отозванного refresh (гонка вкладок vs кража) |
| `Auth:RefreshTokenRetentionDays` | 7 | Срок хранения утративших силу токенов до фоновой очистки |
| `Auth:TokenCleanupIntervalHours` | 24 | Интервал фоновой очистки refresh-токенов |

Значения валидируются при старте Api (`AuthOptions.Validate`): некорректный base64/короткий ключ, нулевые сроки, итерации ниже 100 000 и т.п. — отказ запуска с понятной ошибкой.

Поведение при пустом `SigningKey`: в Production — отказ запуска с понятной ошибкой; в Development/Integration — временный случайный ключ на время жизни процесса (предупреждение в журнале; сессии не переживают перезапуск). Генерация ключа для развёртывания — [08-deployment.md](08-deployment.md).

## 4. Тесты (07-security.md §5)

- `DumaLens.Services.Tests`: `SecurityHelperTests` (детерминизм, разные соли, независимое вычисление PBKDF2, формат с префиксом итераций, legacy-совместимость, дрейф конфигурации, FixedTimeEquals), `RoleHelperTests` (иерархия), `TokenServiceTests` (клеймы включая `pwd_temp`, подпись, срок, чужой ключ), `AuthOptionsTests` (валидация ключа и диапазонов), `AuthServiceTests`, `UserServiceTests` и `AuthConcurrencyTests` — на одноразовой PostgreSQL-базе на каждый тест, реальные репозитории без моков: ротация, просроченный/отозванный refresh, grace-окно (внутри/вне), повторное использование → отзыв всех сессий, смена метки, блокировка, счётчик попыток и окно (включая смену пароля), единое сообщение отказов входа, смена пароля, защита последнего администратора (включая параллельное разжалование), уникальность логина/почты (включая параллельное создание), пагинация и верхняя граница страницы, обязательность `blocked`; `TokenCleanupServiceTests` (очистка только давно утративших силу токенов, идемпотентность).
- `DumaLens.Api.Tests`: `AuthApiTests` (вход/профиль/выход/смена пароля по HTTP, ротация и отзыв по HTTP, 401 без токена, испорченный токен, публичное чтение анонимно и с недействительным токеном, матрица временного пароля), `UsersApiTests` (401/403 для User и Moderator, CRUD Администратора, 201 при создании, дефолты пагинации, 409/422/404, пропуск `blocked`, переполнение страницы), `MinimumRoleHandlerTests` (Administrator проходит Moderator-требование), `LoginRateLimitTests` (429 сверх лимита логина и смены пароля), `VotesApiTests` (переполнение страницы поиска → 422), `DependenciesTests` (резолв новых контроллеров).
- `DumaLens.Web.Tests`: `AuthPageRenderTests` (форма входа, загрузка защищённых страниц при пререндере, публичные страницы анонимно), `UserStateTests`, `AuthDelegatingHandlerTests` (Bearer, авто-refresh с одним повтором, единый refresh параллельных запросов одного и разных клиентов, пропуск логина/refresh, сохранение токенов при 5xx/таймауте, очистка при отказе), `LoginRedirectTests` (bUnit: редирект вошедшего при свежей загрузке и позднем восстановлении состояния, временный пароль, анонимный).
- `DumaLens.Database.Tests`: `DatabaseStructureTests` (соответствие EF-модели, внешний ключ `RefreshTokens.UserId` → `Users.Id`, каскадное удаление токенов при удалении пользователя).
- `DumaLens.Importer.Tests`: `CreateAdminWiringTests` (DI импортёра → создание администратора с временным паролем; вход и токены проверяются в Services.Tests — CLI токенов не выпускает).

## 5. Границы базы

- Cookie-поток для браузерных клиентов вне Web (Specular `SecurityTokenManager`) не реализуется на MVP; контракт допускает добавление (07-security.md §2.3).
- Восстановление пароля по почте, подтверждение почты, двухфакторная аутентификация — вне MVP.
- Модераторские эндпоинты (теги, нормализация) появятся на Э11+ и опираются на готовую политику `Moderator`.