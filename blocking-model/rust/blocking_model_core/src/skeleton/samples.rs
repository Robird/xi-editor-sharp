use super::metrics::{BaseMetric, DefaultMetricProvider, Metric, Utf16Metric};
use super::rope::Rope;
use super::tree::{Interval, Leaf, Node, NodeInfo, TreeBuilder, TreeBuilderTracer};

#[derive(Clone, Debug)]
pub struct SampleNodeInfo {
    base_len: usize,
    utf16_units: usize,
}

impl SampleNodeInfo {
    pub fn new(len: usize) -> Self {
        Self {
            base_len: len,
            utf16_units: len,
        }
    }

    pub fn with_utf16(base_len: usize, utf16_units: usize) -> Self {
        Self {
            base_len,
            utf16_units,
        }
    }

    pub fn base_len(&self) -> usize {
        self.base_len
    }

    pub fn utf16_units(&self) -> usize {
        self.utf16_units
    }
}

impl NodeInfo<SampleLeaf> for SampleNodeInfo {
    fn accumulate(&mut self, other: &Self) {
        self.base_len += other.base_len;
        self.utf16_units += other.utf16_units;
    }

    fn compute_info(leaf: &SampleLeaf) -> Self {
        Self::with_utf16(leaf.len(), leaf.utf16_units())
    }

    fn identity() -> Self {
        Self::with_utf16(0, 0)
    }

    fn interval(&self, len: usize) -> Interval {
        Interval::new(0, len.min(self.base_len))
    }
}

impl DefaultMetricProvider<SampleLeaf> for SampleNodeInfo {}

#[derive(Clone, Default, Debug)]
pub struct SampleLeaf {
    text: String,
}

impl SampleLeaf {
    pub fn from_str(text: &str) -> Self {
        Self {
            text: text.to_owned(),
        }
    }

    pub fn utf16_units(&self) -> usize {
        self.text.encode_utf16().count()
    }

    fn grab_slice<'a>(&'a self, iv: Interval) -> &'a str {
        let start = iv.start().min(self.text.len());
        let end = iv.end().min(self.text.len());
        if start >= end {
            ""
        } else {
            self.text.get(start..end).unwrap_or("")
        }
    }

    fn split_point(&self) -> usize {
        let mut idx = self.text.len() / 2;
        while idx < self.text.len() && !self.text.is_char_boundary(idx) {
            idx += 1;
        }
        idx
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
        let slice = other.grab_slice(iv);
        if slice.is_empty() {
            return None;
        }
        self.text.push_str(slice);
        if self.is_ok_child() {
            None
        } else {
            let split_at = self.split_point();
            let remainder = self.text.split_off(split_at);
            Some(SampleLeaf { text: remainder })
        }
    }
}

pub fn sample_rope() -> Rope<SampleNodeInfo, SampleLeaf> {
    let leaf = SampleLeaf::from_str("root sample");
    let node = Node::from_leaf(leaf);
    let utf16_units = SampleNodeInfo::convert_from_default::<Utf16Metric>(&node, node.len());
    let _roundtrip = SampleNodeInfo::convert_to_default::<Utf16Metric>(&node, utf16_units);
    let shared = node.into_shared();
    let _ = BaseMetric::measure(shared.info(), shared.len());
    Rope::from_root(shared)
}

pub fn sample_rope_via_builder() -> Rope<SampleNodeInfo, SampleLeaf> {
    let mut builder = TreeBuilder::with_tracer(TreeBuilderTracer::new());
    builder.push_leaf(SampleLeaf::from_str("left"));
    builder.push_leaf(SampleLeaf::from_str("right"));
    let (root, tracer) = builder.build_with_tracer();
    if let Some(tracer) = tracer {
        assert!(!tracer.events().is_empty());
    }
    let node = Node::from_shared(root.clone_handle());
    let utf16_units = SampleNodeInfo::convert_from_default::<Utf16Metric>(&node, node.len());
    let _roundtrip = SampleNodeInfo::convert_to_default::<Utf16Metric>(&node, utf16_units);
    let _ = BaseMetric::measure(root.info(), root.len());
    Rope::from_root(root)
}
