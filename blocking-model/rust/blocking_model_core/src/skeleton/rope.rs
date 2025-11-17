use super::metrics::Metric;
use super::tree::{Cursor, CursorDescriptor, Interval, Leaf, Node, NodeInfo, SharedNode, TreeBuilder};

/// Rope exposes the same surface area while delegating to skeleton stubs.
pub struct Rope<N: NodeInfo<L>, L: Leaf> {
    root: SharedNode<N, L>,
}

impl<N: NodeInfo<L>, L: Leaf> Rope<N, L> {
    pub fn new() -> Self {
        let mut builder: TreeBuilder<N, L> = TreeBuilder::new();
        builder.push_leaf(L::default());
        Self { root: builder.build() }
    }

    pub fn from_root(root: SharedNode<N, L>) -> Self {
        Self { root }
    }

    pub fn edit(&mut self, interval: Interval, _text: &str) {
        let mut builder: TreeBuilder<N, L> = TreeBuilder::new();
        builder.push_node(Node::from_leaf(L::default()));
        let _rebuilt = builder.build();
        let _ = interval;
        unimplemented!("skeleton stub: Rope::edit");
    }

    pub fn slice(&self, interval: Interval) -> Rope<N, L> {
        let _ = interval;
        Rope::from_root(self.root.clone_handle())
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
