# Game internals needed by the SupplyChain bundle: chest drop flow, drop lists,
# stage-begin events, and the Inventory.GetItemCount surface.
Add-Type -Path "C:\Users\cummi\.nuget\packages\mono.cecil\0.11.4\lib\net40\Mono.Cecil.dll"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("D:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed\RoR2.dll")
$m = $asm.MainModule

function Dump-Type([string]$name, [string]$memberFilter) {
    $t = $m.GetType($name)
    if (-not $t) { Write-Host "MISSING TYPE: $name"; return }
    Write-Host "=== $name ==="
    foreach ($f in $t.Fields) {
        if ($f.Name -match $memberFilter) {
            $vis = if ($f.IsPublic) {'public'} else {'private'}
            Write-Host "  field $vis $($f.FieldType.Name) $($f.Name)"
        }
    }
    foreach ($p in $t.Properties) {
        if ($p.Name -match $memberFilter) {
            $g = if ($p.GetMethod -and $p.GetMethod.IsPublic) {'public'} else {'priv/-'}
            Write-Host "  prop get:$g $($p.PropertyType.Name) $($p.Name)"
        }
    }
    foreach ($me in $t.Methods) {
        if ($me.Name -match $memberFilter -and -not $me.IsGetter -and -not $me.IsSetter) {
            $vis = if ($me.IsPublic) {'public'} else {'private'}
            Write-Host "  method $vis $($me.ReturnType.Name) $($me.Name)($(($me.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ','))"
        }
    }
}

Dump-Type 'RoR2.ChestBehavior' 'Drop|drop|Open|Roll|Pick|tier'
Dump-Type 'RoR2.Run' 'availab.*DropList|treasureRng|stageRng'
Dump-Type 'RoR2.Stage' 'onServer|onStage|begin|Begin'
Dump-Type 'RoR2.Inventory' 'GetItemCount|itemStacks|onInventoryChanged'
Dump-Type 'RoR2.CharacterMaster' 'onInventoryChanged|OnInventoryChanged'
Dump-Type 'RoR2.PickupDropTable' 'GenerateDrop'
Dump-Type 'RoR2.ItemDef' 'tier'
Dump-Type 'RoR2.PickupDef' 'itemIndex|itemTier'
Dump-Type 'RoR2.PickupCatalog' 'GetPickupDef'
$asm.Dispose()
