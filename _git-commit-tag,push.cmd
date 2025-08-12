@echo off
REM ================================
REM Git Commit + Tag + Push Script
REM ================================

REM Eingabe Commit-Nachricht
set /p cmsg=Bitte Commit-Nachricht eingeben: 

REM Eingabe Tag-Name
set /p tname=Bitte Tag-Name eingeben (z.B. v1.0): 

REM Eingabe Tag-Beschreibung
set /p tmsg=Bitte Tag-Beschreibung eingeben:

echo.
echo ==== Git Status ====
git status
echo.

REM Änderungen hinzufügen
git add .

REM Commit erstellen
git commit -m "%cmsg%"
if errorlevel 1 (
    echo.
    echo *** Commit fehlgeschlagen oder keine Änderungen vorhanden. ***
    pause
    exit /b 1
)

REM Tag setzen
git tag -a %tname% -m "%tmsg%"

REM Push zum Haupt-Branch
git push origin main

REM Tag zum Remote-Repository pushen
git push origin %tname%

echo.
echo *** Fertig: Commit und Tag "%tname%" wurden gepusht. ***
pause
