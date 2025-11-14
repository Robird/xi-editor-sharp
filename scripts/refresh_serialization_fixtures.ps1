param(
    [switch]$SkipRust,
    [switch]$SkipCopy,
    [switch]$SkipDotnet,
    [switch]$DryRun,
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'

function Invoke-ExternalCommand {
    param(
        [string]$Message,
        [string]$Command,
        [string[]]$Arguments = @()
    )

    Write-Host "==> $Message"
    if ($Verbose -and $Arguments.Count -gt 0) {
        Write-Host "    $Command $($Arguments -join ' ')"
    } elseif ($Verbose) {
        Write-Host "    $Command"
    }

    if ($DryRun) {
        return
    }

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed: $Command"
    }
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$rustRoot = Join-Path $repoRoot "xi-editor-ph7/rust"
$runAllChecks = Join-Path $rustRoot "run_all_checks"
$rustFixturesDir = Join-Path $rustRoot "rope/tests/regressions/serde"
$csharpFixturesDir = Join-Path $repoRoot "tests/xi.Core.Tests/Fixtures"
$fixtureNames = @("subset_regression.json", "delta_regression.json", "engine_regression.json")

if (-not (Test-Path $rustRoot)) {
    throw "Missing Rust workspace: $rustRoot"
}

if (-not (Test-Path $csharpFixturesDir)) {
    throw "Missing C# fixture directory: $csharpFixturesDir"
}

if (-not $SkipRust) {
    if (-not (Test-Path $runAllChecks)) {
        throw "Missing run_all_checks script: $runAllChecks"
    }

    Push-Location $rustRoot
    try {
        Invoke-ExternalCommand "rust: run_all_checks --filter serde-fixtures" $runAllChecks @("--filter", "serde-fixtures")
        Invoke-ExternalCommand "rust: cargo subset_serialization_regression" "cargo" @("test", "-p", "xi-rope", "--features", "serde", "subset_serialization_regression", "--", "--nocapture")
        Invoke-ExternalCommand "rust: cargo delta_serialization_regression" "cargo" @("test", "-p", "xi-rope", "--features", "serde", "delta_serialization_regression", "--", "--nocapture")
        Invoke-ExternalCommand "rust: cargo engine_serialization_regression" "cargo" @("test", "-p", "xi-rope", "--features", "serde", "engine_serialization_regression", "--", "--nocapture")
    }
    finally {
        Pop-Location
    }
}

if (-not $SkipCopy) {
    foreach ($name in $fixtureNames) {
        $sourcePath = Join-Path $rustFixturesDir $name
        $destinationPath = Join-Path $csharpFixturesDir $name

        Write-Host "==> copy: $name"
        if ($Verbose) {
            Write-Host "    $sourcePath -> $destinationPath"
        }

        if ($DryRun) {
            continue
        }

        if (-not (Test-Path $sourcePath)) {
            throw "Missing source fixture: $sourcePath"
        }

        Copy-Item -LiteralPath $sourcePath -Destination $destinationPath -Force
    }
}

if (-not $SkipDotnet) {
    Invoke-ExternalCommand "dotnet: dotnet test Xi.Editor.sln" "dotnet" @("test", "Xi.Editor.sln")
}

Write-Host "All steps completed."