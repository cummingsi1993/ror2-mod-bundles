# Builds the mod and produces a Thunderstore-ready zip at .\Thunderstore\AuditDepartment.zip
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

dotnet build (Join-Path $root 'AuditDepartment\AuditDepartment.csproj') -c Debug
if ($LASTEXITCODE -ne 0) { throw 'build failed' }

$ts = Join-Path $root 'Thunderstore'
Copy-Item (Join-Path $root 'AuditDepartment\bin\Debug\netstandard2.1\AuditDepartment.dll') $ts -Force

$zip = Join-Path $ts 'AuditDepartment.zip'
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path (Join-Path $ts 'manifest.json'), (Join-Path $ts 'README.md'), (Join-Path $ts 'icon.png'), (Join-Path $ts 'AuditDepartment.dll') -DestinationPath $zip
Write-Host "packaged: $zip"
