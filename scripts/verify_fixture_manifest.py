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


@dataclass
class ManifestChange:
    name: str
    relative_path: str
    old_hash: str
    new_hash: str


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


def _write_manifest(manifest_path: Path, manifest: dict) -> None:
    manifest_path.write_text(
        json.dumps(manifest, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )


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


def _apply_payload_hash_updates(
    manifest: dict,
    results: Iterable[FixtureResult],
    manifest_path: Path,
) -> List[ManifestChange]:
    fixtures = manifest.get("fixtures")
    if not isinstance(fixtures, list):
        raise ValueError("Manifest missing 'fixtures' array.")

    index = {}
    for entry in fixtures:
        if not isinstance(entry, dict):
            continue
        rel_path = entry.get("path")
        if rel_path:
            index[rel_path] = entry

    changes: List[ManifestChange] = []
    for result in results:
        if result.status != "mismatch" or not result.actual_hash:
            continue
        entry = index.get(result.relative_path)
        if not entry:
            raise ValueError(f"Manifest missing entry for {result.relative_path}")
        old_hash = entry.get("payload_hash", "")
        if old_hash == result.actual_hash:
            continue
        entry["payload_hash"] = result.actual_hash
        changes.append(
            ManifestChange(
                name=result.name,
                relative_path=result.relative_path,
                old_hash=old_hash,
                new_hash=result.actual_hash,
            )
        )

    if changes:
        _write_manifest(manifest_path, manifest)

    return changes


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
        help="Rewrite payload_hash values when drift is detected, then re-run verification.",
    )
    args = parser.parse_args(argv)

    repo_root = _repo_root(args.repo_root)
    manifest_path = Path(args.manifest).resolve()

    if not manifest_path.exists():
        print(f"Manifest not found: {manifest_path}", file=sys.stderr)
        return 2

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

    if args.update:
        blockers = [r for r in results if r.status in {"missing", "error"}]
        if blockers:
            print(_format_summary(results))
            print("\n--update aborted: unresolved fixture errors prevent manifest rewrite.")
            for result in blockers:
                detail = result.error or result.status
                print(f"- {result.name}: {detail} ({result.relative_path})")
            return 1

        mismatches = [r for r in results if r.status == "mismatch"]
        if mismatches:
            try:
                changes = _apply_payload_hash_updates(manifest, mismatches, manifest_path)
            except ValueError as exc:
                print(f"Failed to update manifest: {exc}", file=sys.stderr)
                return 2

            if changes:
                print("\nManifest changes:")
                for change in changes:
                    print(
                        f"- {change.name} ({change.relative_path}): {change.old_hash} -> {change.new_hash}"
                    )

            try:
                manifest = _load_manifest(manifest_path)
                results = _verify_fixtures(manifest, repo_root)
            except (json.JSONDecodeError, ValueError) as exc:
                print(f"Post-update verification failed: {exc}", file=sys.stderr)
                return 2
        else:
            print("\n--update: manifest already in sync; no changes written.")

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
