use std::fmt::Debug;
use std::marker::PhantomData;
use std::sync::Arc;

use super::metrics::Metric;

/// Leaf 接口：保留 Metric 约束与访问器，但不关心实际文本操作。
pub trait Leaf: Clone {
    type Metric: Metric;

    fn text(&self) -> &str;
}

/// NodeInfo 接口：描述节点级别的聚合与 cursor 摘要。
pub trait NodeInfo: Clone {
    type Metric: Metric;
    type Summary: Clone + Debug;

    fn describe_cursor(&self) -> Self::Summary;
}

/// Node 仅保留类型参数和 info，对应 xi-editor `Node<N, L>`。
pub struct Node<N: NodeInfo, L: Leaf<Metric = N::Metric>> {
    info: N,
    _marker: PhantomData<L>,
}

impl<N: NodeInfo, L: Leaf<Metric = N::Metric>> Node<N, L> {
    pub fn new(info: N) -> Self {
        Self {
            info,
            _marker: PhantomData,
        }
    }

    pub fn info(&self) -> &N {
        &self.info
    }
}

#[derive(Clone)]
pub struct SharedNode<N: NodeInfo, L: Leaf<Metric = N::Metric>>(Arc<Node<N, L>>);

impl<N: NodeInfo, L: Leaf<Metric = N::Metric>> SharedNode<N, L> {
    pub fn new(node: Node<N, L>) -> Self {
        Self(Arc::new(node))
    }

    pub fn as_ref(&self) -> &Node<N, L> {
        &self.0
    }

    pub fn clone_handle(&self) -> Self {
        Self(Arc::clone(&self.0))
    }
}

pub struct Cursor<'a, N: NodeInfo, L: Leaf<Metric = N::Metric>> {
    node: &'a Node<N, L>,
}

impl<'a, N: NodeInfo, L: Leaf<Metric = N::Metric>> Cursor<'a, N, L> {
    pub fn new(node: &'a Node<N, L>) -> Self {
        Self { node }
    }

    pub fn node(&self) -> &'a Node<N, L> {
        self.node
    }

    pub fn to_descriptor(&self) -> CursorDescriptor<N> {
        CursorDescriptor {
            summary: self.node.info().describe_cursor(),
        }
    }
}

#[derive(Clone, Debug)]
pub struct CursorDescriptor<N: NodeInfo> {
    summary: N::Summary,
}

impl<N: NodeInfo> CursorDescriptor<N> {
    pub fn summary(&self) -> &N::Summary {
        &self.summary
    }
}
