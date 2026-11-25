@echo off
setlocal
title Slotbound Drop Rate - Installer
set "REPO=zyw313313-collab/slotbound-drop-rate"
set "DEST=%LOCALAPPDATA%\SlotboundDropRate"
set "EXE=%DEST%\SlotboundDropRate.exe"
set "URL=https://github.com/%REPO%/releases/latest/download/SlotboundDropRate.zip"

echo.
echo  Slotbound Drop Rate - Installer
echo  -----------------------------------------
echo  Install to: "%DEST%"
echo.

echo  Downloading and verifying package...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; $stage=New-Item -ItemType Directory -Path (Join-Path ([IO.Path]::GetTempPath()) ('SlotboundDropRate-' + [Guid]::NewGuid().ToString('N'))); try { $zip=Join-Path $stage.FullName 'SlotboundDropRate.zip'; Invoke-WebRequest -Uri $env:URL -OutFile $zip -UseBasicParsing; $files=Join-Path $stage.FullName 'files'; Expand-Archive -LiteralPath $zip -DestinationPath $files; $payload=@((Join-Path $files 'SlotboundDropRate.exe'), (Join-Path $files 'slotbound-compatibility.ini')); foreach ($file in $payload) { $item=Get-Item -LiteralPath $file; if ($item.PSIsContainer -or $item.Length -eq 0) { throw ('Invalid package entry: ' + $file) } }; Get-Process -Name SlotboundDropRate -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $env:EXE } | Stop-Process -Force; New-Item -ItemType Directory -Path $env:DEST -Force | Out-Null; Copy-Item -LiteralPath $payload -Destination $env:DEST -Force } finally { Remove-Item -LiteralPath $stage.FullName -Recurse -Force }"
if errorlevel 1 (
  echo  Installation failed. Check your network or download the ZIP from GitHub Releases.
  pause
  exit /b 1
)

echo  Creating desktop shortcut...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$shortcut=(New-Object -ComObject WScript.Shell).CreateShortcut([Environment]::GetFolderPath('Desktop')+'\Slotbound Drop Rate.lnk'); $shortcut.TargetPath=$env:EXE; $shortcut.WorkingDirectory=$env:DEST; $shortcut.Save()"

echo  Launching...
start "" "%EXE%"
echo.
echo  Done. Open it from your desktop next time.
timeout /t 3 >nul
