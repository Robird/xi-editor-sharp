#!/usr/bin/env python3
"""Verify fixture payload hashes recorded in fixtures.manifest.json.

This script canonicalizes each JSON payload (sort_keys=True, ensure_ascii=False)
so the computed SHA256 values match the exporter manifest. Non-zero exit codes
indicate drift or missing assets.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, List, Optional


@dataclass
class FixtureResult:
    name: str
    relative_path: str
    resolved_path: Path
    expected_hash: str
    actual_hash: Optional[str]
    status: str
    error: Optional[str] = None

    def as_row(self) -> str:
        actual = self.actual_hash or "-"
        return f"{self.name:<28} {self.status:<9} {self.expected_hash} {actual}"


def _repo_root(default_path: Optional[str]) -> Path:
    if default_path:
        return Path(default_path).resolve()
    return Path(__file__).resolve().parent.parent


def _canonical_bytes(json_path: Path) -> bytes:
    with json_path.open("r", encoding="utf-8") as handle:
        payload = json.load(handle)
    canonical_text = json.dumps(
        payload,
        sort_keys=True,
        ensure_ascii=False,
        separators=(",", ":"),
    )
    return canonical_text.encode("utf-8")


def _sha256_hex(data: bytes) -> str:
    digest = hashlib.sha256()
    digest.update(data)
    return digest.hexdigest()


def _load_manifest(manifest_path: Path) -> dict:
    with manifest_path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


# TODO: extend verification to cover feature_gates, rust_commit, cli_rev integrity.

def _verify_fixtures(manifest: dict, repo_root: Path) -> List[FixtureResult]:
    fixtures = manifest.get("fixtures")
    if not isinstance(fixtures, list):
        raise ValueError("Manifest missing 'fixtures' array.")

    results: List[FixtureResult] = []
    for entry in fixtures:
        name = entry.get("name") or entry.get("path") or "<unknown>"
        relative_path = entry.get("path")
        expected_hash = entry.get("payload_hash")
        if not relative_path or not expected_hash:
            results.append(
                FixtureResult(
                    name=name,
                    relative_path=relative_path or "",
                    resolved_path=repo_root,
                    expected_hash=expected_hash or "",
                    actual_hash=None,
                    status="error",
                    error="Manifest entry missing path or payload_hash.",
                )
            )
            continue

        resolved_path = (repo_root / relative_path).resolve()
        if not resolved_path.exists():
            results.append(
                FixtureResult(
                    name=name,
                    relative_path=relative_path,
                    resolved_path=resolved_path,
                    expected_hash=expected_hash,
                    actual_hash=None,
                    status="missing",
                    error=f"Fixture not found at {resolved_path}",
                )
            )
            continue

        try:
            canonical = _canonical_bytes(resolved_path)
        except json.JSONDecodeError as exc:
            results.append(
                FixtureResult(
                    name=name,
                    relative_path=relative_path,
                    resolved_path=resolved_path,
                    expected_hash=expected_hash,
                    actual_hash=None,
                    status="error",
                    error=f"Invalid JSON: {exc}",
                )
            )
            continue

        actual_hash = _sha256_hex(canonical)
        status = "match" if actual_hash == expected_hash else "mismatch"
        error = None if status == "match" else "Hash drift"
        results.append(
            FixtureResult(
                name=name,
                relative_path=relative_path,
                resolved_path=resolved_path,
                expected_hash=expected_hash,
                actual_hash=actual_hash,
                status=status,
                error=error,
            )
        )

    return results


def _format_summary(results: Iterable[FixtureResult]) -> str:
    lines = [
        "Fixture                       Status    Expected Hash                                                   Actual Hash",
        "---------------------------- --------- --------------------------------------------------------------- ---------------------------------------------------------------",
    ]
    lines.extend(result.as_row() for result in results)
    return "\n".join(lines)


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(description="Verify fixture payload hashes against the manifest.")
    parser.add_argument(
        "--manifest",
        default=str(Path(__file__).resolve().parent.parent / "tests/xi.Core.Tests/Fixtures/fixtures.manifest.json"),
        help="Path to fixtures.manifest.json",
    )
    parser.add_argument(
        "--repo-root",
        default=str(Path(__file__).resolve().parent.parent),
        help="Repository root used to resolve relative fixture paths.",
    )
    parser.add_argument(
        "--update",
        action="store_true",
        help="Future flag to rewrite manifest payload hashes when drift is expected.",
    )
    args = parser.parse_args(argv)

    repo_root = _repo_root(args.repo_root)
    manifest_path = Path(args.manifest).resolve()

    if not manifest_path.exists():
        print(f"Manifest not found: {manifest_path}", file=sys.stderr)
        return 2

    if args.update:
        # TODO: implement update mode that rewrites payload_hash entries.
        print("--update is not implemented yet.", file=sys.stderr)
        return 64

    try:
        manifest = _load_manifest(manifest_path)
    except json.JSONDecodeError as exc:
        print(f"Failed to parse manifest: {exc}", file=sys.stderr)
        return 2

    try:
        results = _verify_fixtures(manifest, repo_root)
    except ValueError as exc:
        print(f"Manifest error: {exc}", file=sys.stderr)
        return 2

    print(_format_summary(results))

    mismatches = [r for r in results if r.status in {"mismatch", "missing", "error"}]
    if mismatches:
        print("\nDrift detected:")
        for result in mismatches:
            print(f"- {result.name}: {result.error or result.status} ({result.relative_path})")
        return 1

    print(f"\nAll {len(results)} fixtures match the manifest hashes.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
