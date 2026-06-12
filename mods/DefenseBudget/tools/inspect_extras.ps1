Add-Type -Path "C:\Users\cummi\.nuget\packages\mono.cecil\0.11.4\lib\net40\Mono.Cecil.dll"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("D:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed\RoR2.dll")
$m = $asm.MainModule

$lu = $m.GetType('RoR2.LocalUser')
foreach ($p in $lu.Properties) {
    if ($p.Name -match 'cached') {
        $g = if ($p.GetMethod -and $p.GetMethod.IsPublic) {'public'} else {'PRIVATE/none'}
        Write-Host ("LocalUser.$($p.Name) get: $g")
    }
}
$ra = $m.GetType('RoR2.ResourceAvailability')
foreach ($me in $ra.Methods) {
    $vis = if ($me.IsPublic) {'public'} else {'private'}
    Write-Host ("ResourceAvailability.$($me.Name): $vis")
}
$asm.Dispose()
