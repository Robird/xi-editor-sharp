pub mod metrics;
pub mod rope;
pub mod samples;
pub mod tree;

pub mod prelude {
    pub use super::metrics::{BaseMetric, DefaultMetricProvider, Metric, Utf16Metric};
    pub use super::rope::Rope;
    pub use super::samples::{sample_rope, sample_rope_via_builder, SampleLeaf, SampleNodeInfo};
    pub use super::tree::{
        Cursor, CursorDescriptor, Interval, Leaf, Node, NodeBody, NodeInfo, NodeVal, PathFrame,
        SharedNode, TreeBuilder,
    };
    #[cfg(feature = "cursor_state")]
    // Available only when the `cursor_state` feature flag is enabled.
    pub use super::tree::CursorState;
}
