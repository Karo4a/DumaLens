# 14. Приёмка Э9 — аутентификация и роли (база)

Дата приёмки: 19.11.2026. Спецификация — [07-security.md](07-security.md), описание реализации — [13-e9-auth.md](13-e9-auth.md). Критерии — Docs/06 §Э9 и П-10 ТЗ §7.

## Объём

- `SecurityHelper`: PBKDF2-SHA256, соль/хэш 32 байта, 100 000 итераций, `FixedTimeEquals`; refresh — 256 бит, в базе только SHA-256-хэш (ADR-12, ADR-13).
- Сервисы: `TokenService` (JWT JWS: `sub`/`name`/`role`/`security_stamp`/`jti`, 15 мин), `AuthService` (login/refresh/logout/change-password/me; ротация, отзыв всех сессий при повторном использовании отозванного refresh, смена `SecurityStamp` при смене пароля/роли/блокировке/сбросе), `UserService` (CRUD, защита последнего администратора), валидаторы FluentValidation.
- Api: `AuthController`, `UsersController`, JwtBearer, политики иерархии ролей (`MinimumRoleHandler`), rate limit логина (фиксированное окно на IP, 429), счётчик попыток + блокировка 5/15 мин.
- Web: `ClientTokenStore` (ProtectedLocalStorage), `AuthDelegatingHandler` (Bearer + авто-refresh при 401, один refresh на цепь), `UserState`/`UserStateInitializer`/`UserStateAuthenticationStateProvider`, `ProtectedContent`, страницы `/login`, `/change-password`, `/profile`, `/admin/users`, слот аутентификации в навигации.
- CLI `create-admin` (временный пароль → принудительная смена при первом входе); NSwag-клиенты `AuthClient`/`UsersClient`.
- Миграция `AuthSessionFields`: `Users.LastFailedLoginAt`, `RefreshTokens.SecurityStamp`, фильтр уникального индекса почты (`Email <> ''`).

## Критерии приёмки (Docs/06, Э9 — П-10)

| Критерий | Результат |
|---|---|
| Логин/refresh/logout работают; ротация, отзыв при повторном использовании и смене SecurityStamp проверены тестами | Выполнено: `AuthServiceTests` (18 сценариев на одноразовой PG-базе, реальные репозитории без моков) + `AuthApiTests` по HTTP + живая приёмка (§4) |
| Три роли засеяны; Администратор проходит Moderator-эндпоинты; Пользователь получает 403 на админ-эндпоинтах; публичное чтение без токена | Выполнено: seed `UserRolesDefault` (миграция `InitialCreate`); `MinimumRoleHandlerTests` (Administrator проходит требование Moderator); `UsersApiTests` (User/Moderator → 403, Administrator → 200, без токена → 401); `AuthApiTests.PublicReadingShouldStayAnonymous`. Moderator-эндпоинты данных появятся на Э11 — политика готова |
| Пароли — PBKDF2 ≥ 100 000 итераций; секретов в репозитории нет (О-7) | Выполнено: `SecurityHelperTests` (независимое вычисление PBKDF2, 100 000 итераций); `appsettings.json` — пустой `SigningKey`, строка подключения — плейсхолдер; ключ и пароль БД — только окружение/user-secrets |
| Ревью блокирующих нет | Выполнено: полный гейт `build.ps1` — 0 ошибок, 0 предупреждений, `dotnet format --verify-no-changes` чисто |

## Тесты (полный гейт build.ps1, 19.11.2026)

| Проект | Пройдено | Пропущено |
|---|---|---|
| DumaLens.Parsing.Tests | 159 | 0 |
| DumaLens.Services.Tests | 103 | 0 |
| DumaLens.Repositories.Tests | 67 | 0 |
| DumaLens.Api.Tests | 65 | 0 |
| DumaLens.Importer.Tests | 49 | 0 |
| DumaLens.Web.Tests | 46 | 0 |
| DumaLens.Database.Tests | 36 | 0 |
| **Итого** | **525** | **0** |

Новые тесты Э9: `SecurityHelperTests`, `RoleHelperTests`, `TokenServiceTests`, `AuthServiceTests`, `UserServiceTests` (одноразовая PostgreSQL-база на каждый тест — изоляция мутирующих сценариев; InMemory не используется: провайдер игнорирует фильтры уникальных индексов, ротация refresh на нём невоспроизводима), `AuthApiTests`, `UsersApiTests`, `MinimumRoleHandlerTests`, `LoginRateLimitTests` (отдельный хост с лимитом 3), `AuthPageRenderTests`, `UserStateTests`, `AuthDelegatingHandlerTests`, `CreateAdminWiringTests`.

## Живая приёмка (одноразовая база dumalens_e9_accept)

База создана `dotnet ef database update`, администратор — CLI `create-admin --login admin` (временный пароль). Api — порт 5299 с настроенным `Auth__SigningKey` (32 случайных байта), Web — порт 5199. Строка подключения — только из интеграционного шаблона окружения, в вывод не попадала.

### HTTP-сценарий (31 запрос, 19 проверок — все пройдены)

1. Публичное чтение без токена — 200.
2. Логин с временным паролем — пара токенов + профиль (`passwordIsTemporary = true`); `me` без токена — 401, с токеном — 200.
3. Смена пароля — 204; старый refresh — 401; вход со старым паролем — 401; вход с новым — 200, флаг временного снят.
4. Ротация refresh — новая пара; повторное использование отозванного — 401; новый токен после кражи тоже 401 (все сессии отозваны).
5. Создание User и Moderator — 200 (временный пароль); дубликат логина — 409; неизвестная роль — 422.
6. Матрица доступа: User → 403, Moderator → 403, Administrator → 200 на `/api/users`; без токена — 401.
7. Смена роли отозвала сессию пользователя (refresh 401); блокировка запретила вход (401).
8. Сброс пароля — временный пароль выдан, вход по нему работает с флагом временного.
9. Разжалование последнего администратора — 422.
10. Выход — 204, refresh отозван (401).

### Браузерный сценарий (Web, Blazor-цепь)

1. `/login`: вход администратором → редирект на главную; навигация: логин, «Пользователи» (Administrator), «Выйти».
2. `/admin/users`: список из Api (3 пользователя, статусы «Активен»/«Заблокирован»/«Временный пароль»); создание пользователя через форму — строка появилась в списке.
3. `/profile`: логин, имя, почта, роль.
4. Выход → главная, навигация анонимная («Войти»).
5. Вход с временным паролем → принудительный редирект `/change-password` с баннером; смена пароля → `/login?message=password-changed`; повторный вход с новым паролем → главная (роль User, без «Пользователи»); `/admin/users` → «Недостаточно прав для просмотра этой страницы».
6. Сброс пароля через UI — временный пароль показан один раз (16 символов); полный цикл принудительной смены повторён успешно.

База приёмки удалена после прогона (`DROP DATABASE ... WITH (FORCE)` через интеграционную строку, паттерн ADR-3).

## Исправления по ходе приёмки

- `ImporterHost`: не был зарегистрирован `IMapper` — CLI `create-admin` падал при резолве `UserService` (поймано `CreateAdminWiringTests`).
- Web: `[Authorize]` на страницах давал 500 при пререндере (endpoint-метаданные → `AuthorizationMiddleware` → `IAuthenticationService`, которого нет и не может быть при токенах в браузере). Защита переведена на `ProtectedContent` (`AuthorizeView` + gate инициализации) — механика DocumentGenerator.Web.
- Web: гонка редиректов после смены пароля/выхода (`UserState.Clear()` → `RedirectToLogin` перебивал адрес возврата). Смена пароля и выход выполняются полной перезагрузкой (`forceLoad`) без ручной очистки состояния.
- Api: `AuthOptions` связывается лениво из DI — конфигурация тестового хоста применяется после регистрации сервисов в `Main` (иначе rate limit тест-хоста не применялся).

## Известные ограничения (не дефекты)

- Cookie-поток для браузерных клиентов вне Web — после MVP (07-security.md §2.3).
- Access-токен живёт до истечения срока после отзыва сессии (≤ 15 мин): проверка `SecurityStamp` выполняется при refresh, не на каждом запросе (осознанное упрощение базы, 07-security.md §2.1).
- Rate limit логина считается на IP: запросы Web-хоста приходят с одного серверного IP, поэтому лимит окна щедрый (20/60 с); основная защита от подбора — блокировка учётной записи.
- В Development без `Auth__SigningKey` Api генерирует временный ключ на процесс (сессии не переживают перезапуск); в Production — отказ запуска.