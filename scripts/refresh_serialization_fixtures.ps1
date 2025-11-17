param(
    [switch]$SkipRust,
    [switch]$SkipCopy,
    [switch]$SkipDotnet,
    [Parameter(HelpMessage = "Skip the Stage D loader smoke (StageDDescriptorLoaderTests) step.")]
    [switch]$SkipStageDLoaderTest,
    [switch]$DryRun,
    [switch]$Verbose,
    [switch]$ExportTreeTrace,
    [bool]$ExportParityFixtures = $true,
    [string]$ManifestPath
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
$runAllChecksBase = Join-Path $rustRoot "run_all_checks"
$runAllChecksWindows = "$runAllChecksBase.ps1"
$runAllChecks = if ($IsWindows -and (Test-Path $runAllChecksWindows)) {
    $runAllChecksWindows
}
else {
    $runAllChecksBase
}
$csharpFixturesDir = Join-Path $repoRoot "tests/xi.Core.Tests/Fixtures"
$treeTraceDir = Join-Path $csharpFixturesDir "tree_builder_slice"
$cursorFixturesDir = Join-Path $csharpFixturesDir "cursor_descriptors"
$chunkFixturesDir = Join-Path $csharpFixturesDir "chunk_descriptors"
$graphemeFixturesDir = Join-Path $csharpFixturesDir "grapheme_descriptors"
if (-not $ManifestPath) {
    $ManifestPath = Join-Path $csharpFixturesDir "fixtures.manifest.json"
}

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
        $runAllChecksArgs = @("--filter", "serde-fixtures")
        if ($runAllChecks -like "*.ps1") {
            $runAllChecksArgs = @("-Filter", "serde-fixtures")
        }

        Invoke-ExternalCommand "rust: run_all_checks --filter serde-fixtures" $runAllChecks $runAllChecksArgs
        Invoke-ExternalCommand "rust: cargo subset_serialization_regression" "cargo" @("test", "-p", "xi-rope", "--features", "serde", "subset_serialization_regression", "--", "--nocapture")
        Invoke-ExternalCommand "rust: cargo delta_serialization_regression" "cargo" @("test", "-p", "xi-rope", "--features", "serde", "delta_serialization_regression", "--", "--nocapture")
        Invoke-ExternalCommand "rust: cargo engine_serialization_regression" "cargo" @("test", "-p", "xi-rope", "--features", "serde", "engine_serialization_regression", "--", "--nocapture")
    }
    finally {
        Pop-Location
    }
}

if (-not $SkipCopy) {
    Push-Location $rustRoot
    try {
        $arguments = @(
            "run",
            "-p",
            "xi-rope",
            "--features",
            "serde",
            "--bin",
            "export-serde-fixtures",
            "--",
            "--dir",
            $csharpFixturesDir,
            "--emit-manifest",
            $ManifestPath
        )

        if ($ExportParityFixtures) {
            $arguments += @(
                "--cursor-descriptors",
                $cursorFixturesDir,
                "--chunk-descriptors",
                $chunkFixturesDir,
                "--grapheme-descriptors",
                $graphemeFixturesDir
            )
        }
        $manifestNote = " (manifest -> $ManifestPath)"
        Invoke-ExternalCommand "rust: export-serde-fixtures$manifestNote" "cargo" $arguments

        if ($ExportTreeTrace) {
            $treeArgs = @(
                "run",
                "-p",
                "xi-rope",
                "--features",
                "serde,tree_builder_slice_trace",
                "--bin",
                "export-serde-fixtures",
                "--",
                "--tree-builder-trace",
                $treeTraceDir
            )
            Invoke-ExternalCommand "rust: export-serde-fixtures (tree builder trace)" "cargo" $treeArgs
        }
    }
    finally {
        Pop-Location
    }
}

if (-not $SkipDotnet) {
    Invoke-ExternalCommand "dotnet: dotnet test Xi.Editor.sln" "dotnet" @("test", "Xi.Editor.sln")
}

if ($SkipStageDLoaderTest) {
    Write-Host "Skipping Stage D loader smoke (StageDDescriptorLoaderTests)."
}
else {
    Invoke-ExternalCommand "dotnet: Stage D loader smoke (StageDDescriptorLoaderTests)" "dotnet" @("test", "Xi.Editor.sln", "--filter", "StageDDescriptorLoaderTests")
}

Write-Host "All steps completed."