use super::metrics::{BaseMetric, DefaultMetricProvider, Metric};
use super::rope::Rope;
use super::tree::{Interval, Leaf, Node, NodeInfo, SharedNode, TreeBuilder};

#[derive(Clone, Debug)]
pub struct SampleNodeInfo {
    len: usize,
}

impl SampleNodeInfo {
    pub fn new(len: usize) -> Self {
        Self { len }
    }
}

impl NodeInfo<SampleLeaf> for SampleNodeInfo {
    fn accumulate(&mut self, other: &Self) {
        self.len += other.len;
    }

    fn compute_info(leaf: &SampleLeaf) -> Self {
        Self { len: leaf.len() }
    }

    fn identity() -> Self {
        Self { len: 0 }
    }

    fn interval(&self, len: usize) -> Interval {
        Interval::new(0, len.min(self.len))
    }
}

impl DefaultMetricProvider<SampleLeaf> for SampleNodeInfo {
    fn convert_from_default<M: Metric<Self, SampleLeaf>>(_node: &Node<Self, SampleLeaf>, offset: usize) -> usize {
        let _ = M::can_fragment();
        offset
    }

    fn convert_to_default<M: Metric<Self, SampleLeaf>>(_node: &Node<Self, SampleLeaf>, offset: usize) -> usize {
        let _ = M::can_fragment();
        offset
    }
}

#[derive(Clone, Default, Debug)]
pub struct SampleLeaf {
    text: String,
}

impl SampleLeaf {
    pub fn from_str(text: &str) -> Self {
        Self { text: text.to_owned() }
    }
}

impl Leaf for SampleLeaf {
    fn len(&self) -> usize {
        self.text.len()
    }

    fn is_ok_child(&self) -> bool {
        self.len() <= 32
    }

    fn push_maybe_split(&mut self, other: &Self, iv: Interval) -> Option<Self> {
        let _ = iv;
        self.text.push_str(&other.text);
        None
    }
}

pub fn sample_rope() -> Rope<SampleNodeInfo, SampleLeaf> {
    let leaf = SampleLeaf::from_str("root");
    let shared: SharedNode<SampleNodeInfo, SampleLeaf> = Node::from_leaf(leaf).into_shared();
    let metric_units = BaseMetric::measure(shared.info(), shared.len());
    let _ = metric_units;
    Rope::from_root(shared)
}

pub fn sample_rope_via_builder() -> Rope<SampleNodeInfo, SampleLeaf> {
    let mut builder = TreeBuilder::new();
    builder.push_leaf(SampleLeaf::from_str("left"));
    builder.push_leaf(SampleLeaf::from_str("right"));
    let root = builder.build();
    let _ = BaseMetric::measure(root.info(), root.len());
    Rope::from_root(root)
}
