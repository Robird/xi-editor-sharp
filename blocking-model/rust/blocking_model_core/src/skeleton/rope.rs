use super::tree::{Cursor, CursorDescriptor, Leaf, NodeInfo, SharedNode};

/// Rope 保留 root + cursor/descriptor 的接口形式，逻辑降至最小实现。
pub struct Rope<N: NodeInfo, L: Leaf<Metric = N::Metric>> {
    root: SharedNode<N, L>,
}

impl<N: NodeInfo, L: Leaf<Metric = N::Metric>> Rope<N, L> {
    pub fn from_root(root: SharedNode<N, L>) -> Self {
        Self { root }
    }

    pub fn root(&self) -> &SharedNode<N, L> {
        &self.root
    }

    pub fn cursor(&self) -> Cursor<'_, N, L> {
        Cursor::new(self.root.as_ref())
    }

    pub fn apply_descriptor(&self, descriptor: CursorDescriptor<N>) -> Cursor<'_, N, L> {
        let _ = descriptor;
        self.cursor()
    }
}
