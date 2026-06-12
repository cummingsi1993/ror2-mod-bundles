# One-off inspection of game internals needed for tier/HUD/pickup-model fixes.
Add-Type -Path "C:\Users\cummi\.nuget\packages\mono.cecil\0.11.4\lib\net40\Mono.Cecil.dll"
$managed = "D:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $managed "RoR2.dll"))
$m = $asm.MainModule

Write-Host "=== ItemDef.set_tier IL ==="
$itemDef = $m.GetType('RoR2.ItemDef')
$setter = $itemDef.Methods | Where-Object { $_.Name -eq 'set_tier' }
foreach ($i in $setter.Body.Instructions) { Write-Host ("  " + $i.ToString()) }

Write-Host "=== ItemDef tier-related fields ==="
$itemDef.Fields | Where-Object { $_.Name -match 'tier|Tier' } | ForEach-Object {
    $vis = if ($_.IsPublic) {'public'} else {'private'}
    Write-Host ("  $vis $($_.FieldType.Name) $($_.Name)")
}

Write-Host "=== ItemTierCatalog members ==="
$itc = $m.GetType('RoR2.ItemTierCatalog')
$itc.Methods | Where-Object { $_.Name -match 'Find|Get' } | ForEach-Object {
    $vis = if ($_.IsPublic) {'public'} else {'private'}
    Write-Host ("  $vis $($_.ReturnType.Name) $($_.Name)($(($_.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ','))")
}
$itc.Fields | ForEach-Object { $vis = if ($_.IsPublic) {'public'} else {'private'}; Write-Host ("  $vis field $($_.FieldType.Name) $($_.Name)") }
$itc.Properties | ForEach-Object { Write-Host ("  prop $($_.PropertyType.Name) $($_.Name)") }

Write-Host "=== RoR2.UI money/currency types ==="
$m.Types | Where-Object { $_.FullName -match 'RoR2\.UI\..*(Money|Currency|Gold)' } | ForEach-Object { Write-Host ("  " + $_.FullName) }

$cd = $m.GetType('RoR2.UI.CurrencyDisplay')
if ($cd) {
    Write-Host "=== CurrencyDisplay fields ==="
    $cd.Fields | ForEach-Object { $vis = if ($_.IsPublic) {'public'} else {'private'}; Write-Host ("  $vis $($_.FieldType.FullName) $($_.Name)") }
    Write-Host "=== CurrencyDisplay methods ==="
    $cd.Methods | ForEach-Object { $vis = if ($_.IsPublic) {'public'} else {'private'}; Write-Host ("  $vis $($_.ReturnType.Name) $($_.Name)($(($_.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ','))") }
}
$asm.Dispose()
