#requires -version 5
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path $csc)) { throw '找不到 .NET Framework 4.x 的 csc.exe' }

$temp = Join-Path ([IO.Path]::GetTempPath()) ('slotbound-tests-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null
try {
    $out = Join-Path $temp 'SlotboundTests.exe'
    & $csc /nologo /target:exe /platform:x64 /codepage:65001 /out:"$out" `
        /r:System.dll /r:System.Core.dll `
        "$PSScriptRoot\RarityTests.cs" "$PSScriptRoot\CompatibilityTests.cs" "$PSScriptRoot\ProfileSetupTests.cs" `
        "$root\src\RaritySession.cs" "$root\src\BuildCompatibility.cs" "$root\src\ProfileSetup.cs"
    if ($LASTEXITCODE -ne 0) { throw "Test compilation failed with exit code $LASTEXITCODE" }
    & $out
    if ($LASTEXITCODE -ne 0) { throw "Tests failed with exit code $LASTEXITCODE" }
} finally {
    Remove-Item -LiteralPath $temp -Recurse -Force
}
