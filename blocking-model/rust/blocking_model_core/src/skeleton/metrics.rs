use std::fmt::Debug;

/// Mirror of xi-editor `Metric` trait：保留静态约束，移除实际统计逻辑。
pub trait Metric: Copy + Debug {
    type Unit: Copy + Debug;

    fn zero() -> Self::Unit;
}

#[derive(Copy, Clone, Debug)]
pub struct BaseMetric;

impl Metric for BaseMetric {
    type Unit = usize;

    fn zero() -> Self::Unit {
        0
    }
}

#[derive(Copy, Clone, Debug)]
pub struct Utf16Metric;

impl Metric for Utf16Metric {
    type Unit = usize;

    fn zero() -> Self::Unit {
        0
    }
}

/// 约束 NodeInfo/Leaf 之间的 metric 绑定关系。
pub trait MetricBinder: Copy + Debug {
    type Metric: Metric;

    fn metric() -> Self::Metric;
}

#[derive(Copy, Clone, Debug)]
pub struct DefaultMetricBinder;

impl MetricBinder for DefaultMetricBinder {
    type Metric = BaseMetric;

    fn metric() -> Self::Metric {
        BaseMetric
    }
}
