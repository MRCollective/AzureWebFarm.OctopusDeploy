powershell.exe -ExecutionPolicy Unrestricted Startup\startup.ps1 >> "%TEMP%\StartupLog.txt" 2>&1
IF NOT "%errorlevel%" == "0" GOTO ERROR

echo "Successfully executed startup script" >> "%TEMP%\StartupLog.txt" 2>&1

REM Uncomment these lines for debugging locally when there isn't an error (or put a breakpoint in WebRole.OnStart()
REM start %temp%
REM start /w cmd

EXIT /B 0
GOTO END

:ERROR

echo "Error starting role" >> "%TEMP%\StartupLog.txt" 2>&1

IF "%ComputeEmulatorRunning%" == "true" (
	start %temp%
	start "%temp%\StartupLog.txt"
	start /w cmd
)
EXIT /B 1

:END

