# Audits the accessibility of every RoR2 member the mod touches, against the REAL game
# assembly (not the publicized reference assemblies), to catch FieldAccessException traps.
Add-Type -Path "C:\Users\cummi\.nuget\packages\mono.cecil\0.11.4\lib\net40\Mono.Cecil.dll"

$managed = "D:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $managed "RoR2.dll"))
$module = $asm.MainModule

# type name -> list of member names used by DefenseBudgetPlugin.cs
$usage = @{
    'RoR2.ItemDef' = @('nameToken','pickupToken','descriptionToken','loreToken','canRemove','hidden','tags','pickupIconSprite','pickupModelPrefab','itemIndex','tier','deprecatedTier')
    'RoR2.CharacterBody' = @('master','corePosition','healthComponent','inputBank','transform','HasBuff','AddBuff','RemoveBuff','armor','teamComponent','AddTimedBuff')
    'RoR2.RoR2Content/Buffs' = @('Immune')
    'RoR2.RunArtifactManager' = @('instance','IsArtifactEnabled')
    'RoR2.ArtifactDef' = @('cachedName','nameToken','descriptionToken','smallIconSelectedSprite','smallIconDeselectedSprite')
    'RoR2.CharacterMaster' = @('money','inventory','playerCharacterMasterController','GetBody','GiveMoney')
    'RoR2.PurchaseInteraction' = @('cost','costType','Networkcost','CanBeAffordedByInteractor','OnInteractionBegin','Awake')
    'RoR2.HealthComponent' = @('fullCombinedHealth','alive','TakeDamage')
    'RoR2.Inventory' = @('GetItemCount')
    'RoR2.Run' = @('instance','GetDifficultyScaledCost','onRunStartGlobal')
    'RoR2.Chat' = @('SendBroadcastChat')
    'RoR2.Chat/SimpleChatMessage' = @('baseToken')
    'RoR2.PickupDropletController' = @('CreatePickupDroplet')
    'RoR2.PickupCatalog' = @('FindPickupIndex')
    'RoR2.LocalUserManager' = @('GetFirstLocalUser')
    'RoR2.LocalUser' = @('cachedBody')
    'RoR2.PlayerCharacterMasterController' = @('instances','networkUser')
    'RoR2.NetworkUser' = @('userName')
    'RoR2.BuffDef' = @('iconSprite','buffColor','canStack','isDebuff')
    'RoR2.DamageInfo' = @('damage','position','attacker','inflictor','crit','damageColorIndex','damageType')
    'RoR2.Interactor' = @()
    'RoR2.RoR2Application' = @('onLoad')
    'RoR2.ItemTierCatalog' = @('GetItemTierDef')
    'RoR2.UI.MoneyText' = @('targetText','targetValue')
    'RoR2.DeathRewards' = @('goldReward')
    'RoR2.GlobalEventManager' = @('OnCharacterDeath')
    'RoR2.DamageReport' = @('victimBody')
    'RoR2.ItemRelationshipProvider' = @('relationshipType','relationships')
    'RoR2.Util' = @('GetBestMasterName')
}

foreach ($typeName in ($usage.Keys | Sort-Object)) {
    $type = $module.GetType($typeName)
    if ($null -eq $type) { Write-Host "MISSING TYPE: $typeName" -ForegroundColor Red; continue }
    foreach ($member in $usage[$typeName]) {
        $found = @()
        foreach ($f in $type.Fields)  { if ($f.Name -eq $member) { $vis = if ($f.IsPublic) {'public'} else {'PRIVATE'}; $found += "field $vis" } }
        foreach ($p in $type.Properties) {
            if ($p.Name -eq $member) {
                $g = if ($p.GetMethod) { if ($p.GetMethod.IsPublic) {'public'} else {'PRIVATE'} } else {'-'}
                $s = if ($p.SetMethod) { if ($p.SetMethod.IsPublic) {'public'} else {'PRIVATE'} } else {'-'}
                $found += "prop get:$g set:$s"
            }
        }
        foreach ($m in $type.Methods) {
            if ($m.Name -eq $member -and -not $m.IsGetter -and -not $m.IsSetter) {
                $vis = if ($m.IsPublic) {'public'} else {'PRIVATE'}
                $found += "method $vis"
            }
        }
        foreach ($e in $type.Events) { if ($e.Name -eq $member) { $found += "event (check add/remove)" } }
        if ($found.Count -eq 0) { Write-Host ("NOT FOUND: {0}.{1}" -f $typeName, $member) -ForegroundColor Yellow }
        else {
            $desc = $found -join ' | '
            if ($desc -match 'PRIVATE') { Write-Host ("DANGER:    {0}.{1}  ->  {2}" -f $typeName, $member, $desc) -ForegroundColor Red }
            else { Write-Host ("ok:        {0}.{1}  ->  {2}" -f $typeName, $member, $desc) }
        }
    }
}

# DamageColorIndex.Item enum value exists?
$dci = $module.GetType('RoR2.DamageColorIndex')
if ($dci) {
    $hasItem = ($dci.Fields | Where-Object { $_.Name -eq 'Item' }).Count -gt 0
    Write-Host ("enum RoR2.DamageColorIndex.Item exists: {0}" -f $hasItem)
}
$asm.Dispose()
