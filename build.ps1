#requires -version 5
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path $csc)) { throw '找不到 .NET Framework 4.x 的 csc.exe' }

$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force $dist | Out-Null

$out = Join-Path $dist 'SlotboundDropRate.exe'
& $csc /nologo /target:winexe /platform:x64 /codepage:65001 /optimize+ `
    /win32icon:"$root\assets\icon.ico" `
    /win32manifest:"$root\src\app.manifest" `
    /out:"$out" `
    /r:System.dll /r:System.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll `
    "$root\src\SlotboundDropRate.cs" "$root\src\RaritySession.cs" "$root\src\BuildCompatibility.cs" "$root\src\ProfileSetup.cs"
if ($LASTEXITCODE -ne 0) { throw "编译失败，退出码 $LASTEXITCODE" }

$profile = Join-Path $dist 'slotbound-compatibility.ini'
Copy-Item -LiteralPath (Join-Path $root 'slotbound-compatibility.ini') -Destination $profile -Force
$archive = Join-Path $dist 'SlotboundDropRate.zip'
$launcher = Join-Path $dist 'update-compatibility.bat'
Copy-Item -LiteralPath (Join-Path $root 'update-compatibility.bat') -Destination $launcher -Force
Compress-Archive -LiteralPath $out, $profile, $launcher -DestinationPath $archive -Force
Write-Host "OK -> $out, $profile, $launcher, $archive"
