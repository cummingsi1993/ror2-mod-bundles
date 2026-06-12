# Facts needed for the 4 new items: Roll of Pennies internal name, void pairing API,
# and accessibility of newly-touched members.
Add-Type -Path "C:\Users\cummi\.nuget\packages\mono.cecil\0.11.4\lib\net40\Mono.Cecil.dll"
$managed = "D:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $managed "RoR2.dll"))
$m = $asm.MainModule

Write-Host "=== DLC2Content.Items fields matching gold/money/hurt/penn ==="
foreach ($tn in @('RoR2.DLC2Content/Items','RoR2.RoR2Content/Items','RoR2.DLC1Content/Items')) {
    $t = $m.GetType($tn)
    if ($t) {
        foreach ($f in $t.Fields) {
            if ($f.Name -match 'Gold|Money|Hurt|Penn|Coin') { Write-Host "  $tn :: $($f.Name)" }
        }
    }
}

Write-Host "=== ItemRelationship types ==="
foreach ($t in $m.Types) {
    if ($t.Name -match 'ItemRelationship|ContagiousItem') { Write-Host "  $($t.FullName)" }
}
$irp = $m.GetType('RoR2.ItemRelationshipProvider')
if ($irp) {
    foreach ($f in $irp.Fields) { $vis = if ($f.IsPublic) {'public'} else {'private'}; Write-Host "  IRP field: $vis $($f.FieldType.FullName) $($f.Name)" }
}
$pair = $m.GetType('RoR2.ItemDef/Pair')
if ($pair) { foreach ($f in $pair.Fields) { Write-Host "  Pair field: $($f.FieldType.Name) $($f.Name)" } }

Write-Host "=== Member accessibility checks ==="
$checks = @{
    'RoR2.DeathRewards' = @('goldReward','expReward')
    'RoR2.GlobalEventManager' = @('OnCharacterDeath')
    'RoR2.DamageReport' = @('victimBody','victimMaster','attackerMaster','attackerBody')
    'RoR2.HealthComponent' = @('combinedHealth','fullHealth','health','Networkhealth','barrier','shield')
    'RoR2.DamageInfo' = @('rejected','procCoefficient')
    'RoR2.CharacterBody' = @('armor','maxHealth')
    'RoR2.Run' = @('difficultyCoefficient','time')
    'RoR2.ItemDef' = @('requiredExpansion')
    'RoR2.ExpansionManagement.ExpansionDef' = @()
    'RoR2.ItemCatalog' = @('FindItemIndex','GetItemDef')
    'RoR2.Util' = @('GetBestMasterName')
}
foreach ($typeName in ($checks.Keys | Sort-Object)) {
    $t = $m.GetType($typeName)
    if ($null -eq $t) { Write-Host "MISSING TYPE: $typeName" ; continue }
    foreach ($member in $checks[$typeName]) {
        $found = @()
        foreach ($f in $t.Fields)  { if ($f.Name -eq $member) { $vis = if ($f.IsPublic) {'public'} else {'PRIVATE'}; $found += "field $vis" } }
        foreach ($p in $t.Properties) {
            if ($p.Name -eq $member) {
                $g = if ($p.GetMethod) { if ($p.GetMethod.IsPublic) {'public'} else {'PRIVATE'} } else {'-'}
                $s = if ($p.SetMethod) { if ($p.SetMethod.IsPublic) {'public'} else {'PRIVATE'} } else {'-'}
                $found += "prop get:$g set:$s"
            }
        }
        foreach ($me in $t.Methods) { if ($me.Name -eq $member -and -not $me.IsGetter -and -not $me.IsSetter) { $vis = if ($me.IsPublic) {'public'} else {'PRIVATE'}; $found += "method $vis" } }
        if ($found.Count -eq 0) { Write-Host ("NOT FOUND: {0}.{1}" -f $typeName, $member) }
        else { Write-Host ("{0}.{1} -> {2}" -f $typeName, $member, ($found -join ' | ')) }
    }
}
$asm.Dispose()

Write-Host "=== R2API relationship/content APIs ==="
$r2items = Get-ChildItem "$env:USERPROFILE\.nuget\packages\r2api.contentmanagement" -Recurse -Filter "*.dll" | Select-Object -First 1 -ExpandProperty FullName
Write-Host "inspecting $r2items"
$r2 = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($r2items)
foreach ($t in $r2.MainModule.Types) {
    if ($t.Name -eq 'ContentAddition' -or $t.Name -eq 'R2APIContentManager') {
        foreach ($me in $t.Methods) {
            if ($me.IsPublic -and $me.Name -match 'Add') { Write-Host "  $($t.Name).$($me.Name)($(($me.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ','))" }
        }
    }
}
$r2.Dispose()
