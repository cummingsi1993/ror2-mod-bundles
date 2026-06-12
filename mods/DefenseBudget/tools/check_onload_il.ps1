# Verifies how 'RoR2Application.onLoad +=' compiled in our DLL (field store vs accessor call)
# and what accessors exist at runtime.
Add-Type -Path "C:\Users\cummi\.nuget\packages\mono.cecil\0.11.4\lib\net40\Mono.Cecil.dll"

$mine = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("D:\source\DefenseBudget\mods\DefenseBudget\DefenseBudget\bin\Debug\netstandard2.1\DefenseBudget.dll")
foreach ($t in $mine.MainModule.Types) {
    foreach ($me in $t.Methods) {
        if ($me.HasBody) {
            foreach ($i in $me.Body.Instructions) {
                if ($i.ToString() -match 'onLoad') { Write-Host ("OURS: $($t.Name).$($me.Name): " + $i.ToString()) }
            }
        }
    }
}
$mine.Dispose()

$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("D:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed\RoR2.dll")
$app = $game.MainModule.GetType('RoR2.RoR2Application')
foreach ($me in $app.Methods) {
    if ($me.Name -match 'onLoad') {
        $vis = if ($me.IsPublic) {'public'} else {'PRIVATE'}
        Write-Host ("GAME method: $vis $($me.Name)")
    }
}
foreach ($f in $app.Fields) {
    if ($f.Name -match 'onLoad') {
        $vis = if ($f.IsPublic) {'public'} else {'PRIVATE'}
        Write-Host ("GAME field: $vis $($f.Name)")
    }
}
$game.Dispose()
