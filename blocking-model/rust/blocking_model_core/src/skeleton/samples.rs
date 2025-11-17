use super::metrics::BaseMetric;
use super::rope::Rope;
use super::tree::{Leaf, Node, NodeInfo, SharedNode};

#[derive(Clone, Debug)]
pub struct SampleNodeInfo {
    edit_version: u64,
}

impl SampleNodeInfo {
    pub fn new(edit_version: u64) -> Self {
        Self { edit_version }
    }
}

impl NodeInfo for SampleNodeInfo {
    type Metric = BaseMetric;
    type Summary = (u64, &'static str);

    fn describe_cursor(&self) -> Self::Summary {
        (self.edit_version, "root")
    }
}

#[derive(Clone)]
pub struct SampleLeaf {
    text: &'static str,
}

impl SampleLeaf {
    pub const fn new(text: &'static str) -> Self {
        Self { text }
    }
}

impl Leaf for SampleLeaf {
    type Metric = BaseMetric;

    fn text(&self) -> &str {
        self.text
    }
}

pub fn sample_rope() -> Rope<SampleNodeInfo, SampleLeaf> {
    let node: Node<SampleNodeInfo, SampleLeaf> = Node::new(SampleNodeInfo::new(0));
    Rope::from_root(SharedNode::new(node))
}
