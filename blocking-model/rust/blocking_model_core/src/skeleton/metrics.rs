use std::fmt::Debug;

use super::tree::{Leaf, Node, NodeInfo};

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
    fn convert_from_default<M: Metric<Self, L>>(node: &Node<Self, L>, offset: usize) -> usize;
    fn convert_to_default<M: Metric<Self, L>>(node: &Node<Self, L>, offset: usize) -> usize;
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
