@echo off
setlocal
set "TOOL=%~dp0SlotboundDropRate.exe"
if not exist "%TOOL%" (
  echo Keep this launcher next to SlotboundDropRate.exe and try again.
  pause
  exit /b 1
)
start "" /wait "%TOOL%" --prepare-profile
exit /b %errorlevel%
