#!/usr/bin/env python3
"""Synchronize the architecture goal tree Markdown snippets from the YAML source."""
from __future__ import annotations

import argparse
import datetime as _dt
import hashlib
import json
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional, Sequence

START_MARKER = "<!-- goal-tree:start -->"
END_MARKER = "<!-- goal-tree:end -->"
DEFAULT_TARGETS = (
    "docs/architecture/port-blueprint.md",
    "docs/architecture/m3-implementation-plan.md",
)
DEFAULT_REPORTS_DIR = Path("tests/xi.Core.Tests/Fixtures/Reports")


def _load_goals(source: Path) -> List[Dict[str, Any]]:
    text = source.read_text(encoding="utf-8")
    # Try strict JSON first so we do not require external dependencies.
    try:
        data = json.loads(text)
    except json.JSONDecodeError:
        try:
            import yaml  # type: ignore
        except ModuleNotFoundError as exc:  # pragma: no cover - defensive path
            raise SystemExit(
                "PyYAML is required to parse goal-tree.yaml. Install it via 'pip install pyyaml'."
            ) from exc
        data = yaml.safe_load(text)
    if not isinstance(data, list):
        raise SystemExit("goal-tree.yaml must contain a list of goal entries.")
    for idx, entry in enumerate(data):
        if not isinstance(entry, dict):
            raise SystemExit(f"Goal entry #{idx + 1} is not an object: {entry!r}")
    return data  # type: ignore[return-value]


def _validate_goals(goals: Sequence[Dict[str, Any]]) -> None:
    required_fields = {
        "id",
        "title",
        "status",
        "due",
        "owner",
        "rustCommit",
        "dotnetCommit",
        "rustCliVersion",
        "featureGates",
        "assetRefs",
        "schemaVersion",
        "evidence",
        "next",
        "riskFlag",
    }
    seen_ids = set()
    for goal in goals:
        missing = required_fields - goal.keys()
        if missing:
            raise SystemExit(f"Goal {goal.get('id', '<unknown>')} missing fields: {', '.join(sorted(missing))}")
        gid = goal["id"]
        if gid in seen_ids:
            raise SystemExit(f"Duplicate goal id detected: {gid}")
        seen_ids.add(gid)


def _format_owner(owner_value: Any) -> str:
    if isinstance(owner_value, str):
        return owner_value
    if isinstance(owner_value, Iterable):
        owners = [str(item).strip() for item in owner_value if str(item).strip()]
        if owners:
            return " · ".join(owners)
    return "—"


def _format_next(next_value: Any) -> str:
    if isinstance(next_value, str):
        return next_value
    if isinstance(next_value, Iterable):
        steps = [str(item).strip() for item in next_value if str(item).strip()]
        if steps:
            return "<br>".join(steps)
    return "—"


def _format_qa(goal: Dict[str, Any]) -> str:
    entries: List[str] = []
    for qa in goal.get("qaAnchors", []) or []:
        if isinstance(qa, dict):
            anchor = qa.get("anchor", "").strip()
            note = qa.get("note")
            if anchor:
                if note:
                    entries.append(f"{anchor} ({note})")
                else:
                    entries.append(anchor)
        elif qa:
            entries.append(str(qa))
    stage_d = goal.get("stageDAnchors", []) or []
    for anchor in stage_d:
        if isinstance(anchor, dict):
            text = anchor.get("anchor", "").strip()
            note = anchor.get("note")
            if text:
                entries.append(f"{text} ({note})" if note else text)
        elif anchor:
            entries.append(str(anchor))
    return " · ".join(entries) if entries else "—"


def _build_table(goals: Sequence[Dict[str, Any]]) -> str:
    headers = ["ID", "Title", "Status", "Due", "Owner", "Next", "QA / Stage D"]
    rows: List[List[str]] = []
    for goal in goals:
        rows.append(
            [
                str(goal["id"]),
                str(goal["title"]),
                str(goal["status"]),
                str(goal["due"]),
                _format_owner(goal.get("owner")),
                _format_next(goal.get("next")),
                _format_qa(goal),
            ]
        )
    lines = [
        "| " + " | ".join(headers) + " |",
        "| " + " | ".join(["---"] * len(headers)) + " |",
    ]
    for row in rows:
        lines.append("| " + " | ".join(row) + " |")
    return "\n".join(lines)


def _rewrite_target(target: Path, replacement: str, check_only: bool) -> bool:
    text = target.read_text(encoding="utf-8")
    start = text.find(START_MARKER)
    end = text.find(END_MARKER)
    if start == -1 or end == -1:
        raise SystemExit(f"File '{target}' is missing goal tree markers.")
    end += len(END_MARKER)
    new_text = text[:start] + replacement + text[end:]
    if new_text == text:
        return False
    if not check_only:
        target.write_text(new_text, encoding="utf-8")
    return True


def _build_replacement(table: str, meta_comment: str) -> str:
    return f"{START_MARKER}\n{meta_comment}\n{table}\n{END_MARKER}"


@dataclass
class ManifestDetails:
    rust_commit: Optional[str] = None
    payload_hash: Optional[str] = None


class ReportLogger:
    def __init__(self, log_path: Optional[Path]) -> None:
        self._log_path = log_path
        self._buffer: List[str] = []

    def info(self, message: str) -> None:
        print(message)
        if self._log_path:
            self._buffer.append(message)

    def flush(self) -> None:
        if not self._log_path or not self._buffer:
            return
        self._log_path.parent.mkdir(parents=True, exist_ok=True)
        self._log_path.write_text("\n".join(self._buffer) + "\n", encoding="utf-8")
        print(f"[goal-tree-sync] log written to {self._log_path}")


def _resolve_optional_path(path_value: Optional[Path], repo_root: Path) -> Optional[Path]:
    if path_value is None:
        return None
    candidate = path_value if path_value.is_absolute() else (repo_root / path_value)
    if not candidate.exists():
        raise SystemExit(f"Path '{candidate}' does not exist.")
    return candidate


def _relative_path(path_value: Optional[Path], repo_root: Path) -> Optional[str]:
    if path_value is None:
        return None
    try:
        return str(path_value.resolve().relative_to(repo_root))
    except ValueError:
        return str(path_value.resolve())


def _load_manifest_details(manifest_path: Path, fixture_name: Optional[str]) -> ManifestDetails:
    data = json.loads(manifest_path.read_text(encoding="utf-8"))
    metadata = data.get("metadata") or {}
    rust_commit = metadata.get("rust_commit") or metadata.get("rustCommit")

    payload_hash: Optional[str] = None
    fixtures = data.get("fixtures") or []
    if fixture_name and fixtures:
        for fixture in fixtures:
            name = str(fixture.get("name", "")).strip()
            if name and name.lower() == fixture_name.lower():
                payload_hash = fixture.get("payload_hash") or fixture.get("payloadHash")
                break
        else:
            raise SystemExit(
                f"Fixture '{fixture_name}' was not found in manifest '{manifest_path}'."
            )
    if payload_hash is None and fixtures:
        payload_hash = fixtures[0].get("payload_hash") or fixtures[0].get("payloadHash")

    return ManifestDetails(rust_commit=rust_commit, payload_hash=payload_hash)


def _build_meta_comment(
    source_rel: str,
    checksum: str,
    manifest_details: ManifestDetails,
    inspector_rel: Optional[str],
    chunk_report_rel: Optional[str],
) -> str:
    generated_at = _dt.datetime.now(tz=_dt.timezone.utc).isoformat()
    attributes: Dict[str, str] = {
        "generated-at": generated_at,
        "source": source_rel,
        "checksum": checksum,
    }
    if manifest_details.rust_commit:
        attributes["rust-commit"] = manifest_details.rust_commit
    if manifest_details.payload_hash:
        attributes["payload-hash"] = manifest_details.payload_hash
    if inspector_rel:
        attributes["inspector-report"] = inspector_rel
    if chunk_report_rel:
        attributes["chunk-report"] = chunk_report_rel
    serialized = " ".join(f"{key}=\"{value}\"" for key, value in attributes.items())
    return f"<!-- goal-tree:meta {serialized} -->"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--source",
        default="docs/architecture/templates/goal-tree.yaml",
        type=Path,
        help="Path to goal-tree YAML source (default: %(default)s)",
    )
    parser.add_argument(
        "--targets",
        nargs="*",
        default=list(DEFAULT_TARGETS),
        help="Markdown files whose goal-tree snippet should be replaced",
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="Do not write files; exit with 1 if any file would change",
    )
    parser.add_argument(
        "--manifest",
        type=Path,
        help="Path to fixtures manifest JSON whose rust commit and payload hash should be captured",
    )
    parser.add_argument(
        "--manifest-fixture",
        default="chunk_descriptors.json",
        help="Fixture entry name used to pull payload_hash from the manifest (default: %(default)s)",
    )
    parser.add_argument(
        "--inspector",
        type=Path,
        help="Path to the latest Stage D inspector report (written to the meta comment when provided)",
    )
    parser.add_argument(
        "--chunk-report",
        type=Path,
        help="Path to the Stage D chunk benchmark report (written to the meta comment when provided)",
    )
    parser.add_argument(
        "--reports-dir",
        type=Path,
        default=DEFAULT_REPORTS_DIR,
        help="Directory where tee logs should be written (default: %(default)s)",
    )
    parser.add_argument(
        "--skip-report-log",
        action="store_true",
        help="Skip writing the tee log to the reports directory",
    )
    args = parser.parse_args()

    repo_root = Path(__file__).resolve().parents[1]
    source_path: Path = args.source if args.source.is_absolute() else (repo_root / args.source)
    if not source_path.exists():
        raise SystemExit(f"Goal tree source '{source_path}' does not exist.")

    reports_dir = args.reports_dir if args.reports_dir.is_absolute() else (repo_root / args.reports_dir)
    log_path: Optional[Path] = None
    if not args.skip_report_log:
        reports_dir.mkdir(parents=True, exist_ok=True)
        timestamp = _dt.datetime.now(tz=_dt.timezone.utc).strftime("%Y%m%d-%H%M%S")
        log_path = reports_dir / f"goal-tree-sync-{timestamp}.log"
    reporter = ReportLogger(log_path)

    exit_code = 0
    try:
        goals = _load_goals(source_path)
        _validate_goals(goals)
        table = _build_table(goals)

        try:
            source_rel = str(source_path.resolve().relative_to(repo_root))
        except ValueError:
            source_rel = str(source_path.resolve())
        checksum = hashlib.sha256(source_path.read_bytes()).hexdigest()

        manifest_details = ManifestDetails()
        manifest_path = _resolve_optional_path(args.manifest, repo_root)
        if manifest_path is not None:
            fixture_name = args.manifest_fixture.strip() or None
            manifest_details = _load_manifest_details(manifest_path, fixture_name)

        inspector_path = _resolve_optional_path(args.inspector, repo_root)
        chunk_report_path = _resolve_optional_path(args.chunk_report, repo_root)
        inspector_rel = _relative_path(inspector_path, repo_root)
        chunk_report_rel = _relative_path(chunk_report_path, repo_root)

        meta_comment = _build_meta_comment(
            source_rel=source_rel,
            checksum=checksum,
            manifest_details=manifest_details,
            inspector_rel=inspector_rel,
            chunk_report_rel=chunk_report_rel,
        )
        replacement_block = _build_replacement(table, meta_comment)

        targets: List[Path] = []
        for raw_target in args.targets:
            target_path = Path(raw_target)
            if not target_path.is_absolute():
                target_path = repo_root / target_path
            targets.append(target_path)

        changed: List[str] = []
        for target in targets:
            if not target.exists():
                raise SystemExit(f"Target file '{target}' does not exist.")
            replaced = _rewrite_target(target, replacement_block, args.check)
            if replaced:
                changed.append(str(target))

        if args.check:
            if changed:
                reporter.info("Goal tree snippets are out of date in: " + ", ".join(changed))
                exit_code = 1
            else:
                reporter.info("Goal tree snippets are up to date.")
            return exit_code

        if changed:
            reporter.info("Updated goal tree snippets in: " + ", ".join(changed))
        else:
            reporter.info("No changes were necessary.")
        return exit_code
    finally:
        reporter.flush()


if __name__ == "__main__":
    raise SystemExit(main())
