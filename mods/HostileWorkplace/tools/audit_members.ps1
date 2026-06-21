# Audits runtime accessibility of every RoR2 member HostileWorkplace touches (the GameLibs
# reference assemblies are publicized; private members compile but crash at runtime).
# Members only HOOKED (On.*) or SUBSCRIBED (add_*) are safe even when the underlying field
# is private — only direct field/property/method ACCESS must be runtime-public.
Add-Type -Path "C:\Users\cummi\.nuget\packages\mono.cecil\0.11.4\lib\net40\Mono.Cecil.dll"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("D:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed\RoR2.dll")
$m = $asm.MainModule

$usage = @{
    # window timing (events — subscribe-only, private backing is fine)
    'RoR2.TeleporterInteraction' = @('onTeleporterBeginChargingGlobal','onTeleporterChargedGlobal')
    'RoR2.Stage' = @('onServerStageBegin')
    # window telegraph + state
    'RoR2.PlayerCharacterMasterController' = @('instances','master')
    'RoR2.CharacterMaster' = @('GetBody','playerCharacterMasterController','inventory')
    'RoR2.CharacterBody' = @('HasBuff','AddTimedBuff','teamComponent','master')
    'RoR2.TeamComponent' = @('teamIndex')
    'RoR2.Chat' = @('SendBroadcastChat')
    'RoR2.Run' = @('instance')
    'RoR2.RunArtifactManager' = @('instance','IsArtifactEnabled')
    # slice 2 (friendly fire). friendlyFireMode is set directly; the FF damage scale is
    # forced via reflection (intentional, not audited here). Simulate is HOOKED (private ok).
    'RoR2.FriendlyFireManager' = @('friendlyFireMode')
    'RoR2.HealthComponent' = @('TakeDamage','combinedHealth','fullCombinedHealth','body','alive')
    'RoR2.DamageInfo' = @('attacker','damage','rejected')
    'RoR2.CombatDirector' = @('Simulate')
    # slice 3 (theft) — onCharacterDeathGlobal is subscribe-only (private backing ok)
    'RoR2.Inventory' = @('GiveItem','RemoveItem','GetItemCount','itemAcquisitionOrder')
    'RoR2.DamageReport' = @('attackerMaster','victimMaster','victimBody')
    'RoR2.GlobalEventManager' = @('onCharacterDeathGlobal')
    'RoR2.ItemCatalog' = @('GetItemDef')
    'RoR2.ItemDef' = @('tier','hidden','canRemove')
    # slice 4 (equipment). PerformEquipmentAction is HOOKED (private ok).
    'RoR2.EquipmentDef' = @('cooldown','canDrop','enigmaCompatible','canBeRandomlyTriggered','appearsInSinglePlayer','appearsInMultiPlayer','pickupIconSprite','pickupModelPrefab')
    'RoR2.EquipmentSlot' = @('PerformEquipmentAction')
}

foreach ($typeName in ($usage.Keys | Sort-Object)) {
    $t = $m.GetType($typeName)
    if ($null -eq $t) { Write-Host "MISSING TYPE: $typeName" -ForegroundColor Red; continue }
    foreach ($member in $usage[$typeName]) {
        $found = @()
        foreach ($f in $t.Fields)  { if ($f.Name -eq $member) { $vis = if ($f.IsPublic) {'public'} else {'PRIVATE'}; $found += "field $vis" } }
        foreach ($p in $t.Properties) {
            if ($p.Name -eq $member) {
                $g = if ($p.GetMethod) { if ($p.GetMethod.IsPublic) {'public'} else {'PRIVATE'} } else {'-'}
                $s = if ($p.SetMethod) { if ($p.SetMethod.IsPublic) {'public'} else {'PRIVATE'} } else {'-'}
                $found += "prop get:$g set:$s"
            }
        }
        foreach ($me in $t.Methods) {
            if (($me.Name -eq $member -or $me.Name -eq "add_$member") -and -not $me.IsGetter -and -not $me.IsSetter) {
                $vis = if ($me.IsPublic) {'public'} else {'PRIVATE'}
                $found += "method($($me.Name)) $vis"
            }
        }
        if ($found.Count -eq 0) { Write-Host ("NOT FOUND: {0}.{1}" -f $typeName, $member) -ForegroundColor Yellow }
        else {
            $desc = $found -join ' | '
            if ($desc -match 'PRIVATE') { Write-Host ("DANGER:    {0}.{1}  ->  {2}" -f $typeName, $member, $desc) -ForegroundColor Red }
            else { Write-Host ("ok:        {0}.{1}  ->  {2}" -f $typeName, $member, $desc) }
        }
    }
}
$asm.Dispose()
