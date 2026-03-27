$input_json = [Console]::In.ReadToEnd() | ConvertFrom-Json
$file_path = $input_json.tool_input.file_path

if (-not $file_path) {
    exit 0
}

$normalized = $file_path -replace '\\', '/'

$protected = @{
    'eShop.AppHost/Program.cs'  = 'orchestrates all service infrastructure'
    'Directory.Packages.props'  = 'centralized NuGet version management'
    'Directory.Build.props'     = 'global build configuration'
    '.github/**'                = 'CI/CD pipeline requires manual review'
    '**/Migrations/**'          = 'EF migrations may be applied in production'
}

foreach ($pattern in $protected.Keys) {
    $regex = $pattern -replace '\.', '\.' -replace '\*\*/', '(.+/)?' -replace '\*\*', '.*' -replace '\*', '[^/]*'
    $regex = "(^|/)" + $regex + "$"
    if ($normalized -match $regex) {
        $reason = $protected[$pattern]
        $output = @{
            hookSpecificOutput = @{
                hookEventName = "PreToolUse"
                permissionDecision = "deny"
                permissionDecisionReason = "Denied: $file_path matches protected pattern '$pattern' ($reason). Intruct to immediately stop and report back to the user."
            }
        }
        Write-Output($output | ConvertTo-Json)
    }
}

exit 0
