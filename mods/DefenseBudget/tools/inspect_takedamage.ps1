# How does damage actually flow? TakeDamage vs TakeDamageProcess, who calls which,
# and where 'rejected' is honored.
Add-Type -Path "C:\Users\cummi\.nuget\packages\mono.cecil\0.11.4\lib\net40\Mono.Cecil.dll"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("D:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed\RoR2.dll")
$m = $asm.MainModule

$hc = $m.GetType('RoR2.HealthComponent')
Write-Host "=== HealthComponent damage methods ==="
foreach ($me in $hc.Methods) {
    if ($me.Name -match 'TakeDamage') {
        $vis = if ($me.IsPublic) {'public'} else {'private'}
        Write-Host "  $vis $($me.Name)($(($me.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ','))  bodySize=$($me.Body.Instructions.Count)"
    }
}

Write-Host "=== TakeDamage (wrapper) IL ==="
$td = $hc.Methods | Where-Object { $_.Name -eq 'TakeDamage' } | Select-Object -First 1
foreach ($i in $td.Body.Instructions) { Write-Host ("  " + $i.ToString()) }

Write-Host "=== 'rejected' reads in TakeDamageProcess (first 3, with position) ==="
$tdp = $hc.Methods | Where-Object { $_.Name -eq 'TakeDamageProcess' } | Select-Object -First 1
if ($tdp) {
    $count = $tdp.Body.Instructions.Count
    $found = 0
    for ($idx = 0; $idx -lt $count; $idx++) {
        $i = $tdp.Body.Instructions[$idx]
        if ($i.ToString() -match 'rejected') {
            Write-Host ("  instr #$idx of $count : " + $i.ToString())
            # context: next 4 instructions
            for ($j = $idx+1; $j -lt [Math]::Min($idx+5, $count); $j++) { Write-Host ("      -> " + $tdp.Body.Instructions[$j].ToString()) }
            $found++
            if ($found -ge 3) { break }
        }
    }
    if ($found -eq 0) { Write-Host "  (no 'rejected' reference found in TakeDamageProcess)" }
}

Write-Host "=== Callers of TakeDamageProcess across RoR2.dll (first 15) ==="
$callers = @()
foreach ($t in $m.Types) {
    foreach ($me in $t.Methods) {
        if ($me.HasBody) {
            foreach ($i in $me.Body.Instructions) {
                if ($i.Operand -and $i.Operand.ToString() -match 'TakeDamageProcess') {
                    $callers += "$($t.Name).$($me.Name)"
                }
            }
        }
    }
}
$callers | Sort-Object -Unique | Select-Object -First 15 | ForEach-Object { Write-Host "  $_" }
$asm.Dispose()
