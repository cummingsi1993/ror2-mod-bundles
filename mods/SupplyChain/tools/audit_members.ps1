# Audits runtime accessibility of every RoR2 member SupplyChain touches (the GameLibs
# reference assemblies are publicized; private members compile but crash at runtime).
Add-Type -Path "C:\Users\cummi\.nuget\packages\mono.cecil\0.11.4\lib\net40\Mono.Cecil.dll"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("D:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed\RoR2.dll")
$m = $asm.MainModule

$usage = @{
    'RoR2.ChestBehavior' = @('dropPickup','dropTransform','dropForwardVelocityStrength','dropUpVelocityStrength','RollItem','ItemDrop')
    'RoR2.Run' = @('availableTier1DropList','availableTier2DropList','availableTier3DropList','availableVoidTier1DropList','availableVoidTier2DropList','availableVoidTier3DropList','availableVoidBossDropList','treasureRng','instance')
    'RoR2.Stage' = @('onServerStageBegin')
    'RoR2.Inventory' = @('GetItemCount','GetItemCountEffective','GetItemCountPermanent','GiveItem')
    'RoR2.CharacterMaster' = @('OnInventoryChanged','GiveMoney','inventory','playerCharacterMasterController')
    'RoR2.ItemCatalog' = @('itemCount','GetItemDef')
    'RoR2.PickupCatalog' = @('GetPickupDef','FindPickupIndex')
    'RoR2.PickupDef' = @('itemIndex','itemTier','internalName')
    'RoR2.PickupDropletController' = @('CreatePickupDroplet')
    'RoR2.Xoroshiro128Plus' = @('RangeInt','nextNormalizedFloat')
    'RoR2.PurchaseInteraction' = @('costType','OnInteractionBegin')
    'RoR2.RunArtifactManager' = @('instance','IsArtifactEnabled')
    'RoR2.ItemDef' = @('hidden','itemIndex','tier','requiredExpansion')
    'RoR2.ItemRelationshipProvider' = @('relationshipType','relationships')
    'RoR2.Util' = @('GetBestMasterName')
    'RoR2.Chat' = @('SendBroadcastChat')
    'RoR2.LocalUserManager' = @('GetFirstLocalUser')
    'RoR2.RoR2Application' = @('onLoad')
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
