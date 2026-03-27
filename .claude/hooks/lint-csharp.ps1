$hookData = [Console]::In.ReadToEnd() | ConvertFrom-Json
$filePath = $hookData.tool_input.file_path

if (-not $filePath -or $filePath -notmatch '\.cs$') {
    exit 0
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

if (-not [System.IO.Path]::IsPathRooted($filePath)) {
    $filePath = Join-Path $repoRoot $filePath
}

if (-not (Test-Path -LiteralPath $filePath)) {
    exit 0
}

$searchDir = Split-Path -Parent $filePath
$projectPath = $null

while ($searchDir -and $searchDir.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    $projectPath = Get-ChildItem -Path $searchDir -File -Filter '*.csproj' -ErrorAction SilentlyContinue |
        Select-Object -First 1 -ExpandProperty FullName

    if ($projectPath) {
        break
    }

    $parentDir = Split-Path -Parent $searchDir
    if ($parentDir -eq $searchDir) {
        break
    }

    $searchDir = $parentDir
}

if (-not $projectPath) {
    exit 0
}

Push-Location $repoRoot
try {
    & dotnet build $projectPath --no-restore --nologo -v:q /m \
        -p:UseSharedCompilation=true \
        -p:RunAnalyzersDuringBuild=true \
        -p:EnforceCodeStyleInBuild=true 2>$null | Out-Null
}
finally {
    Pop-Location
}

exit 0