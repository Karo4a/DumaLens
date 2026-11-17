@echo off
chcp 65001 >nul
setlocal
rem ============================================================
rem  DumaLens — запуск демонстрации: Api (Swagger) + Web
rem  Требуется заданная ConnectionStrings__DefaultConnection (см. import.cmd).
rem  Остановка: закрыть окна DumaLens.Api и DumaLens.Web.
rem ============================================================
cd /d "%~dp0.."

if "%ConnectionStrings__DefaultConnection%"=="" (
    echo [ОШИБКА] Не задана переменная ConnectionStrings__DefaultConnection.
    exit /b 1
)

if not exist DumaLens.Api\bin\Release\net8.0\DumaLens.Api.dll (
    echo Сборка Api...
    dotnet build DumaLens.Api -c Release --nologo
    if errorlevel 1 exit /b 1
)
if not exist DumaLens.Web\bin\Release\net8.0\DumaLens.Web.dll (
    echo Сборка Web...
    dotnet build DumaLens.Web -c Release --nologo
    if errorlevel 1 exit /b 1
)

rem Development включает Swagger UI у Api
set ASPNETCORE_ENVIRONMENT=Development
start "DumaLens.Api" cmd /c "cd /d DumaLens.Api\bin\Release\net8.0 && dotnet DumaLens.Api.dll --urls http://localhost:5200"
start "DumaLens.Web" cmd /c "cd /d DumaLens.Web\bin\Release\net8.0 && dotnet DumaLens.Web.dll --urls http://localhost:5100"

echo Api:  http://localhost:5200/swagger
echo Web:  http://localhost:5100 ^(заглушка до реализации Э8^)
