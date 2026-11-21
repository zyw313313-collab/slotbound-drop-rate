@echo off
setlocal
title Slotbound 爆率调节 - 一键安装
set "REPO=zyw313313-collab/slotbound-drop-rate"
set "DEST=%LOCALAPPDATA%\SlotboundDropRate"
set "EXE=%DEST%\SlotboundDropRate.exe"
set "URL=https://github.com/%REPO%/releases/latest/download/SlotboundDropRate.exe"

echo.
echo  Slotbound 爆率调节 - 一键安装
echo  -------------------------------
echo  安装到: %DEST%
echo.

if not exist "%DEST%" mkdir "%DEST%"

taskkill /f /im SlotboundDropRate.exe >nul 2>nul

echo  正在下载...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ProgressPreference='SilentlyContinue'; Invoke-WebRequest -Uri '%URL%' -OutFile '%EXE%' -UseBasicParsing"
if not exist "%EXE%" (
  echo  下载失败：请检查网络，或直接到 GitHub Releases 手动下载。
  pause
  exit /b 1
)

echo  创建桌面快捷方式...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$s=(New-Object -ComObject WScript.Shell).CreateShortcut([Environment]::GetFolderPath('Desktop')+'\Slotbound 爆率调节.lnk'); $s.TargetPath='%EXE%'; $s.WorkingDirectory='%DEST%'; $s.Save()"

echo  启动...
start "" "%EXE%"
echo.
echo  安装完成。以后从桌面「Slotbound 爆率调节」打开即可。
timeout /t 3 >nul
