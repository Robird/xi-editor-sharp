use super::metrics::{DefaultMetricProvider, Metric, Utf16Metric};
use super::tree::{
    Cursor, CursorDescriptor, Interval, Leaf, Node, NodeInfo, SharedNode, TreeBuilder,
    TreeBuilderTracer,
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
    {
        let tracer = TreeBuilderTracer::new();
        let mut builder: TreeBuilder<N, L> = TreeBuilder::with_tracer(tracer);
        builder.push_leaf(L::default());
        builder.push_leaf(L::default());
        let (rebuilt, maybe_tracer) = builder.build_with_tracer();

        let root_node = Node::from_shared(self.root.clone_handle());
        let base_offset = interval.start().min(root_node.len());
        let utf16_units = N::convert_from_default::<Utf16Metric>(&root_node, base_offset);
        let _normalized = N::convert_to_default::<Utf16Metric>(&root_node, utf16_units);
        if let Some(tracer) = maybe_tracer {
            let _ = tracer.events().len();
        }
        let _ = text;
        self.root = rebuilt;
    }

    pub fn slice(&self, interval: Interval) -> Rope<N, L>
    where
        N: DefaultMetricProvider<L>,
    {
        let mut builder: TreeBuilder<N, L> = TreeBuilder::new();
        // TODO: copy the actual leaves that overlap the interval instead of using defaults.
        builder.push_leaf(L::default());
        let rebuilt = builder.build();
        let source = Node::from_shared(self.root.clone_handle());
        let width = interval.end().saturating_sub(interval.start());
        let _ = N::convert_from_default::<Utf16Metric>(&source, width);
        Rope::from_root(rebuilt)
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
