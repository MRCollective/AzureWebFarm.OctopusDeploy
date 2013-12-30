powershell.exe -ExecutionPolicy Unrestricted Startup\startup.ps1 >> "%TEMP%\StartupLog.txt" 2>&1
if NOT ERRORLEVEL 0 GOTO ERROR
cd /D "%PathToInstall%\Tentacle\Agent"
echo %cd% >> "%TEMP%\StartupLog.txt" 2>&1
if NOT ERRORLEVEL 0 GOTO ERROR
Tentacle.exe create-instance --instance Tentacle --config "%PathToInstall%\Tentacle.config" >> "%TEMP%\StartupLog.txt" 2>&1
if NOT ERRORLEVEL 0 GOTO ERROR

echo "success" >> "%TEMP%\StartupLog.txt" 2>&1

start %temp%
start /w cmd

EXIT /B 0
GOTO END
:ERROR

echo "error" >> "%TEMP%\StartupLog.txt" 2>&1

start %temp%
start /w cmd

EXIT /B 1
:END