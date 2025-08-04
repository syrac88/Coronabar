@echo off
REM --------- Git Commit & Push Batch-Script für Unity-Projekte ---------
REM Kompatibel mit deutscher/englischer Git- und Windows-Konfiguration

REM Prüfe, ob ein Argument (Commit-Text) übergeben wurde
IF "%~1"=="" (
    set /p cmsg=Bitte Commit-Nachricht eingeben:
) ELSE (
    set cmsg=%*
)

REM Zeige aktuellen Projekt-Status
echo.
git status
echo.

REM Staged alle Änderungen (auch neue Dateien)
git add .

REM Commit
git commit -m "%cmsg%"

if errorlevel 1 (
    echo.
    echo Kein Commit erstellt – vermutlich keine Änderungen!
    pause
    exit /b 1
)

REM Push zum Standard-Remote (origin) und Branch (main)
git push origin main

echo.
echo Fertig: Commits wurden ins Remote gepusht.
pause
