@echo off
setlocal enabledelayedexpansion
if "%1"=="build" goto build
if "%1"=="linux" goto linux
if "%1"=="win" goto win
if "%1"=="win-nodb" goto win_nodb
if "%1"=="save-images" goto save_images
if "%1"=="load-images" goto load_images
if "%1"=="backup" goto backup
if "%1"=="db-info" goto db_info
if "%1"=="down" goto down
if "%1"=="clean" goto clean
goto help

:help
echo Usage:
echo   make build              - build the Docker image
echo   make linux              - start app + PostgreSQL in Docker
echo   make win [ip1 ip2 ...]  - start PostgreSQL in Docker and run the Windows exe
echo   make win-nodb [ip ...]  - run the Windows exe without Docker / PostgreSQL
echo   make save-images        - save Docker images to tar
echo   make load-images        - load Docker images from tar
echo   make backup             - dump the database
echo   make db-info            - write a database report to db_report.txt
echo   make down               - stop containers
echo   make clean              - remove containers and volumes
goto end

:build
docker build -t fanuc-focas-console:latest .
if errorlevel 1 (
    echo ERROR: docker build failed.
    exit /b 1
)
goto end

:save_images
echo Saving application image...
docker save -o fanuc-focas-console.tar fanuc-focas-console:latest
if errorlevel 1 (
    echo ERROR: failed to save application image.
    exit /b 1
)
echo Saving PostgreSQL image...
docker pull postgres:16-alpine
if errorlevel 1 (
    echo ERROR: failed to pull postgres image.
    exit /b 1
)
docker save -o postgres-16-alpine.tar postgres:16-alpine
if errorlevel 1 (
    echo ERROR: failed to save postgres image.
    exit /b 1
)
echo Images saved: fanuc-focas-console.tar, postgres-16-alpine.tar
goto end

:load_images
if exist fanuc-focas-console.tar (
    echo Loading application image...
    docker load -i fanuc-focas-console.tar
    if errorlevel 1 (
        echo ERROR: failed to load application image.
        exit /b 1
    )
) else (
    echo WARNING: fanuc-focas-console.tar not found, skipping.
)
if exist postgres-16-alpine.tar (
    echo Loading PostgreSQL image...
    docker load -i postgres-16-alpine.tar
    if errorlevel 1 (
        echo ERROR: failed to load postgres image.
        exit /b 1
    )
) else (
    echo WARNING: postgres-16-alpine.tar not found, skipping.
)
goto end

:linux
call :load_images
echo Removing old PostgreSQL container...
docker rm -f fanuc_postgres 2>nul
echo Starting via docker compose...
docker compose up -d --no-build
if errorlevel 1 (
    echo ERROR: docker compose up failed.
    exit /b 1
)
goto end

:win
call :load_images
echo Removing old PostgreSQL container...
docker rm -f fanuc_postgres 2>nul

echo Starting PostgreSQL in Docker...
docker run -d --name fanuc_postgres ^
  -e POSTGRES_USER=postgres ^
  -e POSTGRES_PASSWORD=root ^
  -e POSTGRES_DB=fanuc_data ^
  -p 5432:5432 ^
  postgres:16-alpine
if errorlevel 1 (
    echo ERROR: failed to start PostgreSQL container.
    exit /b 1
)

echo Waiting for PostgreSQL to become ready...
timeout /t 5 >nul

echo Publishing Windows exe (x86 self-contained)...
dotnet publish -c Release -p:PlatformTarget=x86 -p:RuntimeIdentifier=win-x86 --self-contained true -o bin\publish\win-x86
if errorlevel 1 (
    echo ERROR: dotnet publish failed.
    exit /b 1
)

echo Copying native libraries...
copy /Y NativeLibs\Fwlib32.dll bin\publish\win-x86\Fwlib32.dll >nul
if errorlevel 1 (
    echo ERROR: Fwlib32.dll not found in NativeLibs. See NativeLibs/README.md.
    exit /b 1
)
copy /Y NativeLibs\fwlibe1.dll bin\publish\win-x86\fwlibe1.dll >nul
if errorlevel 1 (
    echo ERROR: fwlibe1.dll not found in NativeLibs. See NativeLibs/README.md.
    exit /b 1
)

echo Running Windows exe...
if not "%~2"=="" (
    bin\publish\win-x86\FanucFocasConsole.exe %2 %3 %4 %5 %6
) else (
    bin\publish\win-x86\FanucFocasConsole.exe
)

echo PostgreSQL container is left running.
goto end

:win_nodb
echo Publishing Windows exe (x86 self-contained)...
dotnet publish -c Release -p:PlatformTarget=x86 -p:RuntimeIdentifier=win-x86 --self-contained true -o bin\publish\win-x86
if errorlevel 1 (
    echo ERROR: dotnet publish failed.
    exit /b 1
)

echo Copying native libraries...
copy /Y NativeLibs\Fwlib32.dll bin\publish\win-x86\Fwlib32.dll >nul
if errorlevel 1 (
    echo ERROR: Fwlib32.dll not found in NativeLibs. See NativeLibs/README.md.
    exit /b 1
)
copy /Y NativeLibs\fwlibe1.dll bin\publish\win-x86\fwlibe1.dll >nul
if errorlevel 1 (
    echo ERROR: fwlibe1.dll not found in NativeLibs. See NativeLibs/README.md.
    exit /b 1
)

echo Running Windows exe (no DB)...
if not "%~2"=="" (
    bin\publish\win-x86\FanucFocasConsole.exe %2 %3 %4 %5 %6
) else (
    bin\publish\win-x86\FanucFocasConsole.exe
)
goto end

:backup
echo Creating database dump...
docker exec fanuc_postgres pg_dump -U postgres fanuc_data > backup_fanuc_data.sql
if errorlevel 1 (
    echo ERROR: backup failed. Is the container running?
    exit /b 1
)
echo Backup saved to backup_fanuc_data.sql
goto end

:db_info
echo Generating database report...
echo === Database report: fanuc_data === > db_report.txt
echo Date: %date% %time% >> db_report.txt
echo. >> db_report.txt

echo Tables: >> db_report.txt
docker exec fanuc_postgres psql -U postgres -d fanuc_data -c "\dt" >> db_report.txt
if errorlevel 1 (
    echo ERROR: failed to query tables. Is the container running?
    exit /b 1
)

echo. >> db_report.txt
echo Row counts: >> db_report.txt
docker exec fanuc_postgres psql -U postgres -d fanuc_data -c ^
  "SELECT 'snapshots' AS table, COUNT(*) FROM snapshots UNION ALL SELECT 'status', COUNT(*) FROM status UNION ALL SELECT 'loads', COUNT(*) FROM loads UNION ALL SELECT 'working_time', COUNT(*) FROM working_time UNION ALL SELECT 'alarms', COUNT(*) FROM alarms;" >> db_report.txt

echo. >> db_report.txt
echo Latest 10 snapshots: >> db_report.txt
docker exec fanuc_postgres psql -U postgres -d fanuc_data -c ^
  "SELECT id, machine_ip, timestamp FROM snapshots ORDER BY id DESC LIMIT 10;" >> db_report.txt

echo. >> db_report.txt
echo Latest status rows: >> db_report.txt
docker exec fanuc_postgres psql -U postgres -d fanuc_data -c ^
  "SELECT * FROM status ORDER BY id DESC LIMIT 10;" >> db_report.txt

echo. >> db_report.txt
echo Latest loads rows: >> db_report.txt
docker exec fanuc_postgres psql -U postgres -d fanuc_data -c ^
  "SELECT * FROM loads ORDER BY id DESC LIMIT 10;" >> db_report.txt

echo. >> db_report.txt
echo Latest working_time rows: >> db_report.txt
docker exec fanuc_postgres psql -U postgres -d fanuc_data -c ^
  "SELECT * FROM working_time ORDER BY id DESC LIMIT 10;" >> db_report.txt

echo. >> db_report.txt
echo Latest alarms rows: >> db_report.txt
docker exec fanuc_postgres psql -U postgres -d fanuc_data -c ^
  "SELECT * FROM alarms ORDER BY id DESC LIMIT 10;" >> db_report.txt

echo Report saved to db_report.txt
type db_report.txt
goto end

:down
docker compose down
goto end

:clean
docker compose down -v
docker rm -f fanuc_postgres 2>nul
goto end

:end
endlocal
