Add-Type -Path "C:\Users\cummi\.nuget\packages\mono.cecil\0.11.4\lib\net40\Mono.Cecil.dll"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("D:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed\RoR2.dll")
$inv = $asm.MainModule.GetType('RoR2.Inventory')
foreach ($name in @('GetItemCount','GetItemCountEffective','GetItemCountPermanent')) {
    foreach ($me in $inv.Methods) {
        if ($me.Name -eq $name) {
            Write-Host "=== $name($(($me.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ',')) ==="
            foreach ($i in $me.Body.Instructions) { Write-Host ("  " + $i.ToString()) }
        }
    }
}
$asm.Dispose()
