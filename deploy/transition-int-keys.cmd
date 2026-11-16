@echo off
chcp 65001 >nul
setlocal EnableDelayedExpansion
rem ============================================================
rem  DumaLens — переход на int-ключи DeputyVotes (ADR-14)
rem  Миграция IntKeysDeputyVotes пересоздаёт DeputyVotes с узкими
rem  ключами; данные восстанавливаются полным реимпортом архива.
rem  Админ-права не нужны; dotnet-ef/psql не нужны — миграцию
rem  применяет сам импортёр (MigrateAsync).
rem
rem  1) set ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=dumalens;Username=postgres;Password=***
rem  2) Остановить Api и Web (отключение FK-триггеров действует на все сеансы).
rem  3) deploy\transition-int-keys.cmd [--yes]
rem
rem  Шаги: recalc-stats (миграция + очистка агрегатов) ->
rem        import-archive --bulk -> verify.
rem  Откат: тег pre-int-keys (код до перехода), Docs/08-deployment-college.md.
rem ============================================================
cd /d "%~dp0.."

if "%ConnectionStrings__DefaultConnection%"=="" (
    echo [ОШИБКА] Не задана переменная ConnectionStrings__DefaultConnection.
    echo Пример: set ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=dumalens;Username=postgres;Password=***
    exit /b 1
)
if not exist vote-master.zip (
    echo [ОШИБКА] Не найден vote-master.zip в корне репозитория.
    exit /b 1
)

set "CONN=%ConnectionStrings__DefaultConnection%"
set "DBPART=!CONN:*Database=!"
for /f "delims=;" %%a in ("!DBPART!") do set "TARGETDB=%%a"
set "TARGETDB=!TARGETDB:~1!"
echo ============================================================
echo  Цель перехода: база !TARGETDB!
echo  DeputyVotes будет пересоздана (все индивидуальные голоса
echo  удалены и восстановлены реимпортом), VoteFactionStats очищена.
echo  Операция занимает ~6-15 минут; откат — по Docs/08-deployment-college.md.
echo ============================================================
if /i not "%~1"=="--yes" (
    set /p CONFIRM="Продолжить? Введите yes: "
    if /i not "!CONFIRM!"=="yes" (
        echo Отменено.
        exit /b 1
    )
)

set IMPORTER=DumaLens.Importer\bin\Release\net8.0\DumaLens.Importer.dll
if not exist "%IMPORTER%" (
    echo [1/4] Сборка Release ^(бинарники не найдены^)...
    dotnet build DumaLens.Importer -c Release --nologo
    if errorlevel 1 exit /b 1
) else (
    echo [1/4] Бинарники Release на месте, сборка не требуется.
)

echo [2/4] Миграция IntKeysDeputyVotes + очистка агрегатов ^(recalc-stats^)...
dotnet "%IMPORTER%" recalc-stats > transition-recalc.log 2>&1
if errorlevel 1 (
    echo [ОШИБКА] Миграция/пересчёт завершились ошибкой — см. transition-recalc.log.
    exit /b 1
)
type transition-recalc.log

echo [3/4] Реимпорт архива: bulk-режим...
dotnet "%IMPORTER%" import-archive --zip vote-master.zip --bulk --log transition-import.log
if errorlevel 1 (
    echo [ОШИБКА] Импорт завершился с ошибкой. Если данные загружены, но схема не восстановлена:
    echo   dotnet "%IMPORTER%" restore-schema
    exit /b 1
)

echo [4/4] Сверка с паспортом архива...
dotnet "%IMPORTER%" verify
if errorlevel 1 (
    echo [ОШИБКА] Контрольные суммы не сошлись — см. вывод выше.
    exit /b 1
)
echo Готово: база !TARGETDB! переведена на int-ключи ^(ADR-14^).