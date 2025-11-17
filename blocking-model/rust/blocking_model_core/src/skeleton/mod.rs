pub mod helpers;
pub mod metrics;
pub mod rope;
pub mod samples;
pub mod tree;

pub mod prelude {
    pub use super::helpers;
    pub use super::helpers::string_leaf;
    pub use super::metrics::{BaseMetric, DefaultMetricProvider, Metric, Utf16Metric};
    pub use super::rope::Rope;
    pub use super::samples::{
        SampleLeaf, SampleNodeInfo, sample_deep_tree_rope, sample_rope, sample_rope_via_builder,
    };
    #[cfg(feature = "cursor_state")]
    // Available only when the `cursor_state` feature flag is enabled.
    pub use super::tree::CursorState;
    pub use super::tree::{
        Cursor, CursorDescriptor, Interval, Leaf, Node, NodeBody, NodeInfo, NodeVal, PathFrame,
        SharedNode, TreeBuilder, TreeBuilderEvent, TreeBuilderTrace, TreeBuilderTracer,
    };
}
