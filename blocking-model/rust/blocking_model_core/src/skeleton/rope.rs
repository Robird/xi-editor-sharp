use super::helpers::string_leaf;
use super::metrics::{DefaultMetricProvider, Metric, Utf16Metric};
use super::tree::{
    Cursor, CursorDescriptor, Interval, Leaf, Node, NodeBody, NodeInfo, NodeVal, SharedNode,
    TreeBuilder, TreeBuilderTrace, TreeBuilderTracer,
};

/// Rope exposes the same surface area while delegating to skeleton stubs.
pub struct Rope<N: NodeInfo<L>, L: Leaf> {
    root: SharedNode<N, L>,
}

impl<N: NodeInfo<L>, L: Leaf> Rope<N, L> {
    pub fn new() -> Self {
        let mut builder: TreeBuilder<N, L> = TreeBuilder::new();
        builder.push_leaf(L::default());
        Self {
            root: builder.build(),
        }
    }

    pub fn from_root(root: SharedNode<N, L>) -> Self {
        Self { root }
    }

    pub fn edit(&mut self, interval: Interval, text: &str)
    where
        N: DefaultMetricProvider<L>,
        L: Into<String> + From<String>,
    {
        let original = flatten_text(&self.root);
        let total_len = original.len();
        let (start, end) = normalize_interval(interval, total_len);

        let root_node = Node::from_shared(self.root.clone_handle());
        let utf16_units = N::convert_from_default::<Utf16Metric>(&root_node, start);
        let _base_units = N::convert_to_default::<Utf16Metric>(&root_node, utf16_units);

        let mut edited = String::with_capacity(total_len - (end - start) + text.len());
        edited.push_str(&original[..start]);
        edited.push_str(text);
        edited.push_str(&original[end..]);

        let (rebuilt, trace) = rebuild_from_text::<N, L>(&edited);
        if let Some(tracer) = trace {
            consume_trace(tracer);
        }
        self.root = rebuilt;
    }

    pub fn slice(&self, interval: Interval) -> Rope<N, L>
    where
        N: DefaultMetricProvider<L>,
        L: Into<String> + From<String>,
    {
        let original = flatten_text(&self.root);
        let total_len = original.len();
        let (start, end) = normalize_interval(interval, total_len);
        let slice = if start >= end {
            String::new()
        } else {
            original[start..end].to_owned()
        };
        let source = Node::from_shared(self.root.clone_handle());
        let _ = N::convert_from_default::<Utf16Metric>(&source, end.saturating_sub(start));
        let (root, trace) = rebuild_from_text::<N, L>(&slice);
        if let Some(tracer) = trace {
            consume_trace(tracer);
        }
        Rope::from_root(root)
    }

    pub fn measure<M: Metric<N, L>>(&self) -> M::Unit {
        M::measure(self.root.info(), self.root.len())
    }

    pub fn cursor(&self) -> Cursor<N, L> {
        Cursor::from_root(self.root.clone_handle())
    }

    pub fn apply_descriptor(&self, descriptor: CursorDescriptor) -> Cursor<N, L> {
        Cursor::restore(self.root.clone_handle(), descriptor)
    }

    pub fn root(&self) -> &SharedNode<N, L> {
        &self.root
    }
}

pub fn rebuild_text_for_tests<N, L>(
    text: &str,
) -> (SharedNode<N, L>, Option<TreeBuilderTrace>)
where
    N: NodeInfo<L>,
    L: Leaf + From<String>,
{
    rebuild_from_text(text)
}

fn normalize_interval(interval: Interval, total_len: usize) -> (usize, usize) {
    let start = interval.start().min(total_len);
    let end = interval.end().min(total_len);
    if start <= end {
        (start, end)
    } else {
        (end, start)
    }
}

fn flatten_text<N, L>(root: &SharedNode<N, L>) -> String
where
    N: NodeInfo<L>,
    L: Leaf + Into<String> + Clone,
{
    let mut leaves = Vec::new();
    collect_leaf_text(root.as_ref(), &mut leaves);
    leaves.concat()
}

fn collect_leaf_text<N, L>(body: &NodeBody<N, L>, leaves: &mut Vec<String>)
where
    N: NodeInfo<L>,
    L: Leaf + Into<String> + Clone,
{
    match &body.val {
        NodeVal::Leaf(leaf) => leaves.push(leaf.clone().into()),
        NodeVal::Internal(children) => {
            for child in children {
                collect_leaf_text(child.shared().as_ref(), leaves);
            }
        }
    }
}

fn rebuild_from_text<N, L>(text: &str) -> (SharedNode<N, L>, Option<TreeBuilderTrace>)
where
    N: NodeInfo<L>,
    L: Leaf + From<String>,
{
    let mut builder: TreeBuilder<N, L> = TreeBuilder::with_tracer(TreeBuilderTracer::new());
    builder.set_tracer_enabled(cfg!(feature = "tree_builder_slice_trace"));
    push_text_as_leaves(&mut builder, text);
    let (root, tracer) = builder.build_with_tracer();
    (root, tracer.map(|tracer| tracer.export()))
}

fn push_text_as_leaves<N, L>(builder: &mut TreeBuilder<N, L>, text: &str)
where
    N: NodeInfo<L>,
    L: Leaf + From<String>,
{
    if text.is_empty() {
        builder.push_leaf(L::default());
        return;
    }
    let mut offset = 0;
    while offset < text.len() {
        let remaining = &text[offset..];
        let mut split = string_leaf::split_for_insert(remaining);
        if split == 0 {
            split = remaining.len();
        }
        let next = offset + split;
        builder.push_leaf(L::from(remaining[..split].to_owned()));
        offset = next;
    }
}

fn consume_trace(trace: TreeBuilderTrace) {
    if cfg!(feature = "tree_builder_slice_trace") {
        let _ = trace.events().len();
    } else {
        let _ = trace;
    }
}
