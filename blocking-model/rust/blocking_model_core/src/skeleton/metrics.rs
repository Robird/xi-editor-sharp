use std::fmt::Debug;

use super::tree::{Leaf, Node, NodeBody, NodeInfo, NodeVal};

/// Mirrors xi-rope Metric trait while leaving behavior stubbed.
pub trait Metric<N: NodeInfo<L>, L: Leaf>: Copy + Debug {
    type Unit: Copy + Debug;

    fn measure(info: &N, len: usize) -> Self::Unit;
    fn to_base_units(leaf: &L, in_measured_units: Self::Unit) -> usize;
    fn from_base_units(leaf: &L, in_base_units: usize) -> Self::Unit;
    fn is_boundary(leaf: &L, offset: usize) -> bool;
    fn prev(leaf: &L, offset: usize) -> Option<usize>;
    fn next(leaf: &L, offset: usize) -> Option<usize>;
    fn can_fragment() -> bool;
}

/// Bridge trait used by NodeInfo implementations to convert between metrics.
pub trait DefaultMetricProvider<L: Leaf>: NodeInfo<L> {
    fn convert_from_default<M: Metric<Self, L>>(node: &Node<Self, L>, offset: usize) -> M::Unit {
        convert_from_default_impl::<Self, L, M>(node, offset)
    }

    fn convert_to_default<M: Metric<Self, L>>(node: &Node<Self, L>, units: M::Unit) -> usize {
        convert_to_default_impl::<Self, L, M>(node, units)
    }
}

#[derive(Copy, Clone, Debug)]
pub struct BaseMetric;

impl<N, L> Metric<N, L> for BaseMetric
where
    N: NodeInfo<L>,
    L: Leaf,
{
    type Unit = usize;

    fn measure(_info: &N, len: usize) -> Self::Unit {
        len
    }

    fn to_base_units(_leaf: &L, in_measured_units: Self::Unit) -> usize {
        in_measured_units
    }

    fn from_base_units(_leaf: &L, in_base_units: usize) -> Self::Unit {
        in_base_units
    }

    fn is_boundary(_leaf: &L, _offset: usize) -> bool {
        true
    }

    fn prev(_leaf: &L, offset: usize) -> Option<usize> {
        offset.checked_sub(1)
    }

    fn next(_leaf: &L, offset: usize) -> Option<usize> {
        Some(offset + 1)
    }

    fn can_fragment() -> bool {
        false
    }
}

#[derive(Copy, Clone, Debug)]
pub struct Utf16Metric;

impl<N, L> Metric<N, L> for Utf16Metric
where
    N: NodeInfo<L>,
    L: Leaf,
{
    type Unit = usize;

    fn measure(info: &N, len: usize) -> Self::Unit {
        let _ = info;
        len
    }

    fn to_base_units(leaf: &L, in_measured_units: Self::Unit) -> usize {
        let _ = leaf;
        in_measured_units
    }

    fn from_base_units(leaf: &L, in_base_units: usize) -> Self::Unit {
        let _ = leaf;
        in_base_units
    }

    fn is_boundary(leaf: &L, offset: usize) -> bool {
        let _ = leaf;
        offset == 0
    }

    fn prev(leaf: &L, offset: usize) -> Option<usize> {
        let _ = leaf;
        offset.checked_sub(2)
    }

    fn next(leaf: &L, offset: usize) -> Option<usize> {
        let _ = leaf;
        Some(offset + 2)
    }

    fn can_fragment() -> bool {
        true
    }
}

fn convert_from_default_impl<N, L, M>(node: &Node<N, L>, offset: usize) -> M::Unit
where
    N: NodeInfo<L>,
    L: Leaf,
    M: Metric<N, L>,
{
    let clamped = offset.min(node.len());
    let _total_in_metric = M::measure(node.info(), node.len());
    let default_leaf = L::default();
    let leaf = representative_leaf(node).unwrap_or(&default_leaf);
    M::from_base_units(leaf, clamped)
}

fn convert_to_default_impl<N, L, M>(node: &Node<N, L>, units: M::Unit) -> usize
where
    N: NodeInfo<L>,
    L: Leaf,
    M: Metric<N, L>,
{
    let _total_in_metric = M::measure(node.info(), node.len());
    let default_leaf = L::default();
    let leaf = representative_leaf(node).unwrap_or(&default_leaf);
    M::to_base_units(leaf, units)
}

fn representative_leaf<'a, N, L>(node: &'a Node<N, L>) -> Option<&'a L>
where
    N: NodeInfo<L>,
    L: Leaf,
{
    find_leaf(node.shared().as_ref())
}

fn find_leaf<'a, N, L>(body: &'a NodeBody<N, L>) -> Option<&'a L>
where
    N: NodeInfo<L>,
    L: Leaf,
{
    match &body.val {
        NodeVal::Leaf(leaf) => Some(leaf),
        NodeVal::Internal(children) => children
            .iter()
            .find_map(|child| find_leaf(child.shared().as_ref())),
    }
}
