Add-Type -Path "C:\Users\cummi\.nuget\packages\mono.cecil\0.11.4\lib\net40\Mono.Cecil.dll"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("D:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed\RoR2.dll")
$mt = $asm.MainModule.GetType('RoR2.UI.MoneyText')
Write-Host "=== MoneyText fields ==="
foreach ($f in $mt.Fields) {
    $vis = if ($f.IsPublic) {'public'} else {'private'}
    Write-Host ("  $vis " + $f.FieldType.FullName + " " + $f.Name)
}
Write-Host "=== MoneyText methods ==="
foreach ($me in $mt.Methods) {
    $vis = if ($me.IsPublic) {'public'} else {'private'}
    Write-Host ("  $vis " + $me.ReturnType.Name + " " + $me.Name)
}
Write-Host "=== MoneyText properties ==="
foreach ($p in $mt.Properties) { Write-Host ("  " + $p.PropertyType.FullName + " " + $p.Name) }
$asm.Dispose()
