[CmdletBinding()]
param(
    [switch]$SkipRust,
    [switch]$SkipCopy,
    [switch]$SkipDotnet,
    [Parameter(HelpMessage = "Skip the Stage D loader smoke (StageDDescriptorLoaderTests) step.")]
    [switch]$SkipStageDLoaderTest,
    [Parameter(HelpMessage = "Skip the Stage D hydrator smoke (StageDDescriptorHydratorTests) step.")]
    [switch]$SkipStageDHydratorTest,
    [Parameter(HelpMessage = "Skip manifest verification (python scripts/verify_fixture_manifest.py). Default: run verification.")]
    [switch]$SkipManifestVerification,
    [switch]$DryRun,
    [switch]$ExportTreeTrace,
    [bool]$ExportParityFixtures = $true,
    [string]$ManifestPath
)

$ErrorActionPreference = 'Stop'
$script:VerboseOutputRequested = $PSBoundParameters.ContainsKey('Verbose')

function Invoke-ExternalCommand {
    param(
        [string]$Message,
        [string]$Command,
        [string[]]$Arguments = @()
    )

    Write-Host "==> $Message"
    if ($script:VerboseOutputRequested -and $Arguments.Count -gt 0) {
        Write-Host "    $Command $($Arguments -join ' ')"
    } elseif ($script:VerboseOutputRequested) {
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

function Get-PythonInterpreter {
    $candidates = @("python3", "python", "py")
    foreach ($candidate in $candidates) {
        $commandInfo = Get-Command $candidate -ErrorAction SilentlyContinue
        if ($null -ne $commandInfo) {
            if ($commandInfo.Source) {
                return $commandInfo.Source
            }

            if ($commandInfo.Definition) {
                return $commandInfo.Definition
            }

            return $candidate
        }
    }

    throw "Unable to locate a Python interpreter (checked python3, python, py). Install Python 3 and re-run the fixture refresh script."
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$rustRoot = Join-Path $repoRoot "xi-editor-ph7/rust"
$ropeCrateRoot = Join-Path $rustRoot "rope"
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
$breaksFixturesDir = Join-Path $csharpFixturesDir "breaks_descriptors"
$diffFixturesDir = Join-Path $csharpFixturesDir "diff_regions"
$searchFixturesDir = Join-Path $csharpFixturesDir "search_spans"
if (-not $ManifestPath) {
    $ManifestPath = Join-Path $csharpFixturesDir "fixtures.manifest.json"
}
$manifestVerifierScript = Join-Path $repoRoot "scripts/verify_fixture_manifest.py"

if (-not (Test-Path $rustRoot)) {
    throw "Missing Rust workspace: $rustRoot"
}

if (-not (Test-Path $ropeCrateRoot)) {
    throw "Missing xi-rope crate: $ropeCrateRoot"
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
    Push-Location $ropeCrateRoot
    try {
        $featureList = "serde"
        if ($ExportTreeTrace) {
            $featureList = "serde,tree_builder_slice_trace"
        }

        $arguments = @(
            "run",
            "-p",
            "xi-rope",
            "--features",
            $featureList,
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
                $graphemeFixturesDir,
                "--breaks-descriptors",
                $breaksFixturesDir,
                "--diff-regions",
                $diffFixturesDir,
                "--search-spans",
                $searchFixturesDir
            )
        }
        if ($ExportTreeTrace) {
            $arguments += @("--tree-builder-trace", $treeTraceDir)
        }

        $manifestNote = " (manifest -> $ManifestPath"
        if ($ExportTreeTrace) {
            $manifestNote += ", tree trace -> $treeTraceDir"
        }
        $manifestNote += ")"
        Invoke-ExternalCommand "rust: export-serde-fixtures$manifestNote" "cargo" $arguments
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

if ($SkipStageDHydratorTest) {
    Write-Host "Skipping Stage D hydrator smoke (StageDDescriptorHydratorTests)."
}
else {
    Invoke-ExternalCommand "dotnet: Stage D hydrator smoke (StageDDescriptorHydratorTests)" "dotnet" @("test", "Xi.Editor.sln", "--filter", "StageDDescriptorHydratorTests")
}

if ($SkipManifestVerification) {
    Write-Host "Skipping manifest verification (verify_fixture_manifest.py)."
}
elseif ($DryRun) {
    Write-Host "Skipping manifest verification because -DryRun was supplied."
}
else {
    if (-not (Test-Path $manifestVerifierScript)) {
        throw "Missing manifest verifier: $manifestVerifierScript"
    }

    $pythonInterpreter = Get-PythonInterpreter
    $verifyArgs = @($manifestVerifierScript, "--manifest", $ManifestPath)
    Invoke-ExternalCommand "python: verify fixture manifest" $pythonInterpreter $verifyArgs
}

Write-Host "All steps completed."
