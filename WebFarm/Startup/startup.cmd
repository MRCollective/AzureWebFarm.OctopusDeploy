powershell.exe -ExecutionPolicy Unrestricted Startup\startup.ps1 >> "%TEMP%\StartupLog.txt" 2>&1
start %temp%
start /w cmd
EXIT /B %errorlevel%