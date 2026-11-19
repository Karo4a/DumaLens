# 07 — Аутентификация, авторизация и роли (база)

Спецификация базы системы доступа DumaLens. Требование проекта: роли **Пользователь / Модератор / Администратор**, аутентификация и авторизация; детальная проработка — позже, **базу закладываем сейчас** (сущности и seed — Э1, включение — Э9). **Реализовано на Э9** — описание реализации, конфигурация и тесты: [13-e9-auth.md](13-e9-auth.md).

Эталоны (проверены по коду): **Specular** (приоритетный ориентир: `AccountController`, `SecurityTokenBuilder`, `SecurityTokenManager`, `RefreshTokenManager`, `Role`/`RoleHelper`) и **DocumentGenerator master** (`AuthController`, `AuthService`, `TokenService`, `UserRole`/`UserRolesDefault`, `AuthDelegatingHandler`, `ClientTokenStore`). Решения зафиксированы в ADR-11…ADR-13 ([05-architecture.md](05-architecture.md)).

## 1. Роли

| Роль | Назначение | Права (база) |
|---|---|---|
| `User` (Пользователь) | зарегистрированный читатель | всё публичное + свои оценки важности (v1.2), профиль |
| `Moderator` (Модератор) | ведёт справочники и теги | права User + создание/редактирование тегов, approve/reject очереди модерации (v1.1), правка нормализации |
| `Administrator` (Администратор) | управляет системой | права Moderator + управление пользователями и ролями, блокировка, сброс паролей, запуск служебных операций |

- **Иерархия**: User ⊂ Moderator ⊂ Administrator. Проверка — через политики авторизации (`MinimumRole`), а не размазанные `if` по коду; утилита `RoleHelper` по образцу Specular.
- **Одна роль на пользователя** (`Users.RoleId` → `Roles`), как в DocumentGenerator master (`UserRole`-справочник, seed `UserRolesDefault` с фиксированными GUID). Множественные роли — отложено; при необходимости заменяется связью `*──*` без слома контракта API.
- Справочник `Roles` — seed при миграции: три записи с фиксированными GUID (по образцу `UserRolesDefault`), имена стабильны (`User`, `Moderator`, `Administrator`).
- Первый администратор создаётся seed-ом/командой CLI (`dumalens create-admin`) с временным паролем (`PasswordIsTemporary = true` → принудительная смена при первом входе, механика Specular).
- Гость (неаутентифицированный) — только публичные эндпоинты чтения; регистрация на MVP открытая не делается: пользователей создаёт администратор (упрощение базы; публичная регистрация — отдельное решение позже).

## 2. Аутентификация

### 2.1. Схема токенов (ADR-13)

- **Access-токен — JWT (JWS, подпись HMAC-SHA256)**, срок жизни короткий (по умолчанию 15 мин, конфигурируется). Клеймы: `sub` (UserId), `name`, `role`, `security_stamp`, `jti`. Шифрование (JWE) не используется — Web читает клеймы (в Specular шифрование закомментировано по той же причине).
- **Refresh-токен** — случайная строка (256 бит), в БД хранится **хэш**; запись `RefreshTokens` (`UserId`, `TokenHash`, `SecurityStamp`, `ExpiresAt`, `RevokedAt`; `SecurityStamp` добавлен миграцией `AuthSessionFields` на Э9). Срок жизни — 30 дней (конфигурируется). **Ротация**: при обновлении старый refresh отзывается, выпускается новая пара. Обнаружение повторного использования отозванного refresh → отзыв всех сессий пользователя и смена его `SecurityStamp` (защита от кражи).
- **Отзыв сессий**: `SecurityStamp` пользователя включается в клеймы access-токена и сверяется при refresh; смена пароля/блокировка меняет stamp → все токены инвалидируются (механика Specular).
- Один активный refresh на пользователя (как DG master) — упрощение базы; несколько устройств — позже.

### 2.2. Пароли (ADR-12)

- **PBKDF2-SHA256**, соль 32 байта (`RandomNumberGenerator`), хэш 32 байта, **≥ 100 000 итераций** (конфигурируется). Формат хранения: `PasswordHash`, `PasswordSalt` (base64), `PasswordIsTemporary`.
- В обоих эталонах `SecurityHelper` использует `MaximumIterations = 6` — это криптографически слабо; алгоритм берём их же (`Rfc2898DeriveBytes.Pbkdf2`), итерации поднимаем. Отступление осознанное (ADR-12).
- Сравнение — `CryptographicOperations.FixedTimeEquals` (защита от timing-атак).
- Требования к паролю (база): ≥ 8 символов; сложность не форсируем на MVP.
- Защита от перебора: счётчик `LoginAttemptFailedCount` + `LastFailedLoginAt` (окно блокировки, добавлено на Э9) + блокировка после N неудач (по умолчанию 5) на время T (по умолчанию 15 мин) + rate limiting на эндпоинт логина (ASP.NET Core RateLimiter, фиксированное окно на IP, по умолчанию 20 запросов/60 с).

### 2.3. Потоки токенов по хостам

- **Api** — единственный эмитент токенов: `AuthController` (`login`, `refresh`, `logout`) + JWT Bearer middleware (`AddAuthentication().AddJwtBearer(...)`), валидация подписи/срока/issuer/audience.
- **Web (Blazor Server)** — по образцу DocumentGenerator.Web: логин-форма вызывает Api через NSwag-клиент; токены хранятся на сервере в **ProtectedLocalStorage** (`ClientTokenStore`, DataProtection); `AuthDelegatingHandler` пробрасывает `Authorization: *** в каждый запрос к Api и при 401 делает refresh и повторяет запрос (семафор — один refresh на цепь); `UserState`/`UserStateInitializer` + `AuthenticationStateProvider` дают `AuthorizeView` в компонентах. Защищённые страницы обёрнуты в `ProtectedContent` (gate инициализации); атрибут `[Authorize]` на страницах не используется — HTTP-запрос страницы не несёт учётных данных (токен в браузере), endpoint-авторизация middleware неприменима (подробнее — [13-e9-auth.md](13-e9-auth.md)).
- **Браузерные клиенты вне Web** (будущий SPA/мобильные) — cookie-поток по образцу Specular: access и refresh в **HttpOnly + Secure + SameSite=Strict** cookie (`SecurityTokenManager`), CSRF-защита для cookie-аутентификации. На MVP этот поток не реализуется, но контракт `AuthController` его допускает (решение фиксируется на Э9).
- Строка подключения к PostgreSQL и ключи подписи/DataProtection — только конфигурация/переменные окружения/user-secrets; в репозиторий не попадают (О-7). Ключ JWT — ≥ 32 байта случайных, генерируется при развёртывании.

### 2.4. Эндпоинты auth (Api)

| Эндпоинт | Доступ | Назначение |
|---|---|---|
| `POST /api/auth/login` | анонимно | `{login, password}` → пара токенов + профиль; 401 при неверных данных/блокировке; учитывает rate limit |
| `POST /api/auth/refresh` | refresh-токен | ротация: новая пара токенов; 401 при отозванном/просроченном |
| `POST /api/auth/logout` | аутентифицирован | отзыв текущего refresh |
| `GET /api/auth/me` | аутентифицирован | профиль: id, login, name, роль |
| `POST /api/auth/change-password` | аутентифицирован | смена пароля (обязательна при `PasswordIsTemporary`), меняет SecurityStamp |

Управление пользователями (`UserController`, по образцу DG master):

| Эндпоинт | Доступ |
|---|---|
| `GET /api/users` (список, пагинация) | Administrator |
| `POST /api/users` (создание, роль, временный пароль) | Administrator |
| `PUT /api/users/{id}` (профиль, роль, блокировка) | Administrator |
| `POST /api/users/{id}/reset-password` | Administrator |

Все ответы — единый `ApiExceptionDetail` (Specular); коды 400/401/403/404/409/422 по ситуации.

## 3. Авторизация

- **Api**: `[Authorize]` по умолчанию для защищённых групп; публичные эндпоинты поиска/карточек/справочников — `[AllowAnonymous]`. Ролевые требования — политики иерархии: `RequireRole("Moderator")` пропускает Moderator и Administrator; `RequireRole("Administrator")` — только Administrator. Реализация — `IAuthorizationRequirement` + handler поверх клейма `role` (без Identity, ADR-11).
- **Web**: `AuthorizeView Roles="Moderator,Administrator"` на админ-компонентах; `[Authorize]` на страницах профиля; навигация скрывает недоступные разделы. Web **не проверяет права сам по себе** — источник истины Api (403 пробрасывается в UI как сообщение).
- Матрица доступа MVP (после Э9):

| Группа эндпоинтов | Гость | User | Moderator | Administrator |
|---|---|---|---|---|
| Поиск, карточки, справочники, stats | ✔ | ✔ | ✔ | ✔ |
| auth/me, change-password, logout | — | ✔ | ✔ | ✔ |
| Теги: создание/очередь модерации (v1.1) | — | — | ✔ | ✔ |
| Оценки важности (v1.2) | — | ✔ | ✔ | ✔ |
| Пользователи и роли | — | — | — | ✔ |

## 4. Что отложено (не входит в базу)

- Множественные роли на пользователя и гранулярные permissions (RBAC/ABAC) — при появлении функций, требующих разделения внутри роли.
- Публичная регистрация, подтверждение e-mail, восстановление пароля по почте.
- 2FA, внешние провайдеры (OAuth/OIDC), SSO.
- Аудит действий модераторов/админов в отдельную таблицу (пока — поля аудита `BaseAuditEntity` на сущностях).
- Несколько активных refresh-сессий (устройства).

## 5. Тесты (обязательные, по конвенциям ревью)

- Сервисы auth → реальные репозитории → InMemory/PostgreSQL, **без моков репозиториев**; AAA-комментарии.
- Логин: успех, неверный пароль, блокировка, временный пароль, счётчик неудач.
- Refresh: ротация, просроченный, отозванный, повторное использование → отзыв всех сессий, смена SecurityStamp.
- Ролевой доступ: 401 без токена, 403 при недостаточной роли, иерархия (Administrator проходит Moderator-эндпоинт).
- Хэширование: детерминированность при той же соли, разные хэши при разных солях, ≥ 100 000 итераций.
- Интеграционные: `TestWebApplicationFactory` + уникальная БД `dumalens_{Guid}`; `DependenciesTests` покрывает `AuthController`/`UserController`.