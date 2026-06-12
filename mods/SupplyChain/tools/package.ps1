# Builds the mod and produces a Thunderstore-ready zip at .\Thunderstore\SupplyChain.zip
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

dotnet build (Join-Path $root 'SupplyChain\SupplyChain.csproj') -c Debug
if ($LASTEXITCODE -ne 0) { throw 'build failed' }

$ts = Join-Path $root 'Thunderstore'
Copy-Item (Join-Path $root 'SupplyChain\bin\Debug\netstandard2.1\SupplyChain.dll') $ts -Force

$zip = Join-Path $ts 'SupplyChain.zip'
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path (Join-Path $ts 'manifest.json'), (Join-Path $ts 'README.md'), (Join-Path $ts 'icon.png'), (Join-Path $ts 'SupplyChain.dll') -DestinationPath $zip
Write-Host "packaged: $zip"
