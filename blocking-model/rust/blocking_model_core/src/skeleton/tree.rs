use std::fmt::Debug;
use std::sync::Arc;

#[derive(Clone, Copy, Debug, Default, PartialEq, Eq)]
pub struct Interval {
    start: usize,
    end: usize,
}

impl Interval {
    pub fn new(start: usize, end: usize) -> Self {
        Self { start, end }
    }

    pub fn start(&self) -> usize {
        self.start
    }

    pub fn end(&self) -> usize {
        self.end
    }
}

/// Leaf trait mirrors xi-rope requirements but keeps logic stubbed out.
pub trait Leaf: Clone + Default {
    fn len(&self) -> usize;
    fn is_ok_child(&self) -> bool;
    fn push_maybe_split(&mut self, other: &Self, iv: Interval) -> Option<Self>;
}

/// NodeInfo retains the monoid style API used by the actual rope tree.
pub trait NodeInfo<L: Leaf>: Clone + Debug {
    fn accumulate(&mut self, other: &Self);
    fn compute_info(leaf: &L) -> Self;
    fn identity() -> Self {
        Self::compute_info(&L::default())
    }
    fn interval(&self, len: usize) -> Interval {
        Interval::new(0, len)
    }
}

#[derive(Clone)]
pub struct SharedNode<N: NodeInfo<L>, L: Leaf> {
    arc: Arc<NodeBody<N, L>>,
}

#[derive(Clone)]
pub struct Node<N: NodeInfo<L>, L: Leaf> {
    shared: SharedNode<N, L>,
}

#[derive(Clone)]
pub struct NodeBody<N: NodeInfo<L>, L: Leaf> {
    pub height: usize,
    pub len: usize,
    pub info: N,
    pub val: NodeVal<N, L>,
}

#[derive(Clone)]
pub enum NodeVal<N: NodeInfo<L>, L: Leaf> {
    Leaf(L),
    Internal(Vec<Node<N, L>>),
}

impl<N: NodeInfo<L>, L: Leaf> SharedNode<N, L> {
    pub fn new(body: NodeBody<N, L>) -> Self {
        Self { arc: Arc::new(body) }
    }

    pub fn clone_handle(&self) -> Self {
        Self {
            arc: Arc::clone(&self.arc),
        }
    }

    pub fn len(&self) -> usize {
        self.arc.len
    }

    pub fn height(&self) -> usize {
        self.arc.height
    }

    pub fn info(&self) -> &N {
        &self.arc.info
    }

    pub fn as_ref(&self) -> &NodeBody<N, L> {
        &self.arc
    }

    pub fn ptr_eq(&self, other: &Self) -> bool {
        Arc::ptr_eq(&self.arc, &other.arc)
    }

    pub fn from_children(children: Vec<Node<N, L>>) -> Self {
        debug_assert!(children.len() > 1, "requires at least two children");
        let height = children.first().map(|c| c.height() + 1).unwrap_or(1);
        let mut info = N::identity();
        let mut len = 0;
        for child in &children {
            len += child.len();
            info.accumulate(child.info());
        }
        let body = NodeBody { height, len, info, val: NodeVal::Internal(children) };
        SharedNode::new(body)
    }
}

impl<N: NodeInfo<L>, L: Leaf> Node<N, L> {
    pub fn from_leaf(leaf: L) -> Self {
        let info = N::compute_info(&leaf);
        let body = NodeBody { height: 0, len: leaf.len(), info, val: NodeVal::Leaf(leaf) };
        Node {
            shared: SharedNode::new(body),
        }
    }

    pub fn from_children(children: Vec<Node<N, L>>) -> Self {
        Node {
            shared: SharedNode::from_children(children),
        }
    }

    pub fn len(&self) -> usize {
        self.shared.len()
    }

    pub fn height(&self) -> usize {
        self.shared.height()
    }

    pub fn is_leaf(&self) -> bool {
        matches!(self.shared.as_ref().val, NodeVal::Leaf(_))
    }

    pub fn ptr_eq(&self, other: &Self) -> bool {
        self.shared.ptr_eq(other.shared())
    }

    pub fn info(&self) -> &N {
        self.shared.info()
    }

    pub fn shared(&self) -> &SharedNode<N, L> {
        &self.shared
    }

    pub fn into_shared(self) -> SharedNode<N, L> {
        self.shared
    }
}

#[derive(Clone)]
pub struct PathFrame<N: NodeInfo<L>, L: Leaf> {
    pub node: SharedNode<N, L>,
    pub child_index: usize,
}

impl<N: NodeInfo<L>, L: Leaf> PathFrame<N, L> {
    pub fn new(node: SharedNode<N, L>, child_index: usize) -> Self {
        Self { node, child_index }
    }
}

#[derive(Clone, Debug, PartialEq, Eq)]
pub struct CursorDescriptor {
    path: Vec<usize>,
}

impl CursorDescriptor {
    pub fn new(path: Vec<usize>) -> Self {
        Self { path }
    }

    pub fn path(&self) -> &[usize] {
        &self.path
    }

    pub fn into_path(self) -> Vec<usize> {
        self.path
    }
}

pub struct Cursor<N: NodeInfo<L>, L: Leaf> {
    root: SharedNode<N, L>,
    frames: Vec<PathFrame<N, L>>,
}

impl<N: NodeInfo<L>, L: Leaf> Cursor<N, L> {
    pub fn from_root(root: SharedNode<N, L>) -> Self {
        Self {
            root,
            frames: Vec::new(),
        }
    }

    pub fn push_frame(&mut self, node: SharedNode<N, L>, child_index: usize) {
        self.frames.push(PathFrame::new(node, child_index));
    }

    pub fn to_descriptor(&self) -> CursorDescriptor {
        let path = self.frames.iter().map(|frame| frame.child_index).collect();
        CursorDescriptor::new(path)
    }

    pub fn restore(root: SharedNode<N, L>, descriptor: CursorDescriptor) -> Self {
        let frames = descriptor
            .into_path()
            .into_iter()
            .map(|child_index| PathFrame::new(root.clone_handle(), child_index))
            .collect();
        Self { root, frames }
    }

    pub fn root(&self) -> &SharedNode<N, L> {
        &self.root
    }
}

#[cfg(feature = "cursor_state")]
#[derive(Clone, Debug)]
pub struct CursorState {
    descriptor: CursorDescriptor,
}

#[cfg(feature = "cursor_state")]
impl CursorState {
    pub fn descriptor(&self) -> &CursorDescriptor {
        &self.descriptor
    }
}

#[cfg(feature = "cursor_state")]
impl<N: NodeInfo<L>, L: Leaf> Cursor<N, L> {
    pub fn state(&self) -> CursorState {
        CursorState {
            descriptor: self.to_descriptor(),
        }
    }
}

pub struct TreeBuilder<N: NodeInfo<L>, L: Leaf> {
    pending: Vec<Node<N, L>>,
}

impl<N: NodeInfo<L>, L: Leaf> TreeBuilder<N, L> {
    pub fn new() -> Self {
        Self { pending: Vec::new() }
    }

    pub fn push_leaf(&mut self, leaf: L) {
        self.push_node(Node::from_leaf(leaf));
    }

    pub fn push_node(&mut self, node: Node<N, L>) {
        self.pending.push(node);
    }

    pub fn build(mut self) -> SharedNode<N, L> {
        match self.pending.len() {
            0 => Node::from_leaf(L::default()).into_shared(),
            1 => self.pending.pop().unwrap().into_shared(),
            _ => SharedNode::from_children(self.pending),
        }
    }
}
