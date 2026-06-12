# Publishes Thunderstore\AuditDepartment.zip to Thunderstore via tcli.
# Usage: .\publish_thunderstore.ps1 -Team <your-team-name> -Token <service-account-token>
# Get a token: thunderstore.io -> Teams -> your team -> Service Accounts -> Add (copy the token).
param(
    [string]$Team = 'Isaac',
    [Parameter(Mandatory = $true)][string]$Token
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$ts = Join-Path $root 'Thunderstore'

$manifest = Get-Content (Join-Path $ts 'manifest.json') | ConvertFrom-Json

# tcli wants a project toml; generate it to match the manifest + chosen team
@"
[config]
schemaVersion = "0.0.1"

[package]
namespace = "$Team"
name = "$($manifest.name)"
versionNumber = "$($manifest.version_number)"
description = "$($manifest.description)"
websiteUrl = "$($manifest.website_url)"
containsNsfwContent = false

[package.dependencies]
$($manifest.dependencies | ForEach-Object { $parts = $_ -split '-'; "`"$($parts[0])-$($parts[1])`" = `"$($parts[2])`"" } | Out-String)
[publish]
repository = "https://thunderstore.io"
communities = ["riskofrain2"]
# ai-generated is required by Thunderstore TOS for AI-assisted mods
categories = ["items", "artifacts", "ai-generated"]
"@ | Out-File (Join-Path $ts 'thunderstore.toml') -Encoding utf8

tcli publish --config-path (Join-Path $ts 'thunderstore.toml') --file (Join-Path $ts 'AuditDepartment.zip') --token $Token
