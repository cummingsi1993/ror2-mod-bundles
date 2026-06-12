# Audits runtime accessibility of every RoR2 member AuditDepartment touches (the GameLibs
# reference assemblies are publicized; private members compile but crash at runtime).
Add-Type -Path "C:\Users\cummi\.nuget\packages\mono.cecil\0.11.4\lib\net40\Mono.Cecil.dll"

function Audit-Usage($assemblyPath, $usage) {
    $asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($assemblyPath)
    $m = $asm.MainModule
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
}

$managed = "D:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed"

# Members accessed directly (compiled member references). Members reached via
# reflection (CombatDirector.currentMonsterCardCost) or MMHOOK (Simulate,
# AttemptSpawnOnTarget, TakeDamage) are intentionally absent: private is fine there.
Audit-Usage "$managed\RoR2.dll" @{
    'RoR2.CombatDirector' = @('monsterCredit','teamIndex','shouldSpawnOneWave')
    'RoR2.DirectorCard' = @('spawnCard','cost','IsAvailable')
    'RoR2.DirectorCore' = @('instance','TrySpawnObject')
    'RoR2.DirectorSpawnRequest' = @('teamIndexOverride','ignoreTeamMemberLimit','summonerBodyObject')
    'RoR2.DirectorPlacementRule' = @('placementMode','minDistance','maxDistance','spawnOnTarget')
    'RoR2.ClassicStageInfo' = @('instance','monsterSelection')
    'RoR2.CharacterBody' = @('onBodyStartGlobal','teamComponent','corePosition','coreTransform','AddTimedBuff','HasBuff','master','healthComponent')
    'RoR2.CharacterMaster' = @('GiveMoney','inventory','GetBody','playerCharacterMasterController')
    'RoR2.Inventory' = @('GetItemCount')
    'RoR2.PlayerCharacterMasterController' = @('instances','master')
    'RoR2.GlobalEventManager' = @('onCharacterDeathGlobal')
    'RoR2.DamageReport' = @('victimBody','attackerMaster')
    'RoR2.DamageInfo' = @('damage')
    'RoR2.DeathRewards' = @('spawnValue')
    'RoR2.HealthComponent' = @('body','alive')
    'RoR2.Stage' = @('onServerStageBegin')
    'RoR2.Run' = @('instance','time','difficultyCoefficient')
    'RoR2.RoR2Content/Buffs' = @('Slow60','Weak')
    'RoR2.MasterSuicideOnTimer' = @('lifeTimer')
    'RoR2.Util' = @('GetBestMasterName')
    'RoR2.Chat' = @('SendBroadcastChat')
    'RoR2.RunArtifactManager' = @('instance','IsArtifactEnabled')
    'RoR2.ItemDef' = @('itemIndex','requiredExpansion')
    'RoR2.TeamComponent' = @('teamIndex')
    'RoR2.PickupCatalog' = @('FindPickupIndex')
    'RoR2.PickupDropletController' = @('CreatePickupDroplet')
    'RoR2.LocalUserManager' = @('GetFirstLocalUser')
    'RoR2.RoR2Application' = @('onLoad')
    'WeightedSelection`1' = @('Evaluate')  # global namespace, lives in RoR2.dll
}

Audit-Usage "$managed\HGCSharpUtils.dll" @{
    'Xoroshiro128Plus' = @('nextNormalizedFloat')
}
