#!/usr/bin/env python3
"""Run the end-to-end documentation refresh pipeline in one command."""
from __future__ import annotations

import argparse
import shlex
import shutil
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, List, Sequence


@dataclass
class Step:
    name: str
    description: str
    command: Sequence[str]

    def render(self) -> str:
        return f"{self.name:>12}: {' '.join(shlex.quote(part) for part in self.command)}\n             {self.description}"


def _repo_root() -> Path:
    return Path(__file__).resolve().parent.parent


def _powershell_command() -> str | None:
    for candidate in ("pwsh", "powershell", "powershell.exe"):
        resolved = shutil.which(candidate)
        if resolved:
            return resolved
    return None


def _default_steps(repo_root: Path, warn_if_missing_powershell: bool = True) -> List[Step]:
    dll_path = repo_root / "src/xi.Core/bin/Debug/net9.0/xi.Core.dll"
    steps: List[Step] = [
        Step(
            name="goal-tree",
            description="Sync architecture goal tree snippets from YAML",
            command=[sys.executable, "scripts/goal_tree_sync.py"],
        ),
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
    if powershell_exe:
        steps.append(
            Step(
                name="stage-d-fixtures",
                description="Export Rust fixtures + run Stage D loader smoke",
                command=[
                    powershell_exe,
                    "-NoProfile",
                    "-ExecutionPolicy",
                    "Bypass",
                    "-File",
                    str(repo_root / "scripts/refresh_serialization_fixtures.ps1"),
                    "-Verbose",
                    "-SkipStageDLoaderTest:$false",
                ],
            )
        )
    elif warn_if_missing_powershell:
        print(
            "Skipping 'stage-d-fixtures' step because PowerShell (pwsh/powershell) is not available on PATH.",
            file=sys.stderr,
        )

    steps.append(
        Step(
            name="verify-stage-d",
            description="Run canonical JSON hash verification for fixtures.manifest.json",
            command=[
                sys.executable,
                "scripts/verify_fixture_manifest.py",
            ],
        )
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
                    str(dll_path),
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
    print(f"\n==> {step.name} :: {step.description}")
    pretty_cmd = " ".join(shlex.quote(part) for part in step.command)
    print(f"    $ {pretty_cmd}")
    _ensure_command_available(step.command)
    if dry_run:
        return
    subprocess.run(step.command, cwd=repo_root, check=True)


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
    name_to_step = {step.name: step for step in steps}

    if args.list:
        for step in steps:
            print(step.render())
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
