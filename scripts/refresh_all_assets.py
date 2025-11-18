#!/usr/bin/env python3
"""Run the end-to-end documentation refresh pipeline in one command."""
from __future__ import annotations

import argparse
import datetime as _dt
import shlex
import shutil
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Callable, List, Sequence

REPORTS_DIR = Path("tests/xi.Core.Tests/Fixtures/Reports")
FIXTURES_DIR = Path("tests/xi.Core.Tests/Fixtures")
STAGE_D_MANIFEST = FIXTURES_DIR / "fixtures.manifest.json"
STAGE_D_INSPECTOR = REPORTS_DIR / "stage-d-inspector-latest.txt"
CHUNK_BENCH_REPORT = REPORTS_DIR / "chunk-bench-latest.txt"
GRAPHEME_TELEMETRY_REPORT = REPORTS_DIR / "grapheme-telemetry-latest.txt"
GRAPHEME_TELEMETRY_TRX = REPORTS_DIR / "grapheme-telemetry.trx"


@dataclass
class Step:
    name: str
    description: str
    command: Sequence[str] | None = None
    command_factory: Callable[[Path], Sequence[str]] | None = None

    def resolve_command(self, repo_root: Path) -> Sequence[str]:
        if self.command_factory is not None:
            return self.command_factory(repo_root)
        if self.command is not None:
            return self.command
        raise ValueError(f"Step '{self.name}' has no command or factory defined.")

    def render(self, repo_root: Path) -> str:
        command = self.resolve_command(repo_root)
        pretty_cmd = " ".join(shlex.quote(part) for part in command)
        return f"{self.name:>12}: {pretty_cmd}\n             {self.description}"


def _repo_root() -> Path:
    return Path(__file__).resolve().parent.parent


def _powershell_command() -> str | None:
    for candidate in ("pwsh", "powershell", "powershell.exe"):
        resolved = shutil.which(candidate)
        if resolved:
            return resolved
    return None


def _relative_path(path: Path, repo_root: Path) -> str:
    try:
        return str(path.relative_to(repo_root))
    except ValueError:
        return str(path)


def _default_steps(repo_root: Path, warn_if_missing_powershell: bool = True) -> List[Step]:
    dll_path = repo_root / "src/xi.Core/bin/Debug/net9.0/xi.Core.dll"
    dll_rel = _relative_path(dll_path, repo_root)
    steps: List[Step] = [
        Step(
            name="rust-skeletons",
            description="Regenerate Rust skeleton docs via stub_rust_functions",
            command=[sys.executable, "scripts/refresh_skeleton_docs.py"],
        ),
        Step(
            name="dotnet-build",
            description="Build the Xi.Editor solution to produce xi.Core binaries",
            command=["dotnet", "build", "Xi.Editor.sln"],
        ),
    ]

    powershell_exe = _powershell_command()
    if not powershell_exe:
        if warn_if_missing_powershell:
            raise SystemExit(
                "PowerShell (pwsh/powershell) is required for the 'stage-d-fixtures' step. Install pwsh and ensure it is on PATH."
            )
    else:
        steps.append(
            Step(
                name="stage-d-fixtures",
                description="Export Rust fixtures -> Stage D loader/hydrator -> manifest verifier (inspector deferred) + Release QA harnesses",
                command_factory=lambda root, exe=powershell_exe: [
                    exe,
                    "-NoProfile",
                    "-ExecutionPolicy",
                    "Bypass",
                    "-File",
                    str(root / "scripts/refresh_serialization_fixtures.ps1"),
                    "-Verbose",
                    "-SkipStageDLoaderTest:$false",
                    "-SkipStageDHydratorTest:$false",
                    "-SkipManifestVerification:$false",
                    "-SkipStageDInspector:$true",
                ],
            )
        )

    steps.extend(
        [
            Step(
                name="verify-stage-d",
                description="Run canonical JSON hash verification for fixtures.manifest.json",
                command_factory=lambda root: [
                    sys.executable,
                    "scripts/verify_fixture_manifest.py",
                    "--manifest",
                    _relative_path(root / STAGE_D_MANIFEST, root),
                ],
            ),
            Step(
                name="goal-tree",
                description="Sync architecture goal tree snippets from YAML plus manifest/inspector/chunk reports when available",
                command_factory=_goal_tree_command,
            ),
        ]
    )

    steps.extend(
        [
            Step(
                name="ilspy",
                description="Decompile xi.Core.dll into docs/skeleton using ilspycmd",
                command=[
                    "ilspycmd",
                    "-o",
                    "docs/skeleton",
                    dll_rel,
                ],
            ),
            Step(
                name="csharp-skeleton",
                description="Strip method bodies from the ILSpy output via Skeletonizer",
                command=[
                    "dotnet",
                    "run",
                    "--project",
                    "tools/Skeletonizer/Skeletonizer.csproj",
                    "--",
                    "docs/skeleton/xi.Core.decompiled.cs",
                ],
            ),
        ]
    )

    return steps


def _goal_tree_command(repo_root: Path) -> Sequence[str]:
    command: List[str] = [sys.executable, "scripts/goal_tree_sync.py"]
    manifest_path = repo_root / STAGE_D_MANIFEST
    inspector_path = repo_root / STAGE_D_INSPECTOR
    chunk_report_path = repo_root / CHUNK_BENCH_REPORT

    if manifest_path.exists():
        command.extend(["--manifest", _relative_path(manifest_path, repo_root)])
    if inspector_path.exists():
        command.extend(["--inspector", _relative_path(inspector_path, repo_root)])
    if chunk_report_path.exists():
        command.extend(["--chunk-report", _relative_path(chunk_report_path, repo_root)])

    return command


def _parse_csv(value: str | None) -> set[str]:
    if not value:
        return set()
    return {item.strip() for item in value.split(",") if item.strip()}


def _ensure_command_available(command: Sequence[str]) -> None:
    exe = command[0]
    if exe == sys.executable or Path(exe).exists():
        return
    if shutil.which(exe) is None:
        raise FileNotFoundError(
            f"Required command '{exe}' is not available on PATH."
        )


def _run_step(step: Step, repo_root: Path, dry_run: bool) -> None:
    command = step.resolve_command(repo_root)
    print(f"\n==> {step.name} :: {step.description}")
    pretty_cmd = " ".join(shlex.quote(part) for part in command)
    print(f"    $ {pretty_cmd}")
    _ensure_command_available(command)
    if dry_run:
        return
    subprocess.run(command, cwd=repo_root, check=True)


def _run_stage_d_step(step: Step, repo_root: Path, dry_run: bool) -> Path | None:
    command = step.resolve_command(repo_root)
    print(f"\n==> {step.name} :: {step.description}")
    pretty_cmd = " ".join(shlex.quote(part) for part in command)
    print(f"    $ {pretty_cmd}")
    _ensure_command_available(command)

    reports_dir = repo_root / REPORTS_DIR
    reports_dir.mkdir(parents=True, exist_ok=True)
    timestamp = _dt.datetime.now(tz=_dt.timezone.utc).strftime("%Y%m%d-%H%M%S")
    log_path = reports_dir / f"stage-d-refresh-{timestamp}.log"

    if dry_run:
        print("    (dry run) stage-d-fixtures step skipped; no log written.")
        return None

    with open(log_path, "w", encoding="utf-8") as log_file:
        process = subprocess.Popen(
            command,
            cwd=repo_root,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
            bufsize=1,
        )
        try:
            assert process.stdout is not None
            for line in process.stdout:
                print(line, end="")
                log_file.write(line)
            process.wait()
            if process.returncode:
                raise subprocess.CalledProcessError(process.returncode, command)
        finally:
            print(f"    wrote Stage D refresh log to {log_path}")
    return log_path


def _capture_stage_d_inspector(repo_root: Path, dry_run: bool) -> None:
    fixtures_dir = repo_root / FIXTURES_DIR
    if not fixtures_dir.exists():
        raise FileNotFoundError(
            f"Cannot capture Stage D inspector output because fixture directory '{fixtures_dir}' is missing."
        )

    reports_dir = repo_root / REPORTS_DIR
    reports_dir.mkdir(parents=True, exist_ok=True)
    output_file = reports_dir / STAGE_D_INSPECTOR.name

    command = [
        "dotnet",
        "run",
        "--project",
        "tools/StageDDescriptorInspector/StageDDescriptorInspector.csproj",
        "--",
        "--fixtures",
        str(fixtures_dir),
    ]

    print("\n==> stage-d-inspector :: Capture Stage D descriptor inspector output")
    pretty_cmd = " ".join(shlex.quote(part) for part in command)
    print(f"    $ {pretty_cmd}")
    if dry_run:
        print("    (dry run) stage-d-inspector step skipped; no log written.")
        return

    result = subprocess.run(
        command,
        cwd=repo_root,
        check=True,
        capture_output=True,
        text=True,
    )

    if result.stdout:
        print(result.stdout, end="")

    if result.stderr:
        print(result.stderr, file=sys.stderr, end="")

    output_file.write_text(result.stdout, encoding="utf-8")
    print(f"    wrote inspector log to {output_file}")


def _run_stage_d_chunk_bench(repo_root: Path, dry_run: bool) -> None:
    reports_dir = repo_root / REPORTS_DIR
    reports_dir.mkdir(parents=True, exist_ok=True)
    report_path = reports_dir / CHUNK_BENCH_REPORT.name

    command = [
        "dotnet",
        "run",
        "--project",
        "tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj",
        "--configuration",
        "Release",
        "--",
        "--stage-d",
        "--include-alloc-stats",
        "--report",
        str(report_path),
    ]

    print("\n==> stage-d-chunk-bench :: Replay Stage D chunk/line descriptors")
    pretty_cmd = " ".join(shlex.quote(part) for part in command)
    print(f"    $ {pretty_cmd}")
    if dry_run:
        print("    (dry run) stage-d-chunk-bench step skipped; no log written.")
        return

    subprocess.run(command, cwd=repo_root, check=True)
    print(f"    wrote chunk benchmark log to {report_path}")


def _run_grapheme_telemetry_harness(repo_root: Path, dry_run: bool) -> None:
    reports_dir = repo_root / REPORTS_DIR
    reports_dir.mkdir(parents=True, exist_ok=True)
    report_path = reports_dir / GRAPHEME_TELEMETRY_REPORT.name
    fixtures_dir = repo_root / FIXTURES_DIR

    command = [
        "dotnet",
        "run",
        "--project",
        "tests/xi.Core.Tests/Benchmarks/GraphemeTelemetryHarness/GraphemeTelemetryHarness.csproj",
        "--configuration",
        "Release",
        "--",
        "--fixture-dir",
        str(fixtures_dir),
        "--target-ops",
        "12000",
        "--report",
        str(report_path),
    ]

    print("\n==> stage-d-telemetry-harness :: Grapheme telemetry harness (>=12k ops)")
    pretty_cmd = " ".join(shlex.quote(part) for part in command)
    print(f"    $ {pretty_cmd}")
    if dry_run:
        print("    (dry run) stage-d-telemetry-harness step skipped; no log written.")
        return

    subprocess.run(command, cwd=repo_root, check=True)
    print(f"    wrote telemetry harness log to {report_path}")


def _run_stage_d_telemetry(repo_root: Path, dry_run: bool) -> None:
    reports_dir = repo_root / REPORTS_DIR
    reports_dir.mkdir(parents=True, exist_ok=True)
    trx_path = reports_dir / GRAPHEME_TELEMETRY_TRX.name

    command = [
        "dotnet",
        "test",
        "Xi.Editor.sln",
        "--filter",
        "Category=StageDTelemetry",
        "--logger",
        f"trx;LogFileName={trx_path}",
    ]

    print("\n==> stage-d-telemetry :: Grapheme fallback / telemetry smoke")
    pretty_cmd = " ".join(shlex.quote(part) for part in command)
    print(f"    $ {pretty_cmd}")
    if dry_run:
        print("    (dry run) stage-d-telemetry smoke step skipped; no TRX written.")
        return

    subprocess.run(command, cwd=repo_root, check=True)
    print(f"    wrote telemetry TRX to {trx_path}")


def main() -> int:
    parser = argparse.ArgumentParser(
        description="One-click refresh helper for goal tree, skeleton docs, and xi.Core artifacts.",
    )
    parser.add_argument(
        "--only",
        help="Comma-separated subset of steps to run (defaults to all).",
    )
    parser.add_argument(
        "--skip",
        help="Comma-separated list of steps to skip.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Print commands without executing them.",
    )
    parser.add_argument(
        "--list",
        action="store_true",
        help="List available steps and exit.",
    )
    parser.add_argument(
        "--continue-on-error",
        action="store_true",
        help="Attempt remaining steps even if one fails.",
    )
    args = parser.parse_args()

    repo_root = _repo_root()
    steps = _default_steps(repo_root, warn_if_missing_powershell=not args.list)

    if args.list:
        for step in steps:
            print(step.render(repo_root))
        return 0

    only = _parse_csv(args.only)
    skip = _parse_csv(args.skip)

    selected: List[Step] = []
    for step in steps:
        if only and step.name not in only:
            continue
        if step.name in skip:
            continue
        selected.append(step)

    if not selected:
        print("No steps selected. Use --list to inspect options.")
        return 0

    overall_rc = 0
    for step in selected:
        try:
            if step.name == "stage-d-fixtures":
                _run_stage_d_step(step, repo_root=repo_root, dry_run=args.dry_run)
                _capture_stage_d_inspector(repo_root, dry_run=args.dry_run)
                _run_stage_d_chunk_bench(repo_root, dry_run=args.dry_run)
                _run_grapheme_telemetry_harness(repo_root, dry_run=args.dry_run)
                _run_stage_d_telemetry(repo_root, dry_run=args.dry_run)
            else:
                _run_step(step, repo_root=repo_root, dry_run=args.dry_run)
        except subprocess.CalledProcessError as exc:
            overall_rc = exc.returncode or 1
            print(f"Step '{step.name}' failed with exit code {overall_rc}.")
            if not args.continue_on_error:
                break
        except FileNotFoundError as exc:
            overall_rc = 127
            print(exc)
            if not args.continue_on_error:
                break

    return overall_rc


if __name__ == "__main__":
    sys.exit(main())
