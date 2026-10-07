param([string]$Module)

# One line per project: module | layer/kind | path. Uses git so node_modules/obj are never walked.
Set-Location (git rev-parse --show-toplevel)
git ls-files '*.csproj' | ForEach-Object {
    $path = $_
    $name = [IO.Path]::GetFileNameWithoutExtension($path)
    $top = $path.Split('/')[0]
    $mod = if ($path -match '^(src|tst)/([^/]+)/') { $Matches[2] } else { '-' }
    $kind = if ($name -match "^TimeForCode\.$mod\.(.+)$") { $Matches[1] } elseif ($name -eq "TimeForCode.$mod") { 'Core' } else { $name }
    [pscustomobject]@{ Module = $mod; Kind = "$top/$kind"; Path = $path }
} | Where-Object { -not $Module -or $_.Module -eq $Module } |
    Sort-Object Module, Kind |
    ForEach-Object { '{0} | {1} | {2}' -f $_.Module, $_.Kind, $_.Path }
