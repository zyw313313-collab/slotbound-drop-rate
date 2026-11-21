@echo off
setlocal
title Slotbound Drop Rate - Installer
set "REPO=zyw313313-collab/slotbound-drop-rate"
set "DEST=%LOCALAPPDATA%\SlotboundDropRate"
set "EXE=%DEST%\SlotboundDropRate.exe"
set "URL=https://github.com/%REPO%/releases/latest/download/SlotboundDropRate.exe"

echo.
echo  Slotbound Drop Rate / 爆率调节 - Installer
echo  -----------------------------------------
echo  Install to / 安装到: %DEST%
echo.

if not exist "%DEST%" mkdir "%DEST%"

taskkill /f /im SlotboundDropRate.exe >nul 2>nul

echo  Downloading... / 正在下载...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ProgressPreference='SilentlyContinue'; Invoke-WebRequest -Uri '%URL%' -OutFile '%EXE%' -UseBasicParsing"
if not exist "%EXE%" (
  echo  Download failed. Check your network, or grab it from GitHub Releases.
  echo  下载失败：请检查网络，或直接到 GitHub Releases 手动下载。
  pause
  exit /b 1
)

echo  Creating desktop shortcut... / 创建桌面快捷方式...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$s=(New-Object -ComObject WScript.Shell).CreateShortcut([Environment]::GetFolderPath('Desktop')+'\Slotbound Drop Rate.lnk'); $s.TargetPath='%EXE%'; $s.WorkingDirectory='%DEST%'; $s.Save()"

echo  Launching... / 启动...
start "" "%EXE%"
echo.
echo  Done. Open it from your desktop next time.
echo  安装完成。以后从桌面「Slotbound Drop Rate」打开即可。
timeout /t 3 >nul
