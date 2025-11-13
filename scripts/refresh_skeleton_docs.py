import argparse
import subprocess
import sys
from pathlib import Path

SKELETON_TARGETS: tuple[tuple[str, str], ...] = (
    ("xi-editor-ph7/rust/core-lib", "docs/skeleton/core-lib.md"),
    ("xi-editor-ph7/rust/plugin-lib", "docs/skeleton/plugin-lib.md"),
    ("xi-editor-ph7/rust/rope", "docs/skeleton/rope.md"),
    ("xi-editor-ph7/rust/rpc", "docs/skeleton/rpc.md"),
    ("xi-editor-ph7/rust/trace", "docs/skeleton/trace.md"),
    ("xi-editor-ph7/rust/unicode", "docs/skeleton/unicode.md"),
)


def refresh_skeleton_docs(verbose: bool = False, dry_run: bool = False) -> None:
    repo_root = Path(__file__).resolve().parent.parent
    stub_script = repo_root / "scripts" / "stub_rust_functions.py"

    if not stub_script.exists():
        raise FileNotFoundError(f"Cannot locate stub script at {stub_script}")

    for source_rel, output_rel in SKELETON_TARGETS:
        source_path = repo_root / source_rel
        output_path = repo_root / output_rel

        if not source_path.exists():
            if verbose:
                print(f"[skip] Missing source directory: {source_path}")
            continue

        cmd = [
            sys.executable,
            str(stub_script),
            str(source_path),
            "--mode",
            "doc",
            "--output",
            str(output_path),
            "--root",
            str(repo_root),
            "--truncate-output",
        ]

        if verbose:
            print(f"[run] {' '.join(cmd)}")

        if dry_run:
            continue

        subprocess.run(cmd, check=True, cwd=repo_root)


def main() -> None:
    parser = argparse.ArgumentParser(description="Refresh pre-generated Rust skeleton documentation.")
    parser.add_argument("--verbose", action="store_true", help="Print executed commands.")
    parser.add_argument("--dry-run", action="store_true", help="Show commands without running them.")
    args = parser.parse_args()

    refresh_skeleton_docs(verbose=args.verbose, dry_run=args.dry_run)


if __name__ == "__main__":
    main()
