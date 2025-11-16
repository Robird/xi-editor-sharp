# Architecture Documentation Structure Template
> **Scope**: Define the minimal layout, shared metadata, and cross-link rules for everything under `docs/architecture/`.
> **Owners**: Architecture Mapper (primary) · AI Architect (approver)
> **Adopters**: C# Implementer · Rust Porter · QA Engineer
> **Update Frequency**: Re-evaluate at every milestone kickoff or whenever the goal/progress schema changes.
> **Reviewers**: C# Implementer · Rust Porter · QA Engineer (consensus required before merge)

---

## 1. Document Catalog (Minimal View)
| Doc | Owner | Prefix | Update Trigger |
| --- | --- | --- | --- |
| `port-blueprint.md` | Architecture Mapper | `BP-` | Milestone planning / when goals change |
| `rope-port-mapping.md` | Architecture Mapper | `RPM-` | When file/type mapping or helper parity shifts |
| `type-system-migration-log.md` | Architecture Mapper | `TS-` | Blocker opened/closed |
| `design-divergence-log.md` | Architecture Mapper + QA | `Div-` | Any intentional divergence added/retired |
| `m3-implementation-plan.md` | C# Implementer | `MP-` | Active milestone execution |
| `m3-architect-decision.md` | AI Architect | `Decision-` | Every ratified decision |
| `rope-serialization-fixture-playbook.md` | Rust Porter + QA | `Fixture-` | Each Stage D refresh |

Each file carries the same front-matter (Scope / Owner / Update Frequency / Reviewers / Anchor Prefix / Last Synced Goal Tree). Additional section requirements live in §3.

## 2. Shared Goal & Progress Tree
### 2.1 Single Source
- `docs/architecture/templates/goal-tree.yaml` is the only file humans edit.
- `scripts/goal_tree_sync.py` (planned) projects the YAML into `port-blueprint.md` and `m3-implementation-plan.md`, wrapping each snippet with `<!-- goal-tree:start -->` markers and writing `Generated-At` + SHA metadata.
- Until the sync script ships, edits to Blueprint/Plan must be mirrored manually and called out in PR descriptions.

### 2.2 Core Field Schema (14 required items)
| Field | Description |
| --- | --- |
| `id` | Stable identifier (`G1`, `G2`…). Doubles as anchor suffix `[BP-G1]`, `[MP-G1]`. |
| `title` | Human-friendly goal title. |
| `owner` | Accountable role/person. |
| `status` | Enum/emoji (`✅`, `⚠️`, `🟥`). |
| `due` | ISO date. |
| `rustCommit` | `xi-editor-ph7` commit supplying the latest fixtures/helpers. |
| `dotnetCommit` | `xi-editor-sharp` commit consuming the goal. |
| `rustCliVersion` | `export-serde-fixtures` crate/tag used during the latest Stage D export. |
| `featureGates` | Short list of active gates (e.g., `["serde", "cursor_state"]`). |
| `assetRefs` | Array describing code/tests/fixtures (see §2.3). Includes manifest hash for fixture entries. |
| `schemaVersion` | Fixture schema identifier (e.g., `cursor_descriptors@1.1.0`). |
| `evidence` | Free-form list for non-file proof (CI link, decision log, etc.). |
| `next` | One or two bullet items describing the next concrete action. |
| `riskFlag` | `None/Watch/Mitigate` plus optional link to `[MP-Rx]` entry. |

### 2.3 Optional Extension Fields (max 3 per goal)
| Field | Purpose |
| --- | --- |
| `qaAnchors` | Array of `{ anchor, note }` referencing `[QA-IngestionSmoke]`, `[QA-ChunkBench]`, `[QA-Telemetry]`, `[QA-StageDManual]` with inline thresholds/baselines. |
| `stageDAnchors` | Array of Stage D anchors (`[StageD::ParityAssets]`, `[StageD::FixtureFlow]`, `[StageD::FeatureGates]`). No duplicate CLI text here. |
| `decisionLinks` | Optional list of `[Decision-*]` anchors influencing this goal. |

### 2.4 `assetRefs` Structure
```yaml
assetRefs:
  - kind: code | test | fixture | doc
    path: src/xi.Core/Rope/NodeCursor.cs#L42-L311
    anchor: docs/architecture/rope-port-mapping.md#rpm-cursor
  - kind: fixture
    path: tests/xi.Core.Tests/Fixtures/CursorDescriptors
    manifest: tests/xi.Core.Tests/Fixtures/CursorDescriptors/manifest.json
    manifestHash: 4f2c9b5e
```

### 2.5 Example Snippet
```yaml
- id: G1
  title: NodeCursor Reliability
  owner: C# Implementer
  status: "⚠️"
  due: 2025-11-27
  rustCommit: 3ac9d1e
  dotnetCommit: 0e51fba
  rustCliVersion: 0.5.2
  featureGates: ["serde", "cursor_state"]
  schemaVersion: cursor_descriptors@1.1.0
  assetRefs:
    - kind: code
      path: src/xi.Core/Rope/NodeCursor.cs#L42-L311
    - kind: test
      path: tests/xi.Core.Tests/CursorDescriptorParityTests.cs
    - kind: fixture
      path: tests/xi.Core.Tests/Fixtures/CursorDescriptors
      manifest: tests/xi.Core.Tests/Fixtures/CursorDescriptors/manifest.json
      manifestHash: 4f2c9b5e
  evidence:
    - docs/architecture/rope-port-mapping.md#rpm-cursor
  next:
    - "Implement MoveToNextLeaf/PrevLeaf traversal parity"
  riskFlag: "Watch -> MP-R2"
  qaAnchors:
    - anchor: qa-ingestionsmoke
      note: "scripts/refresh_serialization_fixtures.ps1 -ExportParityFixtures (last pass 2025-11-16)"
    - anchor: qa-telemetry
      note: "Grapheme fallback <=0.5% over 200 ops"
  stageDAnchors:
    - staged-parityassets
```

### 2.6 Sync Workflow
1. Edit `goal-tree.yaml` (do not hand-edit snippets).
2. Run `python scripts/goal_tree_sync.py` (once implemented) to regenerate Blueprint/Plan snippets.
3. Script updates the metadata comment and fails if someone manually tweaked the snippet blocks.
4. `./run_all_checks` verifies the sync plus anchor existence once the tooling lands (until then, reviewers manually confirm).

## 3. Per-Document Obligations (Keep It Short)
- **`port-blueprint.md`**: front-matter, synced goal tree, single “Milestones & Dependencies” section linking `[MP-Tx.y]` + `[TS-*]`, and a concise risk table referencing `[MP-Rx]`.
- **`rope-port-mapping.md`**: front-matter, File/Type matrix `[RPM-Matrix]`, “Parity Assets” paragraph pointing to `[StageD::ParityAssets]`, and “Open Actions” list.
- **`type-system-migration-log.md`**: front-matter, blocker cards `[TS-*]` (Problem→Rust plan→C# plan→Status), and links to `[StageD::RustDependencies]` / `[Decision-*]` where relevant.
- **`design-divergence-log.md`**: front-matter plus divergence table `[Div-*]` (Reason, Mitigation, Exit, QA anchor). Feature gate detail lives at `[StageD::FeatureGates]`.
- **`m3-implementation-plan.md`**: front-matter, synced goal tree, Task table `[MP-Tx.y]`, QA matrix referencing `[QA-*]`, Risk register `[MP-Rx]`, short change log.
- **`m3-architect-decision.md`**: front-matter + Decision log `[Decision-M3-*]` referencing Blueprint/Plan anchors—no restated tables.
- **`rope-serialization-fixture-playbook.md`**: front-matter, `[StageD::StageDChecklist]`, `[StageD::FixtureFlow]`, `[StageD::ParityAssets]` manifest ledger (with hashes), and QA verification anchors `[QA-IngestionSmoke]`/`[QA-StageDManual]`.

## 4. Anchor & Link Rules
1. Anchor prefix = document code (`BP`, `RPM`, `TS`, `Div`, `MP`, `Decision`, `Fixture`, `QA`, `StageD`).
2. Cross-doc references must use Markdown links to anchors (no bare headings).
3. `./run_all_checks` (once updated) lints for: missing anchors referenced in YAML, broken `assetRefs` paths, and absent QA/Stage D anchors listed in `qaAnchors`/`stageDAnchors`.
4. Anchor registries are optional; if needed, maintain a single appendix in Blueprint instead of per-doc duplicates.

## 5. QA & Stage D References
- `qaAnchors` must enumerate the four canonical anchors (`[QA-IngestionSmoke]`, `[QA-ChunkBench]`, `[QA-Telemetry]`, `[QA-StageDManual]`) with short notes (thresholds/baselines). The heavy details (commands, last run, CI links) live in the QA playbook.
- `stageDAnchors` should cover `[StageD::ParityAssets]`, `[StageD::FixtureFlow]`, `[StageD::FeatureGates]`, `[StageD::StageDChecklist]`. Manifest hashes and CLI recipes stay in the Stage D playbook; the goal tree just links to them.
- Fixture entries inside `assetRefs` retain the manifest path/hash so QA can hash-match ingestion smoke results without duplicating Stage D prose.

## 6. Governance & Automation
1. **Change control**: template edits need the four core roles + Architect approval and must be logged in `m3-architect-decision.md`.
2. **Automation (phased)**:
   - Phase 1: Manual sync with reviewer checklist (current state).
   - Phase 2: Land `goal_tree_sync.py` + anchor lint in `./run_all_checks`.
   - Phase 3: Tie fixture manifest validation + benchmark delta checks into CI.
3. **Onboarding**: every role file (`agents/*.md`) references this template; new contributors acknowledge it on first doc PR.
4. **Audit trail**: `AGENTS.md` logs each template revision; individual docs keep a minimal change note (date/author/summary) in their existing change log sections.

**Approval history**: Architecture Mapper · C# Implementer · Rust Porter · QA Engineer (see `agents/*.md` "最近完成" entries dated 2025‑11‑17+ for latest endorsements).