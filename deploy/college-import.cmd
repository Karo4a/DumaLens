@echo off
chcp 65001 >nul
setlocal
rem ============================================================
rem  DumaLens — разовый импорт архива (разворачивание в колледже)
rem  Админ-права не нужны: база создаётся и мигрируется автоматически.
rem
rem  1) set ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=dumalens;Username=postgres;Password=<пароль>
rem  2) deploy\college-import.cmd [аргументы import-archive]
rem     напр.: deploy\college-import.cmd --workers 2 --batch 300
rem ============================================================
cd /d "%~dp0.."

if "%ConnectionStrings__DefaultConnection%"=="" (
    echo [ОШИБКА] Не задана переменная ConnectionStrings__DefaultConnection.
    echo Пример: set ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=dumalens;Username=postgres;Password=...
    exit /b 1
)
if not exist vote-master.zip (
    echo [ОШИБКА] Не найден vote-master.zip в корне репозитория.
    exit /b 1
)

set IMPORTER=DumaLens.Importer\bin\Release\net8.0\DumaLens.Importer.dll
if not exist "%IMPORTER%" (
    echo [1/3] Сборка Release ^(бинарники не найдены^)...
    dotnet build DumaLens.Importer -c Release --nologo
    if errorlevel 1 exit /b 1
) else (
    echo [1/3] Бинарники Release на месте, сборка не требуется.
)

echo [2/3] Импорт архива: bulk-режим ^(при нехватке прав — автоматический переход в обычный^)...
dotnet "%IMPORTER%" import-archive --zip vote-master.zip --bulk --log import-college.log %*
if errorlevel 1 (
    echo [ОШИБКА] Импорт завершился с ошибкой. Если данные загружены, но схема не восстановлена:
    echo   dotnet "%IMPORTER%" restore-schema
    exit /b 1
)

echo [3/3] Сверка с паспортом архива...
dotnet "%IMPORTER%" verify
if errorlevel 1 (
    echo [ОШИБКА] Контрольные суммы не сошлись — см. вывод выше.
    exit /b 1
)
echo Готово. Демонстрация: deploy\college-demo.cmd
