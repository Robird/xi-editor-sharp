#[derive(Clone, Copy, Debug, Eq, PartialEq, Ord, PartialOrd)]
pub enum BlockingPointKind {
    CursorLifecycle,
    GenericNode,
    MetricInteroperation,
    ChunkEnumeration,
    GraphemeNavigation,
    BreaksTree,
}

#[derive(Clone, Debug, PartialEq, Eq)]
pub struct BlockingPointSpec {
    pub kind: BlockingPointKind,
    pub objective: &'static str,
    pub rust_focus: &'static str,
    pub csharp_focus: &'static str,
}

const REGISTRY: &[BlockingPointSpec] = &[
    BlockingPointSpec {
        kind: BlockingPointKind::CursorLifecycle,
        objective: "验证拥有型 cursor + descriptor 的失效策略",
        rust_focus: "CursorDescriptor + CursorState stub crate",
        csharp_focus: "NodeCursor mini tree + version ticket",
    },
    BlockingPointSpec {
        kind: BlockingPointKind::GenericNode,
        objective: "复刻 Node<TInfo, TLeaf> 的 builder 约束",
        rust_focus: "Node<TInfo, L> builder skeleton",
        csharp_focus: "TypeAliases + TreeBuilder.Generic minimal sample",
    },
    BlockingPointSpec {
        kind: BlockingPointKind::MetricInteroperation,
        objective: "度量转换 shim 的 API 契约",
        rust_focus: "convert_* helpers without Rope",
        csharp_focus: "IMetricAdapter contract + sample",
    },
    BlockingPointSpec {
        kind: BlockingPointKind::ChunkEnumeration,
        objective: "Chunk/Line 迭代器 owned descriptor 接口",
        rust_focus: "ChunkDescriptor facade",
        csharp_focus: "RopeChunkEnumerator diagnostics stub",
    },
    BlockingPointSpec {
        kind: BlockingPointKind::GraphemeNavigation,
        objective: "字素降级策略 + telemetry",
        rust_focus: "GraphemeCursor trace exporter",
        csharp_focus: "Degraded navigator + counters",
    },
    BlockingPointSpec {
        kind: BlockingPointKind::BreaksTree,
        objective: "Breaks tree + search shim",
        rust_focus: "BreaksDescriptor exporter",
        csharp_focus: "BreaksModel aggregator",
    },
];

pub struct BlockingPointRegistry;

impl BlockingPointRegistry {
    pub fn all() -> &'static [BlockingPointSpec] {
        REGISTRY
    }

    pub fn find(kind: BlockingPointKind) -> Option<&'static BlockingPointSpec> {
        REGISTRY.iter().find(|spec| spec.kind == kind)
    }
}
