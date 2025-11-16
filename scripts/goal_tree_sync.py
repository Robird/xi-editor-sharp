#!/usr/bin/env python3
"""Synchronize the architecture goal tree Markdown snippets from the YAML source."""
from __future__ import annotations

import argparse
import datetime as _dt
import hashlib
import json
import sys
from pathlib import Path
from typing import Any, Dict, Iterable, List, Sequence

START_MARKER = "<!-- goal-tree:start -->"
END_MARKER = "<!-- goal-tree:end -->"
META_TEMPLATE = "<!-- goal-tree:meta generated-at=\"{generated_at}\" source=\"{source}\" checksum=\"{checksum}\" -->"
DEFAULT_TARGETS = (
    "docs/architecture/port-blueprint.md",
    "docs/architecture/m3-implementation-plan.md",
)


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


def _build_replacement(table: str, source_rel: str, checksum: str) -> str:
    generated_at = _dt.datetime.now(tz=_dt.timezone.utc).isoformat()
    meta = META_TEMPLATE.format(generated_at=generated_at, source=source_rel, checksum=checksum)
    return f"{START_MARKER}\n{meta}\n{table}\n{END_MARKER}"


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
    args = parser.parse_args()

    source_path: Path = args.source
    if not source_path.exists():
        raise SystemExit(f"Goal tree source '{source_path}' does not exist.")

    goals = _load_goals(source_path)
    _validate_goals(goals)
    table = _build_table(goals)

    repo_root = Path(__file__).resolve().parents[1]
    try:
        source_rel = str(source_path.resolve().relative_to(repo_root))
    except ValueError:
        source_rel = str(source_path.resolve())
    checksum = hashlib.sha256(source_path.read_bytes()).hexdigest()
    replacement_block = _build_replacement(table, source_rel, checksum)

    targets = [Path(t) for t in args.targets]
    changed: List[str] = []
    for target in targets:
        if not target.exists():
            raise SystemExit(f"Target file '{target}' does not exist.")
        replaced = _rewrite_target(target, replacement_block, args.check)
        if replaced:
            changed.append(str(target))

    if args.check:
        if changed:
            print("Goal tree snippets are out of date in: " + ", ".join(changed))
            return 1
        print("Goal tree snippets are up to date.")
        return 0

    if changed:
        print("Updated goal tree snippets in: " + ", ".join(changed))
    else:
        print("No changes were necessary.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
