# Decodes the Addressables catalog key data and prints keys matching a pattern.
param([string]$Pattern = 'Pickup')

$t = [System.IO.File]::ReadAllText("D:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\StreamingAssets\aa\catalog.json")
$m = [regex]::Match($t, '"m_KeyDataString":"([^"]+)"')
if (-not $m.Success) { throw "no m_KeyDataString found" }
$bytes = [Convert]::FromBase64String($m.Groups[1].Value)

# Key data layout: int32 count, then entries of [byte type][payload].
# Type 0 = ASCII string: int32 length + bytes. Type 1 = UTF16 string. Others skipped heuristically:
# we just scan for readable ASCII runs >= 8 chars and filter.
$sb = New-Object System.Text.StringBuilder
$results = New-Object System.Collections.Generic.List[string]
$current = New-Object System.Text.StringBuilder
foreach ($b in $bytes) {
    if ($b -ge 32 -and $b -le 126) {
        [void]$current.Append([char]$b)
    } else {
        if ($current.Length -ge 8) { $results.Add($current.ToString()) }
        [void]$current.Clear()
    }
}
if ($current.Length -ge 8) { $results.Add($current.ToString()) }

$results | Where-Object { $_ -match $Pattern } | Sort-Object -Unique
