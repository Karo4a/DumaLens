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

rem Инкрементальная сборка при каждом запуске: без изменений в коде — быстрый
rem no-op, с изменениями — актуальные бинарники (устаревшая версия не запустится)
echo Сборка Api (Release)...
dotnet build DumaLens.Api -c Release --nologo
if errorlevel 1 exit /b 1
echo Сборка Web (Release)...
dotnet build DumaLens.Web -c Release --nologo
if errorlevel 1 exit /b 1

rem Development включает Swagger UI у Api
set ASPNETCORE_ENVIRONMENT=Development
start "DumaLens.Api" cmd /c "cd /d DumaLens.Api\bin\Release\net8.0 && dotnet DumaLens.Api.dll --urls http://localhost:5200"
start "DumaLens.Web" cmd /c "cd /d DumaLens.Web\bin\Release\net8.0 && dotnet DumaLens.Web.dll --urls http://localhost:5100"

echo Api:  http://localhost:5200/swagger
echo Web:  http://localhost:5100

rem Ждём готовности хостов (до 30 попыток с паузой 1 с) и открываем браузер;
rem Web открывается последним — активной вкладкой
powershell -NoProfile -Command "foreach($u in @('http://localhost:5200/swagger','http://localhost:5100/')){ $ok=$false; for($i=0;$i -lt 30;$i++){ try{ $r=Invoke-WebRequest -UseBasicParsing -Uri $u -TimeoutSec 2; if($r.StatusCode -eq 200){$ok=$true;break} }catch{}; Start-Sleep -Seconds 1 }; if($ok){ Start-Process $u } else { Write-Host ('Хост не ответил за 30 попыток: '+$u+' — откройте вручную') } }"