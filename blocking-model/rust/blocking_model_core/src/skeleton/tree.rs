use std::fmt::Debug;
use std::mem;
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
        Self {
            arc: Arc::new(body),
        }
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

    pub fn child_at(&self, index: usize) -> Option<Node<N, L>> {
        match &self.arc.val {
            NodeVal::Internal(children) => children.get(index).cloned(),
            NodeVal::Leaf(_) => None,
        }
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
        let body = NodeBody {
            height,
            len,
            info,
            val: NodeVal::Internal(children),
        };
        SharedNode::new(body)
    }
}

impl<N: NodeInfo<L>, L: Leaf> Node<N, L> {
    pub fn from_leaf(leaf: L) -> Self {
        let info = N::compute_info(&leaf);
        let body = NodeBody {
            height: 0,
            len: leaf.len(),
            info,
            val: NodeVal::Leaf(leaf),
        };
        Node {
            shared: SharedNode::new(body),
        }
    }

    pub fn from_shared(shared: SharedNode<N, L>) -> Self {
        Node { shared }
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

    pub fn child_at(&self, index: usize) -> Option<Node<N, L>> {
        self.shared.child_at(index)
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
        let mut frames = Vec::new();
        let mut current = root.clone_handle();
        for child_index in descriptor.into_path() {
            let parent = current.clone_handle();
            frames.push(PathFrame::new(parent.clone_handle(), child_index));
            match parent.child_at(child_index) {
                Some(child) => {
                    current = child.into_shared();
                }
                None => break,
            }
        }
        Self { root, frames }
    }

    pub fn root(&self) -> &SharedNode<N, L> {
        &self.root
    }
}

#[derive(Clone, Debug, PartialEq, Eq)]
pub enum TreeBuilderEvent {
    PushLeaf {
        len: usize,
    },
    PushNode {
        len: usize,
        height: usize,
    },
    EnterChild {
        parent_len: usize,
        child_index: usize,
    },
    BuildComplete {
        total_len: usize,
        height: usize,
    },
}

#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct TreeBuilderTrace {
    events: Vec<TreeBuilderEvent>,
}

impl TreeBuilderTrace {
    pub fn new(events: Vec<TreeBuilderEvent>) -> Self {
        Self { events }
    }

    pub fn events(&self) -> &[TreeBuilderEvent] {
        &self.events
    }

    pub fn into_events(self) -> Vec<TreeBuilderEvent> {
        self.events
    }
}

#[derive(Clone, Debug, Default)]
pub struct TreeBuilderTracer {
    #[cfg(feature = "tree_builder_slice_trace")]
    events: Vec<TreeBuilderEvent>,
}

impl TreeBuilderTracer {
    pub fn new() -> Self {
        Self::default()
    }

    pub fn record(&mut self, event: TreeBuilderEvent) {
        #[cfg(feature = "tree_builder_slice_trace")]
        {
            self.events.push(event);
        }
        #[cfg(not(feature = "tree_builder_slice_trace"))]
        {
            let _ = event;
        }
    }

    pub fn events(&self) -> &[TreeBuilderEvent] {
        #[cfg(feature = "tree_builder_slice_trace")]
        {
            &self.events
        }
        #[cfg(not(feature = "tree_builder_slice_trace"))]
        {
            &[]
        }
    }

    pub fn into_events(self) -> Vec<TreeBuilderEvent> {
        #[cfg(feature = "tree_builder_slice_trace")]
        {
            self.events
        }
        #[cfg(not(feature = "tree_builder_slice_trace"))]
        {
            Vec::new()
        }
    }

    pub fn export(&self) -> TreeBuilderTrace {
        #[cfg(feature = "tree_builder_slice_trace")]
        {
            TreeBuilderTrace::new(self.events.clone())
        }
        #[cfg(not(feature = "tree_builder_slice_trace"))]
        {
            TreeBuilderTrace::default()
        }
    }
}

#[cfg(feature = "cursor_state")]
#[derive(Clone, Debug)]
pub struct CursorState {
    descriptor: CursorDescriptor,
}

#[cfg(feature = "cursor_state")]
impl CursorState {
    pub fn from_descriptor(descriptor: CursorDescriptor) -> Self {
        Self { descriptor }
    }

    pub fn to_descriptor(&self) -> CursorDescriptor {
        self.descriptor.clone()
    }

    pub fn descriptor(&self) -> &CursorDescriptor {
        &self.descriptor
    }
}

#[cfg(feature = "cursor_state")]
impl<N: NodeInfo<L>, L: Leaf> Cursor<N, L> {
    pub fn state(&self) -> CursorState {
        CursorState::from_descriptor(self.to_descriptor())
    }
}

pub struct TreeBuilder<N: NodeInfo<L>, L: Leaf> {
    pending: Vec<Node<N, L>>,
    tracer: Option<TreeBuilderTracer>,
    tracer_enabled: bool,
}

impl<N: NodeInfo<L>, L: Leaf> TreeBuilder<N, L> {
    pub fn new() -> Self {
        Self {
            pending: Vec::new(),
            tracer: None,
            tracer_enabled: cfg!(feature = "tree_builder_slice_trace"),
        }
    }

    pub fn with_tracer(tracer: TreeBuilderTracer) -> Self {
        Self {
            pending: Vec::new(),
            tracer: Some(tracer),
            tracer_enabled: cfg!(feature = "tree_builder_slice_trace"),
        }
    }

    pub fn tracer(&self) -> Option<&TreeBuilderTracer> {
        self.tracer.as_ref()
    }

    pub fn take_tracer(&mut self) -> Option<TreeBuilderTracer> {
        self.tracer.take()
    }

    pub fn set_tracer_enabled(&mut self, enabled: bool) {
        self.tracer_enabled = enabled && cfg!(feature = "tree_builder_slice_trace");
    }

    pub fn export_trace(&self) -> Option<TreeBuilderTrace> {
        self.tracer.as_ref().map(|tracer| tracer.export())
    }

    pub fn push_leaf(&mut self, leaf: L) {
        let len = leaf.len();
        self.record(TreeBuilderEvent::PushLeaf { len });
        self.push_node(Node::from_leaf(leaf));
    }

    pub fn push_node(&mut self, node: Node<N, L>) {
        let len = node.len();
        let height = node.height();
        self.record(TreeBuilderEvent::PushNode { len, height });
        self.pending.push(node);
    }

    pub fn build(self) -> SharedNode<N, L> {
        self.finish().0
    }

    pub fn build_with_tracer(self) -> (SharedNode<N, L>, Option<TreeBuilderTracer>) {
        self.finish()
    }

    fn finish(mut self) -> (SharedNode<N, L>, Option<TreeBuilderTracer>) {
        let root = match self.pending.len() {
            0 => Node::from_leaf(L::default()).into_shared(),
            1 => self.pending.pop().unwrap().into_shared(),
            _ => {
                let children = mem::take(&mut self.pending);
                let parent_len: usize = children.iter().map(|child| child.len()).sum();
                for (child_index, _) in children.iter().enumerate() {
                    self.record(TreeBuilderEvent::EnterChild {
                        parent_len,
                        child_index,
                    });
                }
                SharedNode::from_children(children)
            }
        };
        let summary = TreeBuilderEvent::BuildComplete {
            total_len: root.len(),
            height: root.height(),
        };
        self.record(summary);
        let tracer = self.tracer.take();
        (root, tracer)
    }

    fn record(&mut self, event: TreeBuilderEvent) {
        if self.tracer_enabled {
            if let Some(tracer) = self.tracer.as_mut() {
                tracer.record(event);
            }
        } else {
            let _ = event;
        }
    }
}
