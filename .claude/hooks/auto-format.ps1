$hookData = [Console]::In.ReadToEnd() | ConvertFrom-Json
$filePath = $hookData.tool_input.file_path

if ($filePath -match '\.cs$') {
    $repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    $workspacePath = Get-ChildItem -Path $repoRoot -File -Filter '*.slnf' -ErrorAction SilentlyContinue |
        Select-Object -First 1 -ExpandProperty FullName
    if (-not $workspacePath) {
        $workspacePath = Get-ChildItem -Path $repoRoot -File -Include '*.slnx','*.sln' -ErrorAction SilentlyContinue |
            Select-Object -First 1 -ExpandProperty FullName
    }

    if ($workspacePath) {
        $relativePath = $filePath
        if ($filePath.StartsWith($repoRoot)) {
            $relativePath = $filePath.Substring($repoRoot.Length).TrimStart('\', '/')
        }
        Push-Location $repoRoot
        try {
            & dotnet format $workspacePath --include $relativePath --verbosity quiet 2>$null | Out-Null
        }
        finally {
            Pop-Location
        }
    }
}

exit 0
