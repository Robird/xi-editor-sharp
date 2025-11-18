## xi-editor-ph7/rust/rope/examples/ropetoy.rs

```rust
extern crate xi_rope;

use xi_rope::Rope;

fn main() {...}
```

## xi-editor-ph7/rust/rope/src/bin/export-serde-fixtures.rs

```rust
#[cfg(not(feature = "serde"))]
fn main() {...}

#[cfg(feature = "serde")]
use std::{env, path::PathBuf};

#[cfg(all(feature = "serde", feature = "tree_builder_slice_trace"))]
use std::{cell::RefCell, collections::HashMap, rc::Rc};

#[cfg(feature = "serde")]
use serde::Serialize;

#[cfg(feature = "serde")]
use serde_json::Value;

#[cfg(feature = "serde")]
use sha2::{Digest, Sha256};

#[cfg(feature = "serde")]
use xi_rope::serde_fixtures::{
    export_breaks_descriptors, export_chunk_descriptors, export_cursor_descriptor_fixtures,
    export_diff_regions, export_grapheme_descriptors, export_search_spans, fixtures,
    BreaksDescriptorExportReport, ChunkDescriptorExportReport, DiffRegionsExportReport, Fixture,
    GraphemeDescriptorExportReport, SearchSpansExportReport, BREAKS_DESCRIPTOR_FILENAME,
    CHUNK_DESCRIPTOR_FILENAME, CURSOR_DESCRIPTOR_FILENAME, DIFF_REGIONS_FILENAME,
    GRAPHEME_DESCRIPTOR_FILENAME, SEARCH_SPANS_FILENAME,
};

#[cfg(feature = "serde")]
const DEFAULT_MANIFEST_RELATIVE: &str =
    "../../../tests/xi.Core.Tests/Fixtures/fixtures.manifest.json";
#[cfg(feature = "serde")]
const SUBSET_SCHEMA_HASH: &str = "serde_fixtures::subset";
#[cfg(feature = "serde")]
const DELTA_SCHEMA_HASH: &str = "serde_fixtures::delta";
#[cfg(feature = "serde")]
const ENGINE_SCHEMA_HASH: &str = "serde_fixtures::engine";
#[cfg(feature = "serde")]
const CURSOR_SCHEMA_HASH: &str = "cursor_descriptors@1.2.0";
#[cfg(feature = "serde")]
const CHUNK_SCHEMA_HASH: &str = "chunk_descriptors@1.0.0";
#[cfg(feature = "serde")]
const GRAPHEME_SCHEMA_HASH: &str = "grapheme_descriptors@1.0.0";
#[cfg(feature = "serde")]
const BREAKS_SCHEMA_HASH: &str = "breaks_descriptors@1.0.0";
#[cfg(feature = "serde")]
const DIFF_REGIONS_SCHEMA_HASH: &str = "diff_regions@1.0.0";
#[cfg(feature = "serde")]
const SEARCH_SPANS_SCHEMA_HASH: &str = "search_spans@1.0.0";
#[cfg(feature = "serde")]
const TREE_BUILDER_TRACE_SCHEMA_HASH: &str = "tree_builder_slice_trace@1.0.0";
#[cfg(feature = "serde")]
const METRIC_WINDOWS_SCHEMA_HASH: &str = "metric_windows@1.0.0";

#[cfg(feature = "serde")]
#[derive(Serialize)]
struct FixtureManifest {
    rust_commit: String,
    cli_rev: String,
    feature_gates: Vec<String>,
    fixtures: Vec<ManifestFixture>,
    #[serde(skip_serializing_if = "Vec::is_empty")]
    metric_windows: Vec<MetricWindowsEntry>,
}

#[cfg(feature = "serde")]
#[derive(Serialize)]
struct ManifestFixture {
    name: String,
    path: String,
    count: usize,
    schema_hash: String,
    payload_hash: String,
}

#[cfg(feature = "serde")]
#[derive(Serialize)]
struct MetricWindowsEntry {
    fixture: String,
    schema_hash: String,
    schema_version: String,
    window_schema: String,
    windows: Vec<MetricWindow>,
}

#[cfg(feature = "serde")]
#[derive(Serialize)]
struct MetricWindow {
    kind: String,
    count: usize,
}

#[cfg(feature = "serde")]
struct FixtureFileReport {
    name: String,
    path: PathBuf,
}

#[cfg(feature = "serde")]
struct TreeBuilderTraceExportReport {
    file_name: String,
    file_path: PathBuf,
    event_count: usize,
}

#[cfg(all(feature = "serde", feature = "tree_builder_slice_trace"))]
use xi_rope::{
    tree::{TreeBuilder, TreeBuilderEvent, TreeBuilderEventKind, TreeBuilderTracer},
    Interval, Rope, RopeInfo,
};

#[cfg(all(feature = "serde", feature = "tree_builder_slice_trace"))]
const TREE_BUILDER_TRACE_FILENAME: &str = "basic_slice_plan.json";

#[cfg(feature = "serde")]
fn main() -> Result<(), Box<dyn std::error::Error>> {...}

#[cfg(feature = "serde")]
fn print_usage() {...}

#[cfg(feature = "serde")]
fn list_fixtures() {...}

#[cfg(feature = "serde")]
fn export_to_directory(
    dir: &std::path::Path,
    fixtures: &[Fixture],
) -> Result<Vec<FixtureFileReport>, Box<dyn std::error::Error>> {...}

#[cfg(all(feature = "serde", feature = "tree_builder_slice_trace"))]
fn handle_tree_builder_trace(
    dir: PathBuf,
) -> Result<TreeBuilderTraceExportReport, Box<dyn std::error::Error>> {...}

#[cfg(all(feature = "serde", not(feature = "tree_builder_slice_trace")))]
fn handle_tree_builder_trace(
    _dir: PathBuf,
) -> Result<TreeBuilderTraceExportReport, Box<dyn std::error::Error>> {...}

#[cfg(feature = "serde")]
fn report_chunk_export(report: &ChunkDescriptorExportReport) {...}

#[cfg(feature = "serde")]
fn report_grapheme_export(report: &GraphemeDescriptorExportReport) {...}

#[cfg(feature = "serde")]
fn report_breaks_export(report: &BreaksDescriptorExportReport) {...}

#[cfg(feature = "serde")]
fn report_diff_export(report: &DiffRegionsExportReport) {...}

#[cfg(feature = "serde")]
fn report_search_export(report: &SearchSpansExportReport) {...}

#[cfg(feature = "serde")]
fn report_tree_builder_trace_export(report: &TreeBuilderTraceExportReport) {...}

#[cfg(feature = "serde")]
fn write_manifest(
    path: &std::path::Path,
    fixtures: Vec<ManifestFixture>,
    metric_windows: Vec<MetricWindowsEntry>,
) -> Result<(), Box<dyn std::error::Error>> {...}

#[cfg(feature = "serde")]
fn collect_feature_gates() -> Vec<String> {...}

#[cfg(feature = "serde")]
fn compute_payload_hash(path: &std::path::Path) -> Result<String, Box<dyn std::error::Error>> {...}

#[cfg(feature = "serde")]
fn hash_value(value: &Value) -> String {...}

#[cfg(feature = "serde")]
fn write_canonical_json(value: &Value, out: &mut String) {...}

#[cfg(feature = "serde")]
fn hex_encode(bytes: &[u8]) -> String {...}

#[cfg(feature = "serde")]
fn schema_hash_for_regression(name: &str) -> String {...}

#[cfg(feature = "serde")]
fn manifest_display_path(path: &std::path::Path) -> String {...}

#[cfg(feature = "serde")]
fn normalize_path(path: std::path::PathBuf) -> String {...}

#[cfg(feature = "serde")]
fn workspace_root() -> PathBuf {...}

#[cfg(feature = "serde")]
fn default_manifest_path() -> PathBuf {...}

#[cfg(feature = "serde")]
fn default_fixture_root() -> PathBuf {...}

#[cfg(feature = "serde")]
fn record_metric_windows(
    ledger: &mut Vec<MetricWindowsEntry>,
    fixture: &str,
    schema_hash: &str,
    windows: Vec<MetricWindow>,
) {...}

#[cfg(feature = "serde")]
fn schema_version_from_hash(schema_hash: &str) -> String {...}

#[cfg(all(feature = "serde", feature = "tree_builder_slice_trace"))]
fn export_tree_builder_trace(
    dir: &std::path::Path,
) -> Result<TreeBuilderTraceExportReport, Box<dyn std::error::Error>> {...}

#[cfg(all(feature = "serde", feature = "tree_builder_slice_trace"))]
fn convert_events(events: &[TreeBuilderEvent]) -> Vec<SerializableEvent> {...}

#[cfg(all(feature = "serde", feature = "tree_builder_slice_trace"))]
struct NodeIdMapper {
    next: u64,
    map: HashMap<usize, u64>,
}

#[cfg(all(feature = "serde", feature = "tree_builder_slice_trace"))]
impl NodeIdMapper {
    fn new() -> Self {...}

    fn map(&mut self, ptr: usize) -> u64 {...}
}

#[cfg(all(feature = "serde", feature = "tree_builder_slice_trace"))]
#[derive(Serialize)]
struct SerializableEvent {
    kind: SerializableEventKind,
    depth: usize,
    node_height: usize,
    node_len: usize,
    node_id: u64,
    reuse: bool,
}

#[cfg(all(feature = "serde", feature = "tree_builder_slice_trace"))]
impl SerializableEvent {
    fn from_event(event: &TreeBuilderEvent, mapper: &mut NodeIdMapper) -> Self {...}
}

#[cfg(all(feature = "serde", feature = "tree_builder_slice_trace"))]
#[derive(Serialize)]
#[serde(tag = "kind")]
enum SerializableEventKind {
    PushFrame,
    ExtendFrame,
    MergePop { merged_children: usize },
    LeafSlice { interval: SerializableInterval },
    EnterChild { requested: SerializableInterval, translated: SerializableInterval },
}

#[cfg(all(feature = "serde", feature = "tree_builder_slice_trace"))]
impl From<&TreeBuilderEventKind> for SerializableEventKind {
    fn from(kind: &TreeBuilderEventKind) -> Self {...}
}

#[cfg(all(feature = "serde", feature = "tree_builder_slice_trace"))]
#[derive(Serialize)]
struct SerializableInterval {
    start: usize,
    end: usize,
}

#[cfg(all(feature = "serde", feature = "tree_builder_slice_trace"))]
impl From<Interval> for SerializableInterval {
    fn from(interval: Interval) -> Self {...}
}

#[cfg(all(feature = "serde", feature = "tree_builder_slice_trace"))]
struct RecordingTracer {
    events: Rc<RefCell<Vec<TreeBuilderEvent>>>,
}

#[cfg(all(feature = "serde", feature = "tree_builder_slice_trace"))]
impl RecordingTracer {
    fn new(events: Rc<RefCell<Vec<TreeBuilderEvent>>>) -> Self {...}
}

#[cfg(all(feature = "serde", feature = "tree_builder_slice_trace"))]
impl TreeBuilderTracer<RopeInfo, String> for RecordingTracer {
    fn record(&mut self, event: TreeBuilderEvent) {...}
}
```

## xi-editor-ph7/rust/rope/src/breaks.rs

```rust
use crate::interval::Interval;
use crate::metrics::{
    count_breaks_up_to, find_next_break, find_prev_break, is_break_boundary, nth_break_offset,
    BreaksBaseMetric,
};
use crate::tree::{DefaultMetricProvider, Leaf, Metric, Node, NodeInfo, TreeBuilder};
use std::cmp::min;
use std::mem;
use std::ops::Range;

/// A set of indexes. A motivating use is storing line breaks.
pub type Breaks = Node<BreaksInfo, BreaksLeaf>;

const MIN_LEAF: usize = 32;
const MAX_LEAF: usize = 64;

// Here the base units are arbitrary, but most commonly match the base units
// of the rope storing the underlying string.

#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct BreaksLeaf {
    /// Length, in base units.
    len: usize,
    /// Indexes, represent as offsets from the start of the leaf.
    data: Vec<usize>,
}

/// The number of breaks.
#[derive(Clone, Debug)]
pub struct BreaksInfo(usize);

impl Leaf for BreaksLeaf {
    fn len(&self) -> usize {...}

    fn is_ok_child(&self) -> bool {...}

    fn push_maybe_split(&mut self, other: &BreaksLeaf, iv: Interval) -> Option<BreaksLeaf> {...}
}

impl NodeInfo<BreaksLeaf> for BreaksInfo {
    fn accumulate(&mut self, other: &Self) {...}

    fn compute_info(l: &BreaksLeaf) -> BreaksInfo {...}
}

impl DefaultMetricProvider<BreaksLeaf> for BreaksInfo {
    fn convert_from_default<M: Metric<Self, BreaksLeaf>>(
        node: &Node<Self, BreaksLeaf>,
        offset: usize,
    ) -> usize {...}

    fn convert_to_default<M: Metric<Self, BreaksLeaf>>(
        node: &Node<Self, BreaksLeaf>,
        offset: usize,
    ) -> usize {...}
}

impl BreaksLeaf {
    /// Exposed for testing.
    #[doc(hidden)]
    pub fn get_data_cloned(&self) -> Vec<usize> {...}
}

#[derive(Copy, Clone)]
pub struct BreaksMetric(());

impl Metric<BreaksInfo, BreaksLeaf> for BreaksMetric {
    fn measure(info: &BreaksInfo, _: usize) -> usize {...}

    fn to_base_units(l: &BreaksLeaf, in_measured_units: usize) -> usize {...}

    fn from_base_units(l: &BreaksLeaf, in_base_units: usize) -> usize {...}

    fn is_boundary(l: &BreaksLeaf, offset: usize) -> bool {...}

    fn prev(l: &BreaksLeaf, offset: usize) -> Option<usize> {...}

    fn next(l: &BreaksLeaf, offset: usize) -> Option<usize> {...}

    fn can_fragment() -> bool {...}
}

// Additional functions specific to breaks

impl Breaks {
    // a length with no break, useful in edit operations; for
    // other use cases, use the builder.
    pub fn new_no_break(len: usize) -> Breaks {...}

    /// Interop shim that counts soft breaks before or at `offset` without exposing metrics.
    #[inline]
    pub fn count_breaks_up_to(&self, offset: usize) -> usize {...}

    /// Interop shim that returns the base-unit offset for the `index`th soft break.
    #[inline]
    pub fn offset_of_break(&self, index: usize) -> usize {...}

    /// Interop shim that reports how many soft breaks lie within `range`.
    #[inline]
    pub fn count_breaks_in_range(&self, range: Range<usize>) -> usize {...}
}

pub struct BreakBuilder {
    b: TreeBuilder<BreaksInfo, BreaksLeaf>,
    leaf: BreaksLeaf,
}

impl Default for BreakBuilder {
    fn default() -> BreakBuilder {...}
}

impl BreakBuilder {
    pub fn new() -> BreakBuilder {...}

    pub fn add_break(&mut self, len: usize) {...}

    pub fn add_no_break(&mut self, len: usize) {...}

    pub fn build(mut self) -> Breaks {...}
}
```

## xi-editor-ph7/rust/rope/src/compare.rs

```rust
use crate::rope::{BaseMetric, Rope, RopeInfo};
use crate::tree::Cursor;

#[allow(dead_code)]
const SSE_STRIDE: usize = 16;

/// Given two 16-byte slices, returns a bitmask where the 1 bits indicate
/// the positions of non-equal bytes.
///
/// The least significant bit in the mask refers to the byte in position 0;
/// that is, you read the mask right to left.
///
/// # Examples
///
/// ```
/// # use xi_rope::compare::sse_compare_mask;
/// # if is_x86_feature_detected!("sse4.2") {
/// let one = "aaaaaaaaaaaaaaaa";
/// let two = "aa3aaaaa9aaaEaaa";
/// let exp = "0001000100000100";
/// let mask = unsafe { sse_compare_mask(one.as_bytes(), two.as_bytes()) };
/// let result = format!("{:016b}", mask);
/// assert_eq!(result.as_str(), exp);
/// # }
/// ```
///
#[allow(clippy::cast_ptr_alignment, clippy::unreadable_literal)]
#[doc(hidden)]
#[cfg(target_arch = "x86_64")]
#[target_feature(enable = "sse4.2")]
pub unsafe fn sse_compare_mask(one: &[u8], two: &[u8]) -> i32 {...}

#[allow(dead_code)]
const AVX_STRIDE: usize = 32;

/// Like above but with 32 byte slices
#[allow(clippy::cast_ptr_alignment, clippy::unreadable_literal)]
#[doc(hidden)]
#[cfg(target_arch = "x86_64")]
#[target_feature(enable = "avx2")]
pub unsafe fn avx_compare_mask(one: &[u8], two: &[u8]) -> i32 {...}

/// Returns the lowest `i` for which `one[i] != two[i]`, if one exists.
pub fn ne_idx(one: &[u8], two: &[u8]) -> Option<usize> {...}

/// Returns the lowest `i` such that `one[one.len()-i] != two[two.len()-i]`,
/// if one exists.
pub fn ne_idx_rev(one: &[u8], two: &[u8]) -> Option<usize> {...}

#[doc(hidden)]
#[cfg(target_arch = "x86_64")]
#[target_feature(enable = "avx2")]
pub unsafe fn ne_idx_avx(one: &[u8], two: &[u8]) -> Option<usize> {...}

#[doc(hidden)]
#[cfg(target_arch = "x86_64")]
#[target_feature(enable = "sse4.2")]
pub unsafe fn ne_idx_sse(one: &[u8], two: &[u8]) -> Option<usize> {...}

#[doc(hidden)]
#[cfg(target_arch = "x86_64")]
#[target_feature(enable = "sse4.2")]
pub unsafe fn ne_idx_rev_sse(one: &[u8], two: &[u8]) -> Option<usize> {...}

#[inline]
#[allow(dead_code)]
#[doc(hidden)]
pub fn ne_idx_fallback(one: &[u8], two: &[u8]) -> Option<usize> {...}

#[inline]
#[allow(dead_code)]
#[doc(hidden)]
pub fn ne_idx_rev_fallback(one: &[u8], two: &[u8]) -> Option<usize> {...}

/// Utility for efficiently comparing two ropes.
pub struct RopeScanner<'a> {
    base: Cursor<'a, RopeInfo, String>,
    target: Cursor<'a, RopeInfo, String>,
    base_chunk: &'a str,
    target_chunk: &'a str,
    scanned: usize,
}

impl<'a> RopeScanner<'a> {
    pub fn new(base: &'a Rope, target: &'a Rope) -> Self {...}

    /// Starting from the two provided offsets in the corresponding ropes,
    /// Returns the distance, moving backwards, to the first non-equal codepoint.
    /// If no such position exists, returns the distance to the closest 0 offset.
    ///
    /// if `stop` is not None, the scan will stop at if it reaches this value.
    ///
    /// # Examples
    ///
    /// ```
    /// # use xi_rope::compare::RopeScanner;
    /// # use xi_rope::Rope;
    ///
    /// let one = Rope::from("hiii");
    /// let two = Rope::from("siii");
    /// let mut scanner = RopeScanner::new(&one, &two);
    /// assert_eq!(scanner.find_ne_char_back(one.len(), two.len(), None), 3);
    /// assert_eq!(scanner.find_ne_char_back(one.len(), two.len(), 2), 2);
    /// ```
    pub fn find_ne_char_back<T>(&mut self, base_off: usize, targ_off: usize, stop: T) -> usize
    where
        T: Into<Option<usize>>,
    {...}

    /// Starting from the two provided offsets into the two ropes, returns
    /// the distance (in bytes) to the first non-equal codepoint. If no such
    /// position exists, returns the shortest distance to the end of a rope.
    ///
    /// This can be thought of as the length of the longest common substring
    /// between `base[base_off..]` and `target[targ_off..]`.
    ///
    /// if `stop` is not None, the scan will stop at if it reaches this value.
    ///
    /// # Examples
    ///
    /// ```
    /// # use xi_rope::compare::RopeScanner;
    /// # use xi_rope::Rope;
    ///
    /// let one = Rope::from("uh-oh🙈");
    /// let two = Rope::from("uh-oh🙉");
    /// let mut scanner = RopeScanner::new(&one, &two);
    /// assert_eq!(scanner.find_ne_char(0, 0, None), 5);
    /// assert_eq!(scanner.find_ne_char(0, 0, 3), 3);
    /// ```
    pub fn find_ne_char<T>(&mut self, base_off: usize, targ_off: usize, stop: T) -> usize
    where
        T: Into<Option<usize>>,
    {...}

    /// Returns the positive offset from the start of the rope to the first
    /// non-equal byte, and the negative offset from the end of the rope to
    /// the first non-equal byte.
    ///
    /// The two offsets are guaranteed not to overlap;
    /// thus `sum(start_offset, end_offset) <= min(one.len(), two.len())`.
    ///
    /// # Examples
    ///
    /// ```
    /// # use xi_rope::compare::RopeScanner;
    /// # use xi_rope::Rope;
    ///
    /// let one = Rope::from("123xxx12345");
    /// let two = Rope::from("123ZZZ12345");
    /// let mut scanner = RopeScanner::new(&one, &two);
    /// assert_eq!(scanner.find_min_diff_range(), (3, 5));
    ///
    ///
    /// let one = Rope::from("friends");
    /// let two = Rope::from("fiends");
    /// let mut scanner = RopeScanner::new(&one, &two);
    /// assert_eq!(scanner.find_min_diff_range(), (1, 5))
    /// ```
    pub fn find_min_diff_range(&mut self) -> (usize, usize) {...}

    fn load_prev_chunk(&mut self) {...}

    fn load_next_chunk(&mut self) {...}
}
```

## xi-editor-ph7/rust/rope/src/delta.rs

```rust
use crate::interval::{Interval, IntervalBounds};
use crate::multiset::{CountMatcher, Subset, SubsetBuilder};
use crate::tree::{Leaf, Node, NodeInfo, TreeBuilder};
use std::cmp::min;
use std::fmt;
use std::ops::Deref;
use std::slice;

#[derive(Clone)]
pub enum DeltaElement<N: NodeInfo<L>, L: Leaf> {
    /// Represents a range of text in the base document. Includes beginning, excludes end.
    Copy(usize, usize), // note: for now, we lose open/closed info at interval endpoints
    Insert(Node<N, L>),
}

/// Represents changes to a document by describing the new document as a
/// sequence of sections copied from the old document and of new inserted
/// text. Deletions are represented by gaps in the ranges copied from the old
/// document.
///
/// For example, Editing "abcd" into "acde" could be represented as:
/// `[Copy(0,1),Copy(2,4),Insert("e")]`
#[derive(Clone)]
pub struct Delta<N: NodeInfo<L>, L: Leaf> {
    pub(crate) els: Vec<DeltaElement<N, L>>,
    pub(crate) base_len: usize,
}

/// A struct marking that a Delta contains only insertions. That is, it copies
/// all of the old document in the same order. It has a `Deref` impl so all
/// normal `Delta` methods can also be used on it.
#[derive(Clone)]
pub struct InsertDelta<N: NodeInfo<L>, L: Leaf>(Delta<N, L>);

impl<N: NodeInfo<L>, L: Leaf> Delta<N, L> {
    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    pub(crate) fn base_len(&self) -> usize {...}

    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    pub fn element_count(&self) -> usize {...}

    /// Exposes the raw delta elements for read-only iteration.
    pub fn elements(&self) -> &[DeltaElement<N, L>] {...}

    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    pub(crate) fn iter_elements(&self) -> ElementIter<'_, N, L> {...}

    #[allow(dead_code)]
    pub(crate) fn element_triples(&self) -> ElementTripleIter<'_, N, L> {...}

    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    pub(crate) fn from_element_vec(
        base_len: usize,
        elements: Vec<DeltaElement<N, L>>,
    ) -> Delta<N, L> {...}

    #[allow(dead_code)]
    pub(crate) fn from_element_tuples<I>(base_len: usize, elements: I) -> Delta<N, L>
    where
        I: IntoIterator<Item = DeltaElement<N, L>>,
    {...}

    pub fn simple_edit<T: IntervalBounds>(
        interval: T,
        rope: Node<N, L>,
        base_len: usize,
    ) -> Delta<N, L> {...}

    /// If this delta represents a simple insertion, returns the inserted node.
    pub fn as_simple_insert(&self) -> Option<&Node<N, L>> {...}

    /// Returns `true` if this delta represents a single deletion without
    /// any insertions.
    ///
    /// Note that this is `false` for the trivial delta, as well as for a deletion
    /// from an empty `Rope`.
    pub fn is_simple_delete(&self) -> bool {...}

    /// Returns `true` if applying the delta will cause no change.
    pub fn is_identity(&self) -> bool {...}

    /// Apply the delta to the given rope. May not work well if the length of the rope
    /// is not compatible with the construction of the delta.
    pub fn apply(&self, base: &Node<N, L>) -> Node<N, L> {...}

    /// Factor the delta into an insert-only delta and a subset representing deletions.
    /// Applying the insert then the delete yields the same result as the original delta:
    ///
    /// ```no_run
    /// # use xi_rope::rope::{Rope, RopeInfo};
    /// # use xi_rope::delta::Delta;
    /// # use std::str::FromStr;
    /// fn test_factor(d : &Delta<RopeInfo, String>, r : &Rope) {
    ///     let (ins, del) = d.clone().factor();
    ///     let del2 = del.transform_expand(&ins.inserted_subset());
    ///     assert_eq!(String::from(del2.delete_from(&ins.apply(r))), String::from(d.apply(r)));
    /// }
    /// ```
    pub fn factor(self) -> (InsertDelta<N, L>, Subset) {...}

    /// Synthesize a delta from a "union string" and two subsets: an old set
    /// of deletions and a new set of deletions from the union. The Delta is
    /// from text to text, not union to union; anything in both subsets will
    /// be assumed to be missing from the Delta base and the new text. You can
    /// also think of these as a set of insertions and one of deletions, with
    /// overlap doing nothing. This is basically the inverse of `factor`.
    ///
    /// Since only the deleted portions of the union string are necessary,
    /// instead of requiring a union string the function takes a `tombstones`
    /// rope which contains the deleted portions of the union string. The
    /// `from_dels` subset must be the interleaving of `tombstones` into the
    /// union string.
    ///
    /// ```no_run
    /// # use xi_rope::rope::{Rope, RopeInfo};
    /// # use xi_rope::delta::Delta;
    /// # use std::str::FromStr;
    /// fn test_synthesize(d : &Delta<RopeInfo, String>, r : &Rope) {
    ///     let (ins_d, del) = d.clone().factor();
    ///     let ins = ins_d.inserted_subset();
    ///     let del2 = del.transform_expand(&ins);
    ///     let r2 = ins_d.apply(&r);
    ///     let tombstones = ins.complement().delete_from(&r2);
    ///     let d2 = Delta::synthesize(&tombstones, &ins, &del);
    ///     assert_eq!(String::from(d2.apply(r)), String::from(d.apply(r)));
    /// }
    /// ```
    // For if last_old.is_some() && last_old.unwrap().0 <= beg {. Clippy complaints
    // about not using if-let, but that'd change the meaning of the conditional.
    #[allow(clippy::unnecessary_unwrap)]
    pub fn synthesize(
        tombstones: &Node<N, L>,
        from_dels: &Subset,
        to_dels: &Subset,
    ) -> Delta<N, L> {...}

    /// Produce a summary of the delta. Everything outside the returned interval
    /// is unchanged, and the old contents of the interval are replaced by new
    /// contents of the returned length. Equations:
    ///
    /// `(iv, new_len) = self.summary()`
    ///
    /// `new_s = self.apply(s)`
    ///
    /// `new_s = simple_edit(iv, new_s.subseq(iv.start(), iv.start() + new_len), s.len()).apply(s)`
    pub fn summary(&self) -> (Interval, usize) {...}

    /// Returns the length of the new document. In other words, the length of
    /// the transformed string after this Delta is applied.
    ///
    /// `d.apply(r).len() == d.new_document_len()`
    pub fn new_document_len(&self) -> usize {...}

    fn total_element_len(els: &[DeltaElement<N, L>]) -> usize {...}

    /// Returns the sum length of the inserts of the delta.
    pub fn inserts_len(&self) -> usize {...}

    /// Iterates over all the inserts of the delta.
    pub fn iter_inserts(&self) -> InsertsIter<'_, N, L> {...}

    /// Iterates over all the deletions of the delta.
    pub fn iter_deletions(&self) -> DeletionsIter<'_, N, L> {...}
}

impl<N: NodeInfo<L>, L: Leaf> fmt::Debug for Delta<N, L>
where
    Node<N, L>: fmt::Debug,
{
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {...}
}

impl<N: NodeInfo<L>, L: Leaf> fmt::Debug for InsertDelta<N, L>
where
    Node<N, L>: fmt::Debug,
{
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {...}
}

impl<N: NodeInfo<L>, L: Leaf> InsertDelta<N, L> {
    #![allow(clippy::many_single_char_names)]
    /// Do a coordinate transformation on an insert-only delta. The `after` parameter
    /// controls whether the insertions in `self` come after those specific in the
    /// coordinate transform.
    //
    // TODO: write accurate equations
    pub fn transform_expand(&self, xform: &Subset, after: bool) -> InsertDelta<N, L> {...}

    // TODO: it is plausible this method also works on Deltas with deletes
    /// Shrink a delta through a deletion of some of its copied regions with
    /// the same base. For example, if `self` applies to a union string, and
    /// `xform` is the deletions from that union, the resulting Delta will
    /// apply to the text.
    pub fn transform_shrink(&self, xform: &Subset) -> InsertDelta<N, L> {...}

    /// Return a Subset containing the inserted ranges.
    ///
    /// `d.inserted_subset().delete_from_string(d.apply_to_string(s)) == s`
    pub fn inserted_subset(&self) -> Subset {...}
}

/// An InsertDelta is a certain kind of Delta, and anything that applies to a
/// Delta that may include deletes also applies to one that definitely
/// doesn't. This impl allows implicit use of those methods.
impl<N: NodeInfo<L>, L: Leaf> Deref for InsertDelta<N, L> {
    type Target = Delta<N, L>;

    fn deref(&self) -> &Delta<N, L> {...}
}

// TODO: this doesn't need the new strings, so it should either be based on a new structure
/// A mapping from coordinates in the source sequence to coordinates in the sequence after
/// the delta is applied.
// like Delta but missing the strings, or perhaps the two subsets it's synthesized from.
pub struct Transformer<'a, N: NodeInfo<L> + 'a, L: Leaf> {
    delta: &'a Delta<N, L>,
}

impl<'a, N: NodeInfo<L> + 'a, L: Leaf> Transformer<'a, N, L> {
    /// Create a new transformer from a delta.
    pub fn new(delta: &'a Delta<N, L>) -> Self {...}

    // TODO: implement a cursor so we're not scanning from the beginning every time.
    /// Transform a single coordinate. The `after` parameter indicates whether it
    /// it should land before or after an inserted region.
    pub fn transform(&mut self, ix: usize, after: bool) -> usize {...}

    /// Determine whether a given interval is untouched by the transformation.
    pub fn interval_untouched<T: IntervalBounds>(&mut self, iv: T) -> bool {...}
}

/// A builder for creating new `Delta` objects.
///
/// Note that all edit operations must be sorted; the start point of each
/// interval must be no less than the end point of the previous one.
pub struct Builder<N: NodeInfo<L>, L: Leaf> {
    delta: Delta<N, L>,
    last_offset: usize,
}

impl<N: NodeInfo<L>, L: Leaf> Builder<N, L> {
    /// Creates a new builder, applicable to a base rope of length `base_len`.
    pub fn new(base_len: usize) -> Builder<N, L> {...}

    /// Deletes the given interval. Panics if interval is not properly sorted.
    pub fn delete<T: IntervalBounds>(&mut self, interval: T) {...}

    /// Replaces the given interval with the new rope. Panics if interval
    /// is not properly sorted.
    pub fn replace<T: IntervalBounds>(&mut self, interval: T, rope: Node<N, L>) {...}

    /// Determines if delta would be a no-op transformation if built.
    pub fn is_empty(&self) -> bool {...}

    /// Builds the `Delta`.
    pub fn build(mut self) -> Delta<N, L> {...}
}

pub struct InsertsIter<'a, N: NodeInfo<L> + 'a, L: Leaf> {
    pos: usize,
    last_end: usize,
    els_iter: slice::Iter<'a, DeltaElement<N, L>>,
}

#[derive(Debug, PartialEq)]
pub struct DeltaRegion {
    pub old_offset: usize,
    pub new_offset: usize,
    pub len: usize,
}

impl DeltaRegion {
    fn new(old_offset: usize, new_offset: usize, len: usize) -> Self {...}
}

impl<'a, N: NodeInfo<L>, L: Leaf> Iterator for InsertsIter<'a, N, L> {
    type Item = DeltaRegion;

    fn next(&mut self) -> Option<Self::Item> {...}
}

pub struct DeletionsIter<'a, N: NodeInfo<L> + 'a, L: Leaf> {
    pos: usize,
    last_end: usize,
    base_len: usize,
    els_iter: slice::Iter<'a, DeltaElement<N, L>>,
}

impl<'a, N: NodeInfo<L>, L: Leaf> Iterator for DeletionsIter<'a, N, L> {
    type Item = DeltaRegion;

    fn next(&mut self) -> Option<Self::Item> {...}
}

#[cfg_attr(not(feature = "serde"), allow(dead_code))]
pub(crate) struct ElementIter<'a, N: NodeInfo<L> + 'a, L: Leaf> {
    iter: slice::Iter<'a, DeltaElement<N, L>>,
}

impl<'a, N: NodeInfo<L>, L: Leaf> Iterator for ElementIter<'a, N, L> {
    type Item = &'a DeltaElement<N, L>;

    fn next(&mut self) -> Option<Self::Item> {...}

    fn size_hint(&self) -> (usize, Option<usize>) {...}
}

impl<'a, N: NodeInfo<L>, L: Leaf> ExactSizeIterator for ElementIter<'a, N, L> {}

#[allow(dead_code)]
pub(crate) struct ElementTripleIter<'a, N: NodeInfo<L> + 'a, L: Leaf> {
    iter: slice::Iter<'a, DeltaElement<N, L>>,
    new_offset: usize,
}

impl<'a, N: NodeInfo<L>, L: Leaf> Iterator for ElementTripleIter<'a, N, L> {
    type Item = (bool, usize, usize);

    fn next(&mut self) -> Option<Self::Item> {...}
}
```

## xi-editor-ph7/rust/rope/src/diff.rs

```rust
use std::borrow::Cow;
use std::collections::HashMap;

use crate::compare::RopeScanner;
use crate::delta::{Delta, DeltaElement};
use crate::interval::Interval;
use crate::rope::{LinesMetric, Rope, RopeDelta, RopeInfo};
use crate::tree::{Leaf, Node, NodeInfo};

/// A trait implemented by various diffing strategies.
pub trait Diff<N, L>
where
    N: NodeInfo<L>,
    L: Leaf,
{
    fn compute_delta(base: &Node<N, L>, target: &Node<N, L>) -> Delta<N, L>;
}

/// The minimum length of non-whitespace characters in a line before
/// we consider it for diffing purposes.
const MIN_SIZE: usize = 32;

/// A line-oriented, hash based diff algorithm.
///
/// This works by taking a hash of each line in either document that
/// has a length, ignoring leading whitespace, above some threshold.
///
/// Lines in the target document are matched against lines in the
/// base document. When a match is found, it is extended forwards
/// and backwards as far as possible.
///
/// This runs in O(n+m) in the lengths of the two ropes, and produces
/// results on a variety of workloads that are comparable in quality
/// (measured in terms of serialized diff size) with the results from
/// using a suffix array, while being an order of magnitude faster.
pub struct LineHashDiff;

impl Diff<RopeInfo, String> for LineHashDiff {
    fn compute_delta(base: &Rope, target: &Rope) -> RopeDelta {...}
}

/// Given two ropes and the offsets of two equal bytes, finds the largest
/// identical substring shared between the two ropes which contains the offset.
///
/// The return value is a pair of offsets, each of which represents an absolute
/// distance. That is to say, the position of the start and end boundaries
/// relative to the input offset.
fn expand_match(
    base: &Rope,
    target: &Rope,
    base_off: usize,
    targ_off: usize,
    prev_match_targ_end: usize,
) -> (usize, usize) {...}

/// Finds the longest increasing subset of copyable regions. This is essentially
/// the longest increasing subsequence problem. This implementation is adapted
/// from https://codereview.stackexchange.com/questions/187337/longest-increasing-subsequence-algorithm
fn longest_increasing_region_set(items: &[(usize, usize)]) -> Vec<(usize, usize)> {...}

#[inline]
fn non_ws_offset(s: &str) -> usize {...}

/// Represents copying `len` bytes from base to target.
#[derive(Debug, Clone, Copy)]
struct DiffOp {
    target_idx: usize,
    base_idx: usize,
    len: usize,
}

/// Keeps track of copy ops during diff construction.
#[derive(Debug, Clone, Default)]
pub struct DiffBuilder {
    ops: Vec<DiffOp>,
}

impl DiffBuilder {
    fn copy(&mut self, base: usize, target: usize, len: usize) {...}

    fn to_delta(self, base: &Rope, target: &Rope) -> RopeDelta {...}
}

/// Creates a map of lines to offsets, ignoring trailing whitespace, and only for those lines
/// where line.len() >= min_size. Offsets refer to the first non-whitespace byte in the line.
fn make_line_hashes(base: &Rope, min_size: usize) -> HashMap<Cow<'_, str>, usize> {...}
```

## xi-editor-ph7/rust/rope/src/engine.rs

```rust
use std::borrow::Cow;
use std::collections::hash_map::DefaultHasher;
use std::collections::BTreeSet;

use crate::delta::{Delta, InsertDelta};
use crate::interval::Interval;
use crate::multiset::{CountMatcher, Subset};
use crate::rope::{Rope, RopeInfo};

/// Represents the current state of a document and all of its history
#[derive(Debug)]
pub struct Engine {
    /// The session ID used to create new `RevId`s for edits made on this device
    session: SessionId,
    /// The incrementing revision number counter for this session used for `RevId`s
    rev_id_counter: u32,
    /// The current contents of the document as would be displayed on screen
    text: Rope,
    /// Storage for all the characters that have been deleted  but could
    /// return if a delete is un-done or an insert is re- done.
    tombstones: Rope,
    /// Imagine a "union string" that contained all the characters ever
    /// inserted, including the ones that were later deleted, in the locations
    /// they would be if they hadn't been deleted.
    ///
    /// This is a `Subset` of the "union string" representing the characters
    /// that are currently deleted, and thus in `tombstones` rather than
    /// `text`. The count of a character in `deletes_from_union` represents
    /// how many times it has been deleted, so if a character is deleted twice
    /// concurrently it will have count `2` so that undoing one delete but not
    /// the other doesn't make it re-appear.
    ///
    /// You could construct the "union string" from `text`, `tombstones` and
    /// `deletes_from_union` by splicing a segment of `tombstones` into `text`
    /// wherever there's a non-zero-count segment in `deletes_from_union`.
    deletes_from_union: Subset,
    // TODO: switch to a persistent Set representation to avoid O(n) copying
    undone_groups: BTreeSet<usize>, // set of undo_group id's
    /// The revision history of the document
    revs: Vec<Revision>,
}

// The advantage of using a session ID over random numbers is that it can be
// easily delta-compressed later.
#[derive(Debug, Clone, Copy, PartialOrd, Ord, PartialEq, Eq, Hash)]
pub struct RevId {
    // 96 bits has a 10^(-12) chance of collision with 400 million sessions and 10^(-6) with 100 billion.
    // `session1==session2==0` is reserved for initialization which is the same on all sessions.
    // A colliding session will break merge invariants and the document will start crashing Xi.
    session1: u64,
    // if this was a tuple field instead of two fields, alignment padding would add 8 more bytes.
    session2: u32,
    // There will probably never be a document with more than 4 billion edits
    // in a single session.
    num: u32,
}

#[derive(Debug)]
struct Revision {
    /// This uniquely represents the identity of this revision and it stays
    /// the same even if it is rebased or merged between devices.
    rev_id: RevId,
    /// The largest undo group number of any edit in the history up to this
    /// point. Used to optimize undo to not look further back.
    max_undo_so_far: usize,
    edit: Contents,
}

/// Valid within a session. If there's a collision the most recent matching
/// Revision will be used, which means only the (small) set of concurrent edits
/// could trigger incorrect behavior if they collide, so u64 is safe.
pub type RevToken = u64;

/// the session ID component of a `RevId`
pub type SessionId = (u64, u32);

/// Type for errors that occur during CRDT operations.
#[derive(Clone)]
pub enum Error {
    /// An edit specified a revision that did not exist. The revision may
    /// have been GC'd, or it may have specified incorrectly.
    MissingRevision(RevToken),
    /// A delta was applied which had a `base_len` that did not match the length
    /// of the revision it was applied to.
    MalformedDelta { rev_len: usize, delta_len: usize },
}

#[derive(Clone, Copy, PartialOrd, Ord, PartialEq, Eq)]
struct FullPriority {
    priority: usize,
    session_id: SessionId,
}

use self::Contents::*;

#[derive(Debug, Clone)]
enum Contents {
    Edit {
        /// Used to order concurrent inserts, for example auto-indentation
        /// should go before typed text.
        priority: usize,
        /// Groups related edits together so that they are undone and re-done
        /// together. For example, an auto-indent insertion would be un-done
        /// along with the newline that triggered it.
        undo_group: usize,
        /// The subset of the characters of the union string from after this
        /// revision that were added by this revision.
        inserts: Subset,
        /// The subset of the characters of the union string from after this
        /// revision that were deleted by this revision.
        deletes: Subset,
    },
    Undo {
        /// The set of groups toggled between undone and done.
        /// Just the `symmetric_difference` (XOR) of the two sets.
        toggled_groups: BTreeSet<usize>, // set of undo_group id's
        /// Used to store a reversible difference between the old
        /// and new deletes_from_union
        deletes_bitxor: Subset,
    },
}

/// Lightweight read-only view over a revision, used by serde and future cross-language bindings.
#[cfg_attr(not(feature = "serde"), allow(dead_code))]
pub(crate) struct RevisionRef<'a> {
    pub(crate) rev_id: RevId,
    pub(crate) max_undo_so_far: usize,
    pub(crate) contents: RevisionContentsRef<'a>,
}

/// Borrowed revision payload details exposed for serialization helpers.
#[cfg_attr(not(feature = "serde"), allow(dead_code))]
pub(crate) enum RevisionContentsRef<'a> {
    Edit(EditContentsRef<'a>),
    Undo(UndoContentsRef<'a>),
}

/// Borrowed view of an edit revision's contents.
#[cfg_attr(not(feature = "serde"), allow(dead_code))]
pub(crate) struct EditContentsRef<'a> {
    pub(crate) priority: usize,
    pub(crate) undo_group: usize,
    pub(crate) inserts: &'a Subset,
    pub(crate) deletes: &'a Subset,
}

/// Borrowed view of an undo revision's contents.
#[cfg_attr(not(feature = "serde"), allow(dead_code))]
pub(crate) struct UndoContentsRef<'a> {
    pub(crate) toggled_groups: &'a BTreeSet<usize>,
    pub(crate) deletes_bitxor: &'a Subset,
}

/// Owned revision payload produced during deserialization.
#[cfg_attr(not(feature = "serde"), allow(dead_code))]
pub(crate) struct RevisionOwned {
    pub(crate) rev_id: RevId,
    pub(crate) max_undo_so_far: usize,
    pub(crate) contents: RevisionContentsOwned,
}

/// Owned representation of revision contents used to rebuild internal state.
#[cfg_attr(not(feature = "serde"), allow(dead_code))]
pub(crate) enum RevisionContentsOwned {
    Edit(EditContentsOwned),
    Undo(UndoContentsOwned),
}

/// Owned edit payload backing `RevisionContentsOwned::Edit`.
#[cfg_attr(not(feature = "serde"), allow(dead_code))]
pub(crate) struct EditContentsOwned {
    pub(crate) priority: usize,
    pub(crate) undo_group: usize,
    pub(crate) inserts: Subset,
    pub(crate) deletes: Subset,
}

/// Owned undo payload backing `RevisionContentsOwned::Undo`.
#[cfg_attr(not(feature = "serde"), allow(dead_code))]
pub(crate) struct UndoContentsOwned {
    pub(crate) toggled_groups: BTreeSet<usize>,
    pub(crate) deletes_bitxor: Subset,
}

/// Iterator over revisions that yields borrowed helper views for serialization.
#[cfg_attr(not(feature = "serde"), allow(dead_code))]
pub(crate) struct RevisionLogIter<'a> {
    inner: std::slice::Iter<'a, Revision>,
}

impl<'a> Iterator for RevisionLogIter<'a> {
    type Item = RevisionRef<'a>;

    fn next(&mut self) -> Option<Self::Item> {...}
}

/// for single user cases, used by serde and ::empty
fn default_session() -> (u64, u32) {...}

/// Revision 0 is always an Undo of the empty set of groups
fn initial_revision_counter() -> u32 {...}

impl RevId {
    /// Returns a u64 that will be equal for equivalent revision IDs and
    /// should be as unlikely to collide as two random u64s.
    pub fn token(&self) -> RevToken {...}

    pub fn session_id(&self) -> SessionId {...}

    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    pub(crate) fn raw_parts(&self) -> (u64, u32, u32) {...}

    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    pub(crate) fn from_raw_parts(session1: u64, session2: u32, num: u32) -> RevId {...}
}

impl Revision {
    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    pub(crate) fn as_ref(&self) -> RevisionRef<'_> {...}

    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    pub(crate) fn from_owned(owned: RevisionOwned) -> Revision {...}
}

impl Contents {
    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    fn as_ref(&self) -> RevisionContentsRef<'_> {...}

    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    fn from_owned(owned: RevisionContentsOwned) -> Contents {...}
}

impl RevisionOwned {
    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    pub(crate) fn new(
        rev_id: RevId,
        max_undo_so_far: usize,
        contents: RevisionContentsOwned,
    ) -> Self {...}
}

impl Engine {
    /// Create a new Engine with a single edit that inserts `initial_contents`
    /// if it is non-empty. It needs to be a separate commit rather than just
    /// part of the initial contents since any two `Engine`s need a common
    /// ancestor in order to be mergeable.
    pub fn new(initial_contents: Rope) -> Engine {...}

    pub fn empty() -> Engine {...}

    fn next_rev_id(&self) -> RevId {...}

    fn find_rev(&self, rev_id: RevId) -> Option<usize> {...}

    fn find_rev_token(&self, rev_token: RevToken) -> Option<usize> {...}

    // TODO: does Cow really help much here? It certainly won't after making Subsets a rope.
    /// Find what the `deletes_from_union` field in Engine would have been at the time
    /// of a certain `rev_index`. In other words, the deletes from the union string at that time.
    fn deletes_from_union_for_index(&self, rev_index: usize) -> Cow<'_, Subset> {...}

    /// Garbage collection means undo can sometimes need to replay the very first
    /// revision, and so needs a way to get the deletion set before then.
    fn deletes_from_union_before_index(
        &self,
        rev_index: usize,
        invert_undos: bool,
    ) -> Cow<'_, Subset> {...}

    /// Get the contents of the document at a given revision number
    fn rev_content_for_index(&self, rev_index: usize) -> Rope {...}

    /// Get the Subset to delete from the current union string in order to obtain a revision's content
    fn deletes_from_cur_union_for_index(&self, rev_index: usize) -> Cow<'_, Subset> {...}

    /// Returns the largest undo group ID used so far
    pub fn max_undo_group_id(&self) -> usize {...}

    /// Get revision id of head revision.
    pub fn get_head_rev_id(&self) -> RevId {...}

    /// Get text of head revision.
    pub fn get_head(&self) -> &Rope {...}

    /// Get text of a given revision, if it can be found.
    pub fn get_rev(&self, rev: RevToken) -> Option<Rope> {...}

    /// A delta that, when applied to `base_rev`, results in the current head. Returns
    /// an error if there is not at least one edit.
    pub fn try_delta_rev_head(&self, base_rev: RevToken) -> Result<Delta<RopeInfo, String>, Error> {...}

    // TODO: don't construct transform if subsets are empty
    // TODO: maybe switch to using a revision index for `base_rev` once we disable GC
    /// Returns a tuple of a new `Revision` representing the edit based on the
    /// current head, a new text `Rope`, a new tombstones `Rope` and a new `deletes_from_union`.
    /// Returns an [`Error`] if `base_rev` cannot be found, or `delta.base_len`
    /// does not equal the length of the text at `base_rev`.
    fn mk_new_rev(
        &self,
        new_priority: usize,
        undo_group: usize,
        base_rev: RevToken,
        delta: Delta<RopeInfo, String>,
    ) -> Result<(Revision, Rope, Rope, Subset), Error> {...}
    // NOTE: maybe just deprecate this? we can panic on the other side of
    // the call if/when that makes sense.
    /// Create a new edit based on `base_rev`.
    ///
    /// # Panics
    ///
    /// Panics if `base_rev` does not exist, or if `delta` is poorly formed.
    pub fn edit_rev(
        &mut self,
        priority: usize,
        undo_group: usize,
        base_rev: RevToken,
        delta: Delta<RopeInfo, String>,
    ) {...}

    // TODO: have `base_rev` be an index so that it can be used maximally
    // efficiently with the head revision, a token or a revision ID.
    // Efficiency loss of token is negligible but unfortunate.
    /// Attempts to apply a new edit based on the [`Revision`] specified by `base_rev`,
    /// Returning an [`Error`] if the `Revision` cannot be found.
    pub fn try_edit_rev(
        &mut self,
        priority: usize,
        undo_group: usize,
        base_rev: RevToken,
        delta: Delta<RopeInfo, String>,
    ) -> Result<(), Error> {...}

    // since undo and gc replay history with transforms, we need an empty set
    // of the union string length *before* the first revision.
    fn empty_subset_before_first_rev(&self) -> Subset {...}

    /// Find the first revision that could be affected by toggling a set of undo groups
    fn find_first_undo_candidate_index(&self, toggled_groups: &BTreeSet<usize>) -> usize {...}

    // This computes undo all the way from the beginning. An optimization would be to not
    // recompute the prefix up to where the history diverges, but it's not clear that's
    // even worth the code complexity.
    fn compute_undo(&self, groups: &BTreeSet<usize>) -> (Revision, Subset) {...}

    // TODO: maybe refactor this API to take a toggle set
    pub fn undo(&mut self, groups: BTreeSet<usize>) {...}

    pub fn is_equivalent_revision(&self, base_rev: RevId, other_rev: RevId) -> bool {...}

    // Note: this function would need some work to handle retaining arbitrary revisions,
    // partly because the reachability calculation would become more complicated (a
    // revision might hold content from an undo group that would otherwise be gc'ed),
    // and partly because you need to retain more undo history, to supply input to the
    // reachability calculation.
    //
    // Thus, it's easiest to defer gc to when all plugins quiesce, but it's certainly
    // possible to fix it so that's not necessary.
    pub fn gc(&mut self, gc_groups: &BTreeSet<usize>) {...}

    /// Merge the new content from another Engine into this one with a CRDT merge
    pub fn merge(&mut self, other: &Engine) {...}

    /// When merging between multiple concurrently-editing sessions, each session should have a unique ID
    /// set with this function, which will make the revisions they create not have colliding IDs.
    /// For safety, this will panic if any revisions have already been added to the Engine.
    ///
    /// Merge may panic or return incorrect results if session IDs collide, which is why they can be
    /// 96 bits which is more than sufficient for this to never happen.
    pub fn set_session_id(&mut self, session: SessionId) {...}
}

impl Engine {
    /// Exposes the session tuple used when generating new `RevId`s.
    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    pub(crate) fn session_components(&self) -> SessionId {...}

    /// Current revision counter associated with this engine's session.
    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    pub(crate) fn revision_counter(&self) -> u32 {...}

    /// Returns an iterator over revisions for serialization or diagnostics.
    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    pub(crate) fn revision_log(&self) -> RevisionLogIter<'_> {...}

    /// Provides a borrowed view of the head text.
    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    pub(crate) fn text_snapshot(&self) -> &Rope {...}

    /// Provides a borrowed view of the tombstones rope.
    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    pub(crate) fn tombstones_snapshot(&self) -> &Rope {...}

    /// Provides a borrowed view of the deletion subset from the union string.
    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    pub(crate) fn deletes_from_union_snapshot(&self) -> &Subset {...}

    /// Provides a borrowed view of undo group state.
    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    pub(crate) fn undone_groups_snapshot(&self) -> &BTreeSet<usize> {...}

    /// Recreates an engine from serialized components.
    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    pub(crate) fn from_serialized_state(
        session: SessionId,
        rev_id_counter: u32,
        text: Rope,
        tombstones: Rope,
        deletes_from_union: Subset,
        undone_groups: BTreeSet<usize>,
        revs: Vec<RevisionOwned>,
    ) -> Engine {...}
}

// ======== Generic helpers

/// Move sections from text to tombstones and out of tombstones based on a new and old set of deletions
fn shuffle_tombstones(
    text: &Rope,
    tombstones: &Rope,
    old_deletes_from_union: &Subset,
    new_deletes_from_union: &Subset,
) -> Rope {...}

/// Move sections from text to tombstones and vice versa based on a new and old set of deletions.
/// Returns a tuple of a new text `Rope` and a new `Tombstones` rope described by `new_deletes_from_union`.
fn shuffle(
    text: &Rope,
    tombstones: &Rope,
    old_deletes_from_union: &Subset,
    new_deletes_from_union: &Subset,
) -> (Rope, Rope) {...}

// ======== Merge helpers

/// Find an index before which everything is the same
fn find_base_index(a: &[Revision], b: &[Revision]) -> usize {...}

/// Find a set of revisions common to both lists
fn find_common(a: &[Revision], b: &[Revision]) -> BTreeSet<RevId> {...}

/// Returns the operations in `revs` that don't have their `rev_id` in
/// `base_revs`, but modified so that they are in the same order but based on
/// the `base_revs`. This allows the rest of the merge to operate on only
/// revisions not shared by both sides.
///
/// Conceptually, see the diagram below, with `.` being base revs and `n` being
/// non-base revs, `N` being transformed non-base revs, and rearranges it:
/// .n..n...nn..  -> ........NNNN -> returns vec![N,N,N,N]
fn rearrange(revs: &[Revision], base_revs: &BTreeSet<RevId>, head_len: usize) -> Vec<Revision> {...}

#[derive(Clone, Debug)]
struct DeltaOp {
    rev_id: RevId,
    priority: usize,
    undo_group: usize,
    inserts: InsertDelta<RopeInfo, String>,
    deletes: Subset,
}

/// Transform `revs`, which doesn't include information on the actual content of the operations,
/// into an `InsertDelta`-based representation that does by working backward from the text and tombstones.
fn compute_deltas(
    revs: &[Revision],
    text: &Rope,
    tombstones: &Rope,
    deletes_from_union: &Subset,
) -> Vec<DeltaOp> {...}

/// Computes a series of priorities and transforms for the deltas on the right
/// from the new revisions on the left.
///
/// Applies an optimization where it combines sequential revisions with the
/// same priority into one transform to decrease the number of transforms that
/// have to be considered in `rebase` substantially for normal editing
/// patterns. Any large runs of typing in the same place by the same user (e.g
/// typing a paragraph) will be combined into a single segment in a transform
/// as opposed to thousands of revisions.
fn compute_transforms(revs: Vec<Revision>) -> Vec<(FullPriority, Subset)> {...}

/// Rebase `b_new` on top of `expand_by` and return revision contents that can be appended as new
/// revisions on top of the revisions represented by `expand_by`.
fn rebase(
    mut expand_by: Vec<(FullPriority, Subset)>,
    b_new: Vec<DeltaOp>,
    mut text: Rope,
    mut tombstones: Rope,
    mut deletes_from_union: Subset,
    mut max_undo_so_far: usize,
) -> (Vec<Revision>, Rope, Rope, Subset) {...}

impl std::fmt::Display for Error {
    fn fmt(&self, f: &mut std::fmt::Formatter) -> std::fmt::Result {...}
}

impl std::fmt::Debug for Error {
    fn fmt(&self, f: &mut std::fmt::Formatter) -> std::fmt::Result {...}
}

impl std::error::Error for Error {}

#[cfg(feature = "serde")]
mod serde_impl;
```

## xi-editor-ph7/rust/rope/src/engine/serde_impl.rs

```rust
use serde::{Deserialize, Deserializer, Serialize, Serializer};
use std::collections::BTreeSet;

use super::{
    default_session, initial_revision_counter, Contents, EditContentsOwned, EditContentsRef,
    Engine, RevId, Revision, RevisionContentsOwned, RevisionContentsRef, RevisionOwned,
    RevisionRef, SessionId, UndoContentsOwned, UndoContentsRef,
};
use crate::multiset::Subset;
use crate::rope::Rope;

#[derive(Serialize)]
struct EngineSerialize<'a> {
    text: &'a Rope,
    tombstones: &'a Rope,
    deletes_from_union: &'a Subset,
    undone_groups: &'a BTreeSet<usize>,
    revs: Vec<RevisionSerialize<'a>>,
}

impl<'a> From<&'a Engine> for EngineSerialize<'a> {
    fn from(engine: &'a Engine) -> Self {...}
}

#[derive(Serialize)]
struct RevisionSerialize<'a> {
    rev_id: RevId,
    max_undo_so_far: usize,
    edit: RevisionContentsSerialize<'a>,
}

#[derive(Serialize)]
enum RevisionContentsSerialize<'a> {
    Edit {
        priority: usize,
        undo_group: usize,
        #[serde(borrow)]
        inserts: &'a Subset,
        #[serde(borrow)]
        deletes: &'a Subset,
    },
    Undo {
        #[serde(borrow)]
        toggled_groups: &'a BTreeSet<usize>,
        #[serde(borrow)]
        deletes_bitxor: &'a Subset,
    },
}

impl<'a> From<RevisionRef<'a>> for RevisionSerialize<'a> {
    fn from(revision: RevisionRef<'a>) -> Self {...}
}

impl<'a> From<RevisionContentsRef<'a>> for RevisionContentsSerialize<'a> {
    fn from(contents: RevisionContentsRef<'a>) -> Self {...}
}

#[derive(Deserialize)]
struct EngineDeserialize {
    #[serde(default = "default_session")]
    session: SessionId,
    #[serde(default = "initial_revision_counter")]
    rev_id_counter: u32,
    text: Rope,
    tombstones: Rope,
    deletes_from_union: Subset,
    undone_groups: BTreeSet<usize>,
    revs: Vec<RevisionDeserialize>,
}

#[derive(Deserialize)]
struct RevisionDeserialize {
    rev_id: RevId,
    max_undo_so_far: usize,
    edit: RevisionContentsDeserialize,
}

#[derive(Deserialize)]
enum RevisionContentsDeserialize {
    Edit { priority: usize, undo_group: usize, inserts: Subset, deletes: Subset },
    Undo { toggled_groups: BTreeSet<usize>, deletes_bitxor: Subset },
}

impl From<RevisionContentsDeserialize> for RevisionContentsOwned {
    fn from(contents: RevisionContentsDeserialize) -> Self {...}
}

impl From<RevisionDeserialize> for RevisionOwned {
    fn from(revision: RevisionDeserialize) -> Self {...}
}

#[derive(Serialize, Deserialize)]
struct RevIdParts {
    session1: u64,
    session2: u32,
    num: u32,
}

impl Serialize for RevId {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {...}
}

impl<'de> Deserialize<'de> for RevId {
    fn deserialize<D>(deserializer: D) -> Result<Self, D::Error>
    where
        D: Deserializer<'de>,
    {...}
}

impl Serialize for Revision {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {...}
}

impl<'de> Deserialize<'de> for Revision {
    fn deserialize<D>(deserializer: D) -> Result<Self, D::Error>
    where
        D: Deserializer<'de>,
    {...}
}

impl Serialize for Contents {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {...}
}

impl<'de> Deserialize<'de> for Contents {
    fn deserialize<D>(deserializer: D) -> Result<Self, D::Error>
    where
        D: Deserializer<'de>,
    {...}
}

impl Serialize for Engine {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {...}
}

impl<'de> Deserialize<'de> for Engine {
    fn deserialize<D>(deserializer: D) -> Result<Self, D::Error>
    where
        D: Deserializer<'de>,
    {...}
}
```

## xi-editor-ph7/rust/rope/src/find.rs

```rust
use std::cmp::min;

use memchr::{memchr, memchr2, memchr3};

use crate::rope::BaseMetric;
use crate::rope::LinesRaw;
use crate::rope::RopeInfo;
use crate::tree::Cursor;
use regex::Regex;
use std::borrow::Cow;
use std::str;

/// The result of a [`find`][find] operation.
///
/// [find]: fn.find.html
pub enum FindResult {
    /// The pattern was found at this position.
    Found(usize),
    /// The pattern was not found.
    NotFound,
    /// The cursor has been advanced by some amount. The pattern is not
    /// found before the new cursor, but may be at or beyond it.
    TryAgain,
}

/// A policy for case matching. There may be more choices in the future (for
/// example, an even more forgiving mode that ignores accents, or possibly
/// handling Unicode normalization).
#[derive(Clone, Copy, PartialEq)]
pub enum CaseMatching {
    /// Require an exact codepoint-for-codepoint match (implies case sensitivity).
    Exact,
    /// Case insensitive match. Guaranteed to work for the ASCII case, and
    /// reasonably well otherwise (it is currently defined in terms of the
    /// `to_lowercase` methods in the Rust standard library).
    CaseInsensitive,
}

/// Finds a pattern string in the rope referenced by the cursor, starting at
/// the current location of the cursor (and finding the first match). Both
/// case sensitive and case insensitive matching is provided, controlled by
/// the `cm` parameter. The `regex` parameter controls whether the query
/// should be considered as a regular expression.
///
/// On success, the cursor is updated to immediately follow the found string.
/// On failure, the cursor's position is indeterminate.
///
/// Can panic if `pat` is empty.
pub fn find(
    cursor: &mut Cursor<RopeInfo, String>,
    lines: &mut LinesRaw,
    cm: CaseMatching,
    pat: &str,
    regex: Option<&Regex>,
) -> Option<usize> {...}

/// A variant of [`find`][find] that makes a bounded amount of progress, then either
/// returns or suspends (returning `TryAgain`).
///
/// The `num_steps` parameter controls the number of "steps" processed per
/// call. The unit of "step" is not formally defined but is typically
/// scanning one leaf (using a memchr-like scan) or testing one candidate
/// when scanning produces a result. It should be empirically tuned for a
/// balance between overhead and impact on interactive performance, but the
/// exact value is probably not critical.
///
/// [find]: fn.find.html
pub fn find_progress(
    cursor: &mut Cursor<RopeInfo, String>,
    lines: &mut LinesRaw,
    cm: CaseMatching,
    pat: &str,
    num_steps: usize,
    regex: Option<&Regex>,
) -> FindResult {...}

// Run the core repeatedly until there is a result, up to a certain number of steps.
fn find_progress_iter(
    cursor: &mut Cursor<RopeInfo, String>,
    lines: &mut LinesRaw,
    pat: &str,
    scanner: impl Fn(&str) -> Option<usize>,
    matcher: impl Fn(&mut Cursor<RopeInfo, String>, &mut LinesRaw, &str) -> Option<usize>,
    num_steps: usize,
) -> FindResult {...}

// The core of the find algorithm. It takes a "scanner", which quickly
// scans through a single leaf searching for some prefix of the pattern,
// then a "matcher" which confirms that such a candidate actually matches
// in the full rope.
fn find_core(
    cursor: &mut Cursor<RopeInfo, String>,
    lines: &mut LinesRaw,
    pat: &str,
    scanner: impl Fn(&str) -> Option<usize>,
    matcher: impl Fn(&mut Cursor<RopeInfo, String>, &mut LinesRaw, &str) -> Option<usize>,
) -> FindResult {...}

/// Compare whether the substring beginning at the current cursor location
/// is equal to the provided string. Leaves the cursor at an indeterminate
/// position on failure, but the end of the string on success. Returns the
/// start position of the match.
pub fn compare_cursor_str(
    cursor: &mut Cursor<RopeInfo, String>,
    _lines: &mut LinesRaw,
    mut pat: &str,
) -> Option<usize> {...}

/// Like `compare_cursor_str` but case invariant (using to_lowercase() to
/// normalize both strings before comparison). Returns the start position
/// of the match.
pub fn compare_cursor_str_casei(
    cursor: &mut Cursor<RopeInfo, String>,
    _lines: &mut LinesRaw,
    pat: &str,
) -> Option<usize> {...}

/// Compare whether the substring beginning at the cursor location matches
/// the provided regular expression. The substring begins at the beginning
/// of the start of the line.
/// If the regular expression can match multiple lines then the entire text
/// is consumed and matched against the regular expression. Otherwise only
/// the current line is matched. Returns the start position of the match.
pub fn compare_cursor_regex(
    cursor: &mut Cursor<RopeInfo, String>,
    lines: &mut LinesRaw,
    pat: &str,
    regex: &Regex,
) -> Option<usize> {...}

/// Checks if a regular expression can match multiple lines.
pub fn is_multiline_regex(regex: &str) -> bool {...}

/// Scan for a codepoint that, after conversion to lowercase, matches the probe.
fn scan_lowercase(probe: char, s: &str) -> Option<usize> {...}
```

## xi-editor-ph7/rust/rope/src/helpers/mod.rs

```rust
pub(crate) mod string_leaf;
```

## xi-editor-ph7/rust/rope/src/helpers/string_leaf.rs

```rust
use std::cmp::{max, min};

use memchr::memrchr;

use crate::metrics::count_utf16_code_units_bytes;

pub(crate) const MIN_LEAF: usize = 511;
pub(crate) const MAX_LEAF: usize = 1024;
pub(crate) const NEWLINE_WINDOW: usize = MAX_LEAF - MIN_LEAF;

pub(crate) fn count_utf16_code_units(s: &str) -> usize {...}

pub(crate) fn find_leaf_split_for_bulk(s: &str) -> usize {...}

pub(crate) fn find_leaf_split_for_merge(s: &str) -> usize {...}

pub(crate) fn find_leaf_split(s: &str, minsplit: usize) -> usize {...}
```

## xi-editor-ph7/rust/rope/src/interval.rs

```rust
use std::cmp::{max, min};
use std::fmt;
use std::ops::{Range, RangeFrom, RangeFull, RangeInclusive, RangeTo, RangeToInclusive};

/// A fancy version of Range<usize>, representing a closed-open range;
/// the interval [5, 7) is the set {5, 6}.
///
/// It is an invariant that `start <= end`. An interval where `end < start` is
/// considered empty.
#[derive(Clone, Copy, PartialEq, Eq)]
pub struct Interval {
    pub start: usize,
    pub end: usize,
}

impl Interval {
    /// Construct a new `Interval` representing the range [start..end).
    /// It is an invariant that `start <= end`.
    pub fn new(start: usize, end: usize) -> Interval {...}

    #[deprecated(since = "0.3.0", note = "all intervals are now closed_open, use Interval::new")]
    pub fn new_closed_open(start: usize, end: usize) -> Interval {...}

    #[deprecated(since = "0.3.0", note = "all intervals are now closed_open")]
    pub fn new_open_closed(start: usize, end: usize) -> Interval {...}

    #[deprecated(since = "0.3.0", note = "all intervals are now closed_open")]
    pub fn new_closed_closed(start: usize, end: usize) -> Interval {...}

    #[deprecated(since = "0.3.0", note = "all intervals are now closed_open")]
    pub fn new_open_open(start: usize, end: usize) -> Interval {...}

    pub fn start(&self) -> usize {...}

    pub fn end(&self) -> usize {...}

    pub fn start_end(&self) -> (usize, usize) {...}

    // The following 3 methods define a trisection, exactly one is true.
    // (similar to std::cmp::Ordering, but "Equal" is not the same as "contains")

    /// the interval is before the point (the point is after the interval)
    pub fn is_before(&self, val: usize) -> bool {...}

    /// the point is inside the interval
    pub fn contains(&self, val: usize) -> bool {...}

    /// the interval is after the point (the point is before the interval)
    pub fn is_after(&self, val: usize) -> bool {...}

    pub fn is_empty(&self) -> bool {...}

    // impl BitAnd would be completely valid for this
    pub fn intersect(&self, other: Interval) -> Interval {...}

    // smallest interval that encloses both inputs; if the inputs are
    // disjoint, then it fills in the hole.
    pub fn union(&self, other: Interval) -> Interval {...}

    // the first half of self - other
    pub fn prefix(&self, other: Interval) -> Interval {...}

    // the second half of self - other
    pub fn suffix(&self, other: Interval) -> Interval {...}

    // could impl Add trait, but that's probably too cute
    pub fn translate(&self, amount: usize) -> Interval {...}

    // as above for Sub trait
    pub fn translate_neg(&self, amount: usize) -> Interval {...}

    // insensitive to open or closed ends, just the size of the interior
    pub fn size(&self) -> usize {...}
}

impl fmt::Display for Interval {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {...}
}

impl fmt::Debug for Interval {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {...}
}

impl From<Range<usize>> for Interval {
    fn from(src: Range<usize>) -> Interval {...}
}

impl From<RangeTo<usize>> for Interval {
    fn from(src: RangeTo<usize>) -> Interval {...}
}

impl From<RangeInclusive<usize>> for Interval {
    fn from(src: RangeInclusive<usize>) -> Interval {...}
}

impl From<RangeToInclusive<usize>> for Interval {
    fn from(src: RangeToInclusive<usize>) -> Interval {...}
}

/// A trait for types that represent unbounded ranges; they need an explicit
/// upper bound in order to be converted to `Interval`s.
///
/// This exists so that some methods that use `Interval` under the hood can
/// accept arguments like `..` or `10..`.
///
/// This trait should only be used when the idea of taking all of something
/// makes sense.
pub trait IntervalBounds {
    fn into_interval(self, upper_bound: usize) -> Interval;
}

impl<T: Into<Interval>> IntervalBounds for T {
    fn into_interval(self, _upper_bound: usize) -> Interval {...}
}

impl IntervalBounds for RangeFrom<usize> {
    fn into_interval(self, upper_bound: usize) -> Interval {...}
}

impl IntervalBounds for RangeFull {
    fn into_interval(self, upper_bound: usize) -> Interval {...}
}
```

## xi-editor-ph7/rust/rope/src/lib.rs

```rust
#![allow(
    clippy::collapsible_if,
    clippy::len_without_is_empty,
    clippy::many_single_char_names,
    clippy::needless_range_loop,
    clippy::new_without_default,
    clippy::should_implement_trait,
    clippy::wrong_self_convention
)]

extern crate bytecount;
extern crate memchr;
extern crate regex;
extern crate unicode_segmentation;

#[cfg(feature = "serde")]
extern crate serde;


pub mod breaks;
pub mod compare;
pub mod delta;
pub mod diff;
pub mod engine;
pub mod find;
pub(crate) mod helpers;
pub mod interval;
pub(crate) mod metrics;
pub mod multiset;
pub mod rope;
#[cfg(feature = "serde")]
pub mod serde_fixtures;
#[cfg(feature = "serde")]
mod serde_impls;
pub mod spans;
pub mod tree;

pub use crate::delta::{Builder as DeltaBuilder, Delta, DeltaElement, Transformer};
pub use crate::interval::Interval;
pub use crate::rope::{LinesMetric, Rope, RopeDelta, RopeInfo};
#[cfg(feature = "cursor_state")]
pub use crate::tree::CursorState;
pub use crate::tree::{Cursor, CursorDescriptor, Metric};
#[cfg(feature = "tree_builder_slice_trace")]
pub use crate::tree::{
    NullTreeBuilderTracer, TreeBuilderEvent, TreeBuilderEventKind, TreeBuilderTracer,
};
```

## xi-editor-ph7/rust/rope/src/metrics/break_indices.rs

```rust
#[inline]
pub(crate) fn nth_break_offset(data: &[usize], leaf_len: usize, in_measured_units: usize) -> usize {...}

#[inline]
pub(crate) fn count_breaks_up_to(data: &[usize], offset: usize) -> usize {...}

#[inline]
pub(crate) fn find_prev_break(data: &[usize], offset: usize) -> Option<usize> {...}

#[inline]
pub(crate) fn find_next_break(data: &[usize], offset: usize) -> Option<usize> {...}

#[inline]
pub(crate) fn is_break_boundary(data: &[usize], offset: usize) -> bool {...}
```

## xi-editor-ph7/rust/rope/src/metrics/codepoint.rs

```rust
use std::cmp;

const CONT_MASK: u8 = 0b1100_0000;
const CONT_TAG: u8 = 0b1000_0000;

#[inline(always)]
fn is_continuation(byte: u8) -> bool {...}

#[inline(always)]
pub(crate) fn len_utf8_from_first_byte(b: u8) -> usize {...}

#[inline]
pub(crate) fn is_codepoint_boundary(bytes: &[u8], offset: usize) -> bool {...}

#[inline]
pub(crate) fn prev_codepoint_boundary(bytes: &[u8], offset: usize) -> Option<usize> {...}

#[inline]
pub(crate) fn next_codepoint_boundary(bytes: &[u8], offset: usize) -> Option<usize> {...}

#[inline]
pub(crate) fn count_utf16_code_units_bytes(bytes: &[u8]) -> usize {...}
```

## xi-editor-ph7/rust/rope/src/metrics/identity.rs

```rust
use std::marker::PhantomData;

use crate::breaks::BreaksMetric;
use crate::tree::{Leaf, Metric, NodeInfo};

#[derive(Clone, Copy)]
pub(crate) struct BaseUnitsIdentity<M> {
    _marker: PhantomData<M>,
}

impl<M> Default for BaseUnitsIdentity<M> {
    #[inline]
    fn default() -> Self {...}
}

impl<N, L, M> Metric<N, L> for BaseUnitsIdentity<M>
where
    N: NodeInfo<L>,
    L: Leaf,
    M: Metric<N, L>,
{
    #[inline]
    fn measure(_: &N, len: usize) -> usize {...}

    #[inline]
    fn to_base_units(_: &L, in_measured_units: usize) -> usize {...}

    #[inline]
    fn from_base_units(_: &L, in_base_units: usize) -> usize {...}

    #[inline]
    fn is_boundary(leaf: &L, offset: usize) -> bool {...}

    #[inline]
    fn prev(leaf: &L, offset: usize) -> Option<usize> {...}

    #[inline]
    fn next(leaf: &L, offset: usize) -> Option<usize> {...}

    #[inline]
    fn can_fragment() -> bool {...}
}

pub(crate) type BreaksBaseMetric = BaseUnitsIdentity<BreaksMetric>;
```

## xi-editor-ph7/rust/rope/src/metrics/lines.rs

```rust
use memchr::{memchr, memrchr};

#[inline]
pub(crate) fn count_newlines_bytes(bytes: &[u8]) -> usize {...}

#[inline]
pub(crate) fn is_newline_boundary(bytes: &[u8], offset: usize) -> bool {...}

#[inline]
pub(crate) fn find_next_newline(bytes: &[u8], offset: usize) -> Option<usize> {...}

#[inline]
pub(crate) fn find_prev_newline(bytes: &[u8], offset: usize) -> Option<usize> {...}
```

## xi-editor-ph7/rust/rope/src/metrics/mod.rs

```rust
pub(crate) mod break_indices;
pub(crate) mod codepoint;
pub(crate) mod identity;
pub(crate) mod lines;

pub(crate) use break_indices::{
    count_breaks_up_to, find_next_break, find_prev_break, is_break_boundary, nth_break_offset,
};
pub(crate) use codepoint::{
    count_utf16_code_units_bytes, is_codepoint_boundary, len_utf8_from_first_byte,
    next_codepoint_boundary, prev_codepoint_boundary,
};
#[allow(unused_imports)]
pub(crate) use identity::{BaseUnitsIdentity, BreaksBaseMetric};
pub(crate) use lines::{
    count_newlines_bytes, find_next_newline, find_prev_newline, is_newline_boundary,
};
```

## xi-editor-ph7/rust/rope/src/multiset.rs

```rust
use std::cmp;

// These two imports are for the `apply` method only.
use crate::interval::Interval;
use crate::tree::{Leaf, Node, NodeInfo, TreeBuilder};
use std::fmt;
use std::slice;

#[derive(Clone, PartialEq, Eq, Debug)]
struct Segment {
    len: usize,
    count: usize,
}

/// Represents a multi-subset of a string, that is a subset where elements can
/// be included multiple times. This is represented as each element of the
/// string having a "count" which is the number of times that element is
/// included in the set.
///
/// Internally, this is stored as a list of "segments" with a length and a count.
#[derive(Clone, PartialEq, Eq)]
pub struct Subset {
    /// Invariant, maintained by `SubsetBuilder`: all `Segment`s have non-zero
    /// length, and no `Segment` has the same count as the one before it.
    segments: Vec<Segment>,
}

#[derive(Default)]
pub struct SubsetBuilder {
    segments: Vec<Segment>,
    total_len: usize,
}

impl SubsetBuilder {
    pub fn new() -> SubsetBuilder {...}

    /// Intended for use with `add_range` to ensure the total length of the
    /// `Subset` corresponds to the document length.
    pub fn pad_to_len(&mut self, total_len: usize) {...}

    /// Sets the count for a given range. This method must be called with a
    /// non-empty range with `begin` not before the largest range or segment added
    /// so far. Gaps will be filled with a 0-count segment.
    pub fn add_range(&mut self, begin: usize, end: usize, count: usize) {...}

    /// Assign `count` to the next `len` elements in the string.
    /// Will panic if called with `len==0`.
    pub fn push_segment(&mut self, len: usize, count: usize) {...}

    pub fn build(self) -> Subset {...}
}

/// Determines which elements of a `Subset` a method applies to
/// based on the count of the element.
#[derive(Clone, Copy, Debug)]
pub enum CountMatcher {
    Zero,
    NonZero,
    All,
}

impl CountMatcher {
    fn matches(self, seg: &Segment) -> bool {...}
}

impl Subset {
    /// Creates an empty `Subset` of a string of length `len`
    pub fn new(len: usize) -> Subset {...}

    /// Mostly for testing.
    pub fn delete_from_string(&self, s: &str) -> String {...}

    // Maybe Subset should be a pure data structure and this method should
    // be a method of Node.
    /// Builds a version of `s` with all the elements in this `Subset` deleted from it.
    pub fn delete_from<N: NodeInfo<L>, L: Leaf>(&self, s: &Node<N, L>) -> Node<N, L> {...}

    /// The length of the resulting sequence after deleting this subset. A
    /// convenience alias for `self.count(CountMatcher::Zero)` to reduce
    /// thinking about what that means in the cases where the length after
    /// delete is what you want to know.
    ///
    /// `self.delete_from_string(s).len() = self.len(s.len())`
    pub fn len_after_delete(&self) -> usize {...}

    /// Count the total length of all the segments matching `matcher`.
    pub fn count(&self, matcher: CountMatcher) -> usize {...}

    /// Convenience alias for `self.count(CountMatcher::All)`
    pub fn len(&self) -> usize {...}

    /// Determine whether the subset is empty.
    /// In this case deleting it would do nothing.
    pub fn is_empty(&self) -> bool {...}

    /// Compute the union of two subsets. The count of an element in the
    /// result is the sum of the counts in the inputs.
    pub fn union(&self, other: &Subset) -> Subset {...}

    /// Compute the difference of two subsets. The count of an element in the
    /// result is the subtraction of the counts of other from self.
    pub fn subtract(&self, other: &Subset) -> Subset {...}

    /// Compute the bitwise xor of two subsets, useful as a reversible
    /// difference. The count of an element in the result is the bitwise xor
    /// of the counts of the inputs. Unchanged segments will be 0.
    ///
    /// This works like set symmetric difference when all counts are 0 or 1
    /// but it extends nicely to the case of larger counts.
    pub fn bitxor(&self, other: &Subset) -> Subset {...}

    /// Map the contents of `self` into the 0-regions of `other`.
    /// Precondition: `self.count(CountMatcher::All) == other.count(CountMatcher::Zero)`
    fn transform(&self, other: &Subset, union: bool) -> Subset {...}

    /// Transform through coordinate transform represented by other.
    /// The equation satisfied is as follows:
    ///
    /// s1 = other.delete_from_string(s0)
    ///
    /// s2 = self.delete_from_string(s1)
    ///
    /// element in self.transform_expand(other).delete_from_string(s0) if (not in s1) or in s2
    pub fn transform_expand(&self, other: &Subset) -> Subset {...}

    /// The same as taking transform_expand and then unioning with `other`.
    pub fn transform_union(&self, other: &Subset) -> Subset {...}

    /// Transform subset through other coordinate transform, shrinking.
    /// The following equation is satisfied:
    ///
    /// C = A.transform_expand(B)
    ///
    /// B.transform_shrink(C).delete_from_string(C.delete_from_string(s)) =
    ///   A.delete_from_string(B.delete_from_string(s))
    pub fn transform_shrink(&self, other: &Subset) -> Subset {...}

    /// Return an iterator over the ranges with a count matching the `matcher`.
    /// These will often be easier to work with than raw segments.
    pub fn range_iter(&self, matcher: CountMatcher) -> RangeIter<'_> {...}

    /// Convenience alias for `self.range_iter(CountMatcher::Zero)`.
    /// Semantically iterates the ranges of the complement of this `Subset`.
    pub fn complement_iter(&self) -> RangeIter<'_> {...}

    /// Return an iterator over `ZipSegment`s where each `ZipSegment` contains
    /// the count for both self and other in that range. The two `Subset`s
    /// must have the same total length.
    ///
    /// Each returned `ZipSegment` will differ in at least one count.
    pub fn zip<'a>(&'a self, other: &'a Subset) -> ZipIter<'a> {...}

    /// Returns an iterator over `(start, len, count)` triples that describe each
    /// segment in document order. Intended for serialization and cross-crate interop
    /// without exposing the internal `Segment` type.
    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    pub(crate) fn segment_triples(&self) -> SegmentTripleIter<'_> {...}

    /// Rebuilds a `Subset` from a sequence of `(start, len, count)` triples.
    /// Segments must be supplied in non-decreasing `start` order and represent
    /// non-overlapping regions of the backing document. Gaps are interpreted as
    /// zero-count segments.
    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    pub(crate) fn from_segment_triples<I>(triples: I) -> Subset
    where
        I: IntoIterator<Item = (usize, usize, usize)>,
    {...}

    /// Returns the number of segments currently stored. Primarily used by
    /// serialization helpers when sizing output sequences.
    #[cfg_attr(not(feature = "serde"), allow(dead_code))]
    pub(crate) fn segment_count(&self) -> usize {...}

    /// Find the complement of this Subset. Every 0-count element will have a
    /// count of 1 and every non-zero element will have a count of 0.
    pub fn complement(&self) -> Subset {...}

    /// Return a `Mapper` that can be use to map coordinates in the document to coordinates
    /// in this `Subset`, but only in non-decreasing order for performance reasons.
    pub fn mapper(&self, matcher: CountMatcher) -> Mapper<'_> {...}
}

/// Iterator produced by `Subset::segment_triples()`.
#[cfg_attr(not(feature = "serde"), allow(dead_code))]
pub(crate) struct SegmentTripleIter<'a> {
    iter: slice::Iter<'a, Segment>,
    offset: usize,
}

impl<'a> Iterator for SegmentTripleIter<'a> {
    type Item = (usize, usize, usize);

    fn next(&mut self) -> Option<Self::Item> {...}
}

impl fmt::Debug for Subset {
    /// Use the alternate flag (`#`) to print a more compact representation
    /// where each character represents the count of one element:
    /// '-' is 0, '#' is 1, 2-9 are digits, `+` is >9
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {...}
}

pub struct RangeIter<'a> {
    seg_iter: slice::Iter<'a, Segment>,
    pub consumed: usize,
    matcher: CountMatcher,
}

impl<'a> Iterator for RangeIter<'a> {
    type Item = (usize, usize);

    fn next(&mut self) -> Option<(usize, usize)> {...}
}

/// See `Subset::zip`
pub struct ZipIter<'a> {
    a_segs: &'a [Segment],
    b_segs: &'a [Segment],
    a_i: usize,
    b_i: usize,
    a_consumed: usize,
    b_consumed: usize,
    pub consumed: usize,
}

/// See `Subset::zip`
#[derive(Clone, Debug)]
pub struct ZipSegment {
    len: usize,
    a_count: usize,
    b_count: usize,
}

impl<'a> Iterator for ZipIter<'a> {
    type Item = ZipSegment;

    /// Consume as far as possible from `self.consumed` until reaching a
    /// segment boundary in either `Subset`, and return the resulting
    /// `ZipSegment`. Will panic if it reaches the end of one `Subset` before
    /// the other, that is when they have different total length.
    fn next(&mut self) -> Option<ZipSegment> {...}
}

#[cfg(feature = "serde")]
mod subset_serde {
    use super::Subset;
    use serde::{Deserialize, Deserializer, Serialize, Serializer};

    #[derive(Serialize, Deserialize)]
    struct SegmentRepr {
        len: usize,
        count: usize,
    }

    #[derive(Serialize, Deserialize)]
    struct SubsetRepr {
        segments: Vec<SegmentRepr>,
    }

    impl Serialize for Subset {
        fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
        where
            S: Serializer,
        {...}
    }

    impl<'de> Deserialize<'de> for Subset {
        fn deserialize<D>(deserializer: D) -> Result<Self, D::Error>
        where
            D: Deserializer<'de>,
        {...}
    }
}

pub struct Mapper<'a> {
    range_iter: RangeIter<'a>,
    // Not actually necessary for computation, just for dynamic checking of invariant
    last_i: usize,
    cur_range: (usize, usize),
    pub subset_amount_consumed: usize,
}

impl<'a> Mapper<'a> {
    /// Map a coordinate in the document this subset corresponds to, to a
    /// coordinate in the subset matched by the `CountMatcher`. For example,
    /// if the Subset is a set of deletions and the matcher is
    /// `CountMatcher::NonZero`, this would map indices in the union string to
    /// indices in the tombstones string.
    ///
    /// Will return the closest coordinate in the subset if the index is not
    /// in the subset. If the coordinate is past the end of the subset it will
    /// return one more than the largest index in the subset (i.e the length).
    /// This behaviour is suitable for mapping closed-open intervals in a
    /// string to intervals in a subset of the string.
    ///
    /// In order to guarantee good performance, this method must be called
    /// with `i` values in non-decreasing order or it will panic. This allows
    /// the total cost to be O(n) where `n = max(calls,ranges)` over all times
    /// called on a single `Mapper`.
    pub fn doc_index_to_subset(&mut self, i: usize) -> usize {...}
}
```

## xi-editor-ph7/rust/rope/src/rope.rs

```rust
#![allow(clippy::needless_return)]

use std::borrow::Cow;
use std::cmp::{min, Ordering};
use std::fmt;
use std::ops::Add;
use std::str::FromStr;
use std::string::ParseError;

use crate::delta::{Delta, DeltaElement};
use crate::helpers::string_leaf::{
    count_utf16_code_units, find_leaf_split_for_bulk, find_leaf_split_for_merge, MAX_LEAF, MIN_LEAF,
};
use crate::interval::{Interval, IntervalBounds};
use crate::metrics::{
    count_newlines_bytes, count_utf16_code_units_bytes, find_next_newline, find_prev_newline,
    is_codepoint_boundary, is_newline_boundary, next_codepoint_boundary, prev_codepoint_boundary,
};
use crate::tree::{Cursor, DefaultMetricProvider, Leaf, Metric, Node, NodeInfo, TreeBuilder};

use memchr::memchr;
use unicode_segmentation::{GraphemeCursor, GraphemeIncomplete};

/// A rope data structure.
///
/// A [rope](https://en.wikipedia.org/wiki/Rope_(data_structure)) is a data structure
/// for strings, specialized for incremental editing operations. Most operations
/// (such as insert, delete, substring) are O(log n). This module provides an immutable
/// (also known as [persistent](https://en.wikipedia.org/wiki/Persistent_data_structure))
/// version of Ropes, and if there are many copies of similar strings, the common parts
/// are shared.
///
/// Internally, the implementation uses thread safe reference counting.
/// Mutations are generally copy-on-write, though in-place edits are
/// supported as an optimization when only one reference exists, making the
/// implementation as efficient as a mutable version.
///
/// Also note: in addition to the `From` traits described below, this module
/// implements `From<Rope> for String` and `From<&Rope> for String`, for easy
/// conversions in both directions.
///
/// # Examples
///
/// Create a `Rope` from a `String`:
///
/// ```rust
/// # use xi_rope::Rope;
/// let a = Rope::from("hello ");
/// let b = Rope::from("world");
/// assert_eq!("hello world", String::from(a.clone() + b.clone()));
/// assert!("hello world" == String::from(a + b));
/// ```
///
/// Get a slice of a `Rope`:
///
/// ```rust
/// # use xi_rope::Rope;
/// let a = Rope::from("hello world");
/// let b = a.slice(1..9);
/// assert_eq!("ello wor", String::from(&b));
/// let c = b.slice(1..7);
/// assert_eq!("llo wo", String::from(c));
/// ```
///
/// Replace part of a `Rope`:
///
/// ```rust
/// # use xi_rope::Rope;
/// let mut a = Rope::from("hello world");
/// a.edit(1..9, "era");
/// assert_eq!("herald", String::from(a));
/// ```
pub type Rope = Node<RopeInfo, String>;

/// Represents a transform from one rope to another.
pub type RopeDelta = Delta<RopeInfo, String>;

/// An element in a `RopeDelta`.
pub type RopeDeltaElement = DeltaElement<RopeInfo, String>;

impl Leaf for String {
    fn len(&self) -> usize {...}

    fn is_ok_child(&self) -> bool {...}

    fn push_maybe_split(&mut self, other: &String, iv: Interval) -> Option<String> {...}
}

#[derive(Clone, Copy)]
pub struct RopeInfo {
    lines: usize,
    utf16_size: usize,
}

impl NodeInfo<String> for RopeInfo {
    fn accumulate(&mut self, other: &Self) {...}

    fn compute_info(s: &String) -> Self {...}

    fn identity() -> Self {...}
}

impl DefaultMetricProvider<String> for RopeInfo {
    fn convert_from_default<M: Metric<Self, String>>(
        node: &Node<Self, String>,
        offset: usize,
    ) -> usize {...}

    fn convert_to_default<M: Metric<Self, String>>(
        node: &Node<Self, String>,
        offset: usize,
    ) -> usize {...}
}

//TODO: document metrics, based on https://github.com/google/xi-editor/issues/456
//See ../docs/MetricsAndBoundaries.md for more information.
/// This metric let us walk utf8 text by code point.
///
/// `BaseMetric` implements the trait [Metric].  Both its _measured unit_ and
/// its _base unit_ are utf8 code unit.
///
/// Offsets that do not correspond to codepoint boundaries are _invalid_, and
/// calling functions that assume valid offsets with invalid offets will panic
/// in debug mode.
///
/// Boundary is atomic and determined by codepoint boundary.  Atomicity is
/// implicit, because offsets between two utf8 code units that form a code
/// point is considered invalid. For example, if a string starts with a
/// 0xC2 byte, then `offset=1` is invalid.
#[derive(Clone, Copy)]
pub struct BaseMetric(());

impl Metric<RopeInfo, String> for BaseMetric {
    fn measure(_: &RopeInfo, len: usize) -> usize {...}

    fn to_base_units(s: &String, in_measured_units: usize) -> usize {...}

    fn from_base_units(s: &String, in_base_units: usize) -> usize {...}

    fn is_boundary(s: &String, offset: usize) -> bool {...}

    fn prev(s: &String, offset: usize) -> Option<usize> {...}

    fn next(s: &String, offset: usize) -> Option<usize> {...}

    fn can_fragment() -> bool {...}
}

/// Given the inital byte of a UTF-8 codepoint, returns the number of
/// bytes required to represent the codepoint.
/// RFC reference : https://tools.ietf.org/html/rfc3629#section-4
pub fn len_utf8_from_first_byte(b: u8) -> usize {...}

#[derive(Clone, Copy)]
#[allow(dead_code)]
pub struct LinesMetric(usize); // number of lines

/// Measured unit is newline amount.
/// Base unit is utf8 code unit.
/// Boundary is trailing and determined by a newline char.
impl Metric<RopeInfo, String> for LinesMetric {
    fn measure(info: &RopeInfo, _: usize) -> usize {...}

    fn is_boundary(s: &String, offset: usize) -> bool {...}

    fn to_base_units(s: &String, in_measured_units: usize) -> usize {...}

    fn from_base_units(s: &String, in_base_units: usize) -> usize {...}

    fn prev(s: &String, offset: usize) -> Option<usize> {...}

    fn next(s: &String, offset: usize) -> Option<usize> {...}

    fn can_fragment() -> bool {...}
}

#[derive(Clone, Copy)]
#[allow(dead_code)]
pub struct Utf16CodeUnitsMetric(usize);

impl Metric<RopeInfo, String> for Utf16CodeUnitsMetric {
    fn measure(info: &RopeInfo, _: usize) -> usize {...}

    fn is_boundary(s: &String, offset: usize) -> bool {...}

    fn to_base_units(s: &String, in_measured_units: usize) -> usize {...}

    fn from_base_units(s: &String, in_base_units: usize) -> usize {...}

    fn prev(s: &String, offset: usize) -> Option<usize> {...}

    fn next(s: &String, offset: usize) -> Option<usize> {...}

    fn can_fragment() -> bool {...}
}

// Low level functions

pub fn count_newlines(s: &str) -> usize {...}

// Additional APIs custom to strings

impl FromStr for Rope {
    type Err = ParseError;
    fn from_str(s: &str) -> Result<Rope, Self::Err> {...}
}

impl Rope {
    /// Edit the string, replacing the byte range [`start`..`end`] with `new`.
    ///
    /// Time complexity: O(log n)
    #[deprecated(since = "0.3.0", note = "Use Rope::edit instead")]
    pub fn edit_str<T: IntervalBounds>(&mut self, iv: T, new: &str) {...}

    /// Returns a new Rope with the contents of the provided range.
    pub fn slice<T: IntervalBounds>(&self, iv: T) -> Rope {...}

    // encourage callers to use Cursor instead?

    /// Determine whether `offset` lies on a codepoint boundary.
    pub fn is_codepoint_boundary(&self, offset: usize) -> bool {...}

    /// Return the offset of the codepoint before `offset`.
    pub fn prev_codepoint_offset(&self, offset: usize) -> Option<usize> {...}

    /// Return the offset of the codepoint after `offset`.
    pub fn next_codepoint_offset(&self, offset: usize) -> Option<usize> {...}

    /// Returns `offset` if it lies on a codepoint boundary. Otherwise returns
    /// the codepoint after `offset`.
    pub fn at_or_next_codepoint_boundary(&self, offset: usize) -> Option<usize> {...}

    /// Returns `offset` if it lies on a codepoint boundary. Otherwise returns
    /// the codepoint before `offset`.
    pub fn at_or_prev_codepoint_boundary(&self, offset: usize) -> Option<usize> {...}

    pub fn prev_grapheme_offset(&self, offset: usize) -> Option<usize> {...}

    pub fn next_grapheme_offset(&self, offset: usize) -> Option<usize> {...}

    /// Return the line number corresponding to the byte index `offset`.
    ///
    /// The line number is 0-based, thus this is equivalent to the count of newlines
    /// in the slice up to `offset`.
    ///
    /// Time complexity: O(log n)
    ///
    /// # Panics
    ///
    /// This function will panic if `offset > self.len()`. Callers are expected to
    /// validate their input.
    pub fn line_of_offset(&self, offset: usize) -> usize {...}

    /// Return the byte offset corresponding to the line number `line`.
    /// If `line` is equal to one plus the current number of lines,
    /// this returns the offset of the end of the rope. Arguments higher
    /// than this will panic.
    ///
    /// The line number is 0-based.
    ///
    /// Time complexity: O(log n)
    ///
    /// # Panics
    ///
    /// This function will panic if `line > self.measure::<LinesMetric>() + 1`.
    /// Callers are expected to validate their input.
    pub fn offset_of_line(&self, line: usize) -> usize {...}

    /// Converts a UTF-8 byte offset into a zero-based line count.
    ///
    /// This portability shim mirrors `count::<LinesMetric>` for consumers in
    /// other languages that cannot call the generic metric APIs directly.
    #[inline]
    pub fn convert_lines_from_bytes(&self, offset: usize) -> usize {...}

    /// Converts a zero-based line index into a UTF-8 byte offset.
    ///
    /// This portability shim mirrors `count_base_units::<LinesMetric>` for
    /// language bindings that require concrete method names.
    #[inline]
    pub fn convert_bytes_from_lines(&self, line: usize) -> usize {...}

    /// Converts a UTF-8 byte offset into a UTF-16 code unit count.
    ///
    /// This portability shim mirrors `count::<Utf16CodeUnitsMetric>` to make
    /// cross-language consumers independent of the generic metric plumbing.
    #[inline]
    pub fn convert_utf16_from_bytes(&self, offset: usize) -> usize {...}

    /// Converts a UTF-16 code unit count into a UTF-8 byte offset.
    ///
    /// This portability shim mirrors `count_base_units::<Utf16CodeUnitsMetric>`
    /// for language bindings that prefer dedicated helper names.
    #[inline]
    pub fn convert_bytes_from_utf16(&self, units: usize) -> usize {...}

    /// Returns an iterator over chunks of the rope.
    ///
    /// Each chunk is a `&str` slice borrowed from the rope's storage. The size
    /// of the chunks is indeterminate but for large strings will generally be
    /// in the range of 511-1024 bytes.
    ///
    /// The empty string will yield a single empty slice. In all other cases, the
    /// slices will be nonempty.
    ///
    /// Time complexity: technically O(n log n), but the constant factor is so
    /// tiny it is effectively O(n). This iterator does not allocate.
    pub fn iter_chunks<T: IntervalBounds>(&self, range: T) -> ChunkIter<'_> {...}

    /// An iterator over the raw lines. The lines, except the last, include the
    /// terminating newline.
    ///
    /// The return type is a `Cow<str>`, and in most cases the lines are slices
    /// borrowed from the rope.
    pub fn lines_raw<T: IntervalBounds>(&self, range: T) -> LinesRaw<'_> {...}

    /// An iterator over the lines of a rope.
    ///
    /// Lines are ended with either Unix (`\n`) or MS-DOS (`\r\n`) style line endings.
    /// The line ending is stripped from the resulting string. The final line ending
    /// is optional.
    ///
    /// The return type is a `Cow<str>`, and in most cases the lines are slices borrowed
    /// from the rope.
    ///
    /// The semantics are intended to match `str::lines()`.
    pub fn lines<T: IntervalBounds>(&self, range: T) -> Lines<'_> {...}

    // callers should be encouraged to use cursor instead
    pub fn byte_at(&self, offset: usize) -> u8 {...}

    pub fn slice_to_cow<T: IntervalBounds>(&self, range: T) -> Cow<'_, str> {...}
}

// should make this generic, but most leaf types aren't going to be sliceable
pub struct ChunkIter<'a> {
    cursor: Cursor<'a, RopeInfo, String>,
    end: usize,
}

impl<'a> Iterator for ChunkIter<'a> {
    type Item = &'a str;

    fn next(&mut self) -> Option<&'a str> {...}
}

impl TreeBuilder<RopeInfo, String> {
    /// Push a string on the accumulating tree in the naive way.
    ///
    /// Splits the provided string in chunks that fit in a leaf
    /// and pushes the leaves one by one onto the tree by calling
    /// `push_leaf` on the builder.
    pub fn push_str(&mut self, mut s: &str) {...}
}

impl<T: AsRef<str>> From<T> for Rope {
    fn from(s: T) -> Rope {...}
}

impl From<Rope> for String {
    // maybe explore grabbing leaf? would require api in tree
    fn from(r: Rope) -> String {...}
}

impl From<&Rope> for String {
    fn from(r: &Rope) -> String {...}
}

impl fmt::Display for Rope {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {...}
}

impl fmt::Debug for Rope {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {...}
}

impl Add for Rope {
    type Output = Rope;
    fn add(self, rhs: Rope) -> Rope {...}
}

//additional cursor features

impl<'a> Cursor<'a, RopeInfo, String> {
    /// Get previous codepoint before cursor position, and advance cursor backwards.
    pub fn prev_codepoint(&mut self) -> Option<char> {...}

    /// Get next codepoint after cursor position, and advance cursor.
    pub fn next_codepoint(&mut self) -> Option<char> {...}

    /// Get the next codepoint after the cursor position, without advancing
    /// the cursor.
    pub fn peek_next_codepoint(&self) -> Option<char> {...}

    pub fn next_grapheme(&mut self) -> Option<usize> {...}

    pub fn prev_grapheme(&mut self) -> Option<usize> {...}
}

// line iterators

pub struct LinesRaw<'a> {
    inner: ChunkIter<'a>,
    fragment: &'a str,
}

fn cow_append<'a>(a: Cow<'a, str>, b: &'a str) -> Cow<'a, str> {...}

impl<'a> Iterator for LinesRaw<'a> {
    type Item = Cow<'a, str>;

    fn next(&mut self) -> Option<Cow<'a, str>> {...}
}

pub struct Lines<'a> {
    inner: LinesRaw<'a>,
}

impl<'a> Iterator for Lines<'a> {
    type Item = Cow<'a, str>;

    fn next(&mut self) -> Option<Cow<'a, str>> {...}
}
```

## xi-editor-ph7/rust/rope/src/serde_fixtures.rs

```rust
pub mod breaks_descriptors;
pub mod chunk_descriptors;
pub mod cursor_descriptors;
pub mod diff_regions;
pub mod grapheme_descriptors;
pub mod search_spans;
pub mod snapshots;

pub use cursor_descriptors::{
    cursor_descriptor_samples, export_cursor_descriptor_fixtures, CursorDescriptorExportReport,
    CursorDescriptorFixture, CursorDescriptorFrame, CursorDescriptorOffsets, DescriptorMetric,
    CURSOR_DESCRIPTOR_FILENAME,
};

pub use chunk_descriptors::{
    chunk_descriptor_fixtures, export_chunk_descriptors, ChunkDescriptor,
    ChunkDescriptorExportReport, ChunkDescriptorFile, LineDescriptor, CHUNK_DESCRIPTOR_FILENAME,
};

pub use grapheme_descriptors::{
    export_grapheme_descriptors, grapheme_descriptor_fixtures, GraphemeDescriptor,
    GraphemeDescriptorExportReport, GraphemeDescriptorFile, GRAPHEME_DESCRIPTOR_FILENAME,
};

pub use breaks_descriptors::{
    export_breaks_descriptors, BreakMetricKind, BreakSetDescriptor, BreaksDescriptorExportReport,
    BreaksDescriptorFile, BREAKS_DESCRIPTOR_FILENAME,
};

pub use diff_regions::{
    export_diff_regions, DiffCase, DiffOpKind, DiffOpSnapshot, DiffRegionsExportReport,
    DiffRegionsFile, DIFF_REGIONS_FILENAME,
};

pub use search_spans::{
    export_search_spans, CaseMatchingSnapshot, SearchCaseSnapshot, SearchHitSnapshot,
    SearchSpansExportReport, SearchSpansFile, SpanSegmentSnapshot, SEARCH_SPANS_FILENAME,
};

pub use snapshots::{frames_from_descriptor, PathFrameSnapshot, RangeSnapshot};

/// Describes a single serde regression fixture.
#[derive(Copy, Clone, Debug)]
pub struct Fixture {
    pub name: &'static str,
    pub json: &'static str,
}

pub const SUBSET_FIXTURE: Fixture = Fixture {
    name: "subset_regression.json",
    json: r#"{"segments":[{"len":2,"count":0},{"len":3,"count":3},{"len":1,"count":0},{"len":1,"count":1},{"len":2,"count":0}]}"#,
};

pub const DELTA_FIXTURE: Fixture = Fixture {
    name: "delta_regression.json",
    json: r#"{"els":[{"copy":[0,3]},{"insert":"[ins]"},{"copy":[8,10]},{"insert":"!"},{"copy":[15,62]}],"base_len":62}"#,
};

pub const ENGINE_FIXTURE: Fixture = Fixture {
    name: "engine_regression.json",
    json: r#"{"text":"Hi there","tombstones":"Well, ","deletes_from_union":{"segments":[{"len":6,"count":1},{"len":8,"count":0}]},"undone_groups":[2],"revs":[{"rev_id":{"session1":0,"session2":0,"num":0},"max_undo_so_far":0,"edit":{"Undo":{"toggled_groups":[],"deletes_bitxor":{"segments":[]}}}},{"rev_id":{"session1":1,"session2":0,"num":1},"max_undo_so_far":0,"edit":{"Edit":{"priority":0,"undo_group":0,"inserts":{"segments":[{"len":2,"count":1}]},"deletes":{"segments":[{"len":2,"count":0}]}}}},{"rev_id":{"session1":1,"session2":0,"num":2},"max_undo_so_far":1,"edit":{"Edit":{"priority":1,"undo_group":1,"inserts":{"segments":[{"len":2,"count":0},{"len":6,"count":1}]},"deletes":{"segments":[{"len":8,"count":0}]}}}},{"rev_id":{"session1":1,"session2":0,"num":3},"max_undo_so_far":2,"edit":{"Edit":{"priority":0,"undo_group":2,"inserts":{"segments":[{"len":6,"count":1},{"len":8,"count":0}]},"deletes":{"segments":[{"len":14,"count":0}]}}}},{"rev_id":{"session1":1,"session2":0,"num":4},"max_undo_so_far":2,"edit":{"Undo":{"toggled_groups":[2],"deletes_bitxor":{"segments":[{"len":6,"count":1},{"len":8,"count":0}]}}}}]}"#,
};

pub const FIXTURES: [Fixture; 3] = [SUBSET_FIXTURE, DELTA_FIXTURE, ENGINE_FIXTURE];

/// Returns the registered fixtures as a slice for iteration.
pub const fn fixtures() -> &'static [Fixture] {...}

/// Attempts to lookup a fixture by file name.
pub fn get_fixture(name: &str) -> Option<&'static Fixture> {...}

pub fn detect_git_commit() -> Option<String> {...}
```

## xi-editor-ph7/rust/rope/src/serde_fixtures/breaks_descriptors.rs

```rust
use std::error::Error;
use std::path::{Path, PathBuf};
use std::time::{SystemTime, UNIX_EPOCH};

use serde::{Deserialize, Serialize};

use crate::breaks::{BreakBuilder, Breaks};
use crate::rope::Rope;
use crate::tree::Cursor;

use super::detect_git_commit;
use super::snapshots::{frames_from_descriptor, PathFrameSnapshot, RangeSnapshot};

pub const BREAKS_DESCRIPTOR_FILENAME: &str = "breaks_descriptors.json";
const BREAKS_SCHEMA_VERSION: &str = "1.0.0";
const EXCERPT_CODEPOINT_LIMIT: usize = 160;

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct BreaksDescriptorFile {
    pub metadata: BreaksDescriptorMetadata,
    pub break_sets: Vec<BreakSetDescriptor>,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct BreaksDescriptorMetadata {
    pub schema_version: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub rust_commit: Option<String>,
    pub generated_at_unix_millis: u128,
    pub descriptor_count: usize,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct BreakSetDescriptor {
    pub sample: String,
    pub rope_len: usize,
    pub wrap_width_units: usize,
    pub metric: BreakMetricKind,
    pub break_offsets: Vec<usize>,
    pub break_count: usize,
    pub leaf_runs: Vec<LeafRunSnapshot>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub text_excerpt: Option<String>,
    #[serde(skip_serializing_if = "Vec::is_empty")]
    pub tags: Vec<String>,
}

#[derive(Clone, Copy, Debug, Serialize, Deserialize, PartialEq, Eq)]
pub enum BreakMetricKind {
    #[serde(rename = "BreaksMetric")]
    BreaksMetric,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct LeafRunSnapshot {
    pub range: RangeSnapshot,
    pub break_count: usize,
    pub path: Vec<PathFrameSnapshot>,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct BreaksDescriptorExportReport {
    pub file_path: PathBuf,
    pub descriptor_count: usize,
}

struct BreaksSample {
    name: &'static str,
    text: &'static str,
    wrap_width: usize,
    tags: &'static [&'static str],
}

pub fn export_breaks_descriptors(
    dir: &Path,
) -> Result<BreaksDescriptorExportReport, Box<dyn Error>> {...}

fn build_breaks_descriptor_file(existing_timestamp: Option<u128>) -> BreaksDescriptorFile {...}

fn build_break_set(sample: &BreaksSample) -> BreakSetDescriptor {...}

fn breaks_samples() -> Vec<BreaksSample> {...}

fn greedy_break_offsets(text: &str, wrap: usize) -> Vec<usize> {...}

fn build_breaks_tree(text_len: usize, offsets: &[usize]) -> Breaks {...}

fn capture_leaf_runs(rope: &Rope, breaks: &Breaks) -> Vec<LeafRunSnapshot> {...}

fn compose_break_tags(base: &[&str], text: &str) -> Vec<String> {...}

fn text_excerpt(text: &str) -> Option<String> {...}

fn truncate_codepoints(text: &str, limit: usize) -> String {...}

fn current_millis() -> u128 {...}

fn read_existing_generated_at(path: &Path) -> Option<u128> {...}
```

## xi-editor-ph7/rust/rope/src/serde_fixtures/chunk_descriptors.rs

```rust
use std::borrow::Cow;
use std::path::{Path, PathBuf};
use std::time::{SystemTime, UNIX_EPOCH};

use serde::{Deserialize, Serialize};

use crate::helpers::string_leaf::{MAX_LEAF, MIN_LEAF};
use crate::rope::Rope;
use crate::tree::{Cursor, TreeBuilder};

use super::detect_git_commit;
use super::snapshots::{frames_from_descriptor, PathFrameSnapshot, RangeSnapshot};
use crate::rope::RopeInfo;

pub const CHUNK_DESCRIPTOR_FILENAME: &str = "chunk_descriptors.json";
const CHUNK_SCHEMA_VERSION: &str = "1.0.0";
const DEFAULT_CONTEXT_WINDOW: usize = 16;

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct ChunkDescriptorFile {
    pub metadata: ChunkDescriptorMetadata,
    pub chunk_descriptors: Vec<ChunkDescriptor>,
    pub line_descriptors: Vec<LineDescriptor>,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct ChunkDescriptorMetadata {
    pub schema_version: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub rust_commit: Option<String>,
    pub generated_at_unix_millis: u128,
    pub chunk_descriptor_count: usize,
    pub line_descriptor_count: usize,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct ChunkDescriptor {
    pub sample: String,
    pub chunk_index: usize,
    pub text: String,
    pub byte_range: RangeSnapshot,
    pub utf16_range: RangeSnapshot,
    pub leaf_range: RangeSnapshot,
    pub contains_crlf: bool,
    pub is_empty: bool,
    pub tags: Vec<String>,
    pub path: Vec<PathFrameSnapshot>,
    pub context: ChunkContext,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct LineDescriptor {
    pub sample: String,
    pub line_index: usize,
    pub raw: String,
    pub logical: String,
    pub byte_range: RangeSnapshot,
    pub utf16_range: RangeSnapshot,
    pub newline_kind: LineEndingKind,
    pub tags: Vec<String>,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct ChunkContext {
    pub before: String,
    pub after: String,
}

#[derive(Clone, Copy, Debug, Serialize, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "snake_case")]
pub enum LineEndingKind {
    None,
    Lf,
    Cr,
    CrLf,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct ChunkDescriptorExportReport {
    pub file_path: PathBuf,
    pub chunk_count: usize,
    pub line_count: usize,
}

struct RopeFixtureSample {
    name: &'static str,
    rope: Rope,
    tags: &'static [&'static str],
    include_in_lines: bool,
    max_chunks: usize,
    max_lines: usize,
}

pub fn export_chunk_descriptors(
    dir: &Path,
) -> Result<ChunkDescriptorExportReport, Box<dyn std::error::Error>> {...}

fn chunk_context(rope: &Rope, start: usize, end: usize) -> ChunkContext {...}

fn clamp_prev_boundary(rope: &Rope, offset: usize) -> usize {...}

fn clamp_next_boundary(rope: &Rope, offset: usize) -> usize {...}
pub fn chunk_descriptor_fixtures(existing_timestamp: Option<u128>) -> ChunkDescriptorFile {...}

fn build_chunk_descriptors(samples: &[RopeFixtureSample]) -> Vec<ChunkDescriptor> {...}

fn build_line_descriptors(samples: &[RopeFixtureSample]) -> Vec<LineDescriptor> {...}

fn snapshot_chunk(
    sample: &RopeFixtureSample,
    chunk_index: usize,
    chunk_text: &str,
    absolute_start: usize,
    rope: &Rope,
    cursor: &Cursor<'_, RopeInfo, String>,
) -> ChunkDescriptor {...}

fn empty_chunk_descriptor(sample: &RopeFixtureSample) -> ChunkDescriptor {...}

fn compose_chunk_tags(base: &[&str], chunk_text: &str) -> Vec<String> {...}

fn compose_line_tags(base: &[&str], newline_kind: LineEndingKind) -> Vec<String> {...}

fn detect_newline_kind(raw: &str) -> LineEndingKind {...}

fn current_millis() -> u128 {...}

fn chunk_samples() -> Vec<RopeFixtureSample> {...}

fn build_deep_tree_sample() -> Rope {...}

fn deep_leaf_payload(idx: usize) -> String {...}

fn read_existing_generated_at(path: &Path) -> Option<u128> {...}
```

## xi-editor-ph7/rust/rope/src/serde_fixtures/cursor_descriptors.rs

```rust
use std::path::{Path, PathBuf};

use serde::{Deserialize, Serialize};

#[cfg(feature = "cursor_state")]
use super::snapshots::frames_from_state;
use super::snapshots::PathFrameSnapshot;
#[cfg(feature = "cursor_state")]
use crate::tree::CursorState;
use crate::{
    helpers::string_leaf::{MAX_LEAF, MIN_LEAF},
    rope::{LinesMetric, Rope, RopeInfo, Utf16CodeUnitsMetric},
    tree::{Cursor, CursorDescriptor, TreeBuilder},
};

pub const CURSOR_DESCRIPTOR_FILENAME: &str = "cursor_descriptors.json";
const DEEP_TREE_LEAF_COUNT_EXP: u32 = 5;
const MIN_DEEP_PATH_DEPTH: usize = 5;

#[derive(Clone, Copy, Debug, Serialize, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "snake_case")]
pub enum DescriptorMetric {
    Base,
    Lines,
    Utf16,
    Breaks,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct CursorDescriptorOffsets {
    pub offset_of_leaf: usize,
    pub offset_in_leaf: usize,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub leaf_len: Option<usize>,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct CursorDescriptorFrame {
    pub node_height: usize,
    pub node_len: usize,
    pub child_index: usize,
    pub child_offset: usize,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct CursorStateSnapshot {
    pub cursor_state_enabled: bool,
    pub position: usize,
    pub offset_of_leaf: usize,
    pub is_valid: bool,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub leaf_len: Option<usize>,
    pub path: Vec<PathFrameSnapshot>,
    pub metric: DescriptorMetric,
    pub edit_version: u64,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub edit_version_after_edit: Option<u64>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub invalidated_after_edit: Option<bool>,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct CursorDescriptorFixture {
    pub name: String,
    pub text: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub edited_text: Option<String>,
    #[serde(default = "default_true")]
    pub expect_apply: bool,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub expect_apply_after_edit: Option<bool>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub notes: Option<String>,
    pub metric: DescriptorMetric,
    pub position: usize,
    pub is_valid: bool,
    pub offsets: CursorDescriptorOffsets,
    pub leaf_path: Vec<CursorDescriptorFrame>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub cursor_state: Option<CursorStateSnapshot>,
}

fn default_true() -> bool {...}

#[derive(Clone, Debug)]
pub struct CursorDescriptorExportReport {
    pub file_path: PathBuf,
    pub sample_count: usize,
}

#[cfg_attr(not(feature = "cursor_state"), allow(dead_code))]
#[derive(Clone, Copy, Debug)]
struct CursorStateParams {
    edit_version: u64,
    edit_version_after_edit: Option<u64>,
    invalidated_after_edit: Option<bool>,
    metric_override: Option<DescriptorMetric>,
}

impl CursorStateParams {
    const fn new(edit_version: u64) -> Self {...}

    fn with_after_edit(mut self, edit_version_after_edit: u64, invalidated: bool) -> Self {...}

    fn with_metric(mut self, metric: DescriptorMetric) -> Self {...}
}

pub fn export_cursor_descriptor_fixtures(
    dir: &Path,
) -> Result<CursorDescriptorExportReport, Box<dyn std::error::Error>> {...}

pub fn cursor_descriptor_samples() -> Vec<CursorDescriptorFixture> {...}

fn sample_empty_base() -> CursorDescriptorFixture {...}

fn sample_single_leaf_midpoint() -> CursorDescriptorFixture {...}

fn sample_single_leaf_end() -> CursorDescriptorFixture {...}

fn sample_lines_middle() -> CursorDescriptorFixture {...}

fn sample_lines_tail_boundary() -> CursorDescriptorFixture {...}

fn sample_utf16_surrogate_midpoint() -> CursorDescriptorFixture {...}

fn sample_utf16_cluster_tail() -> CursorDescriptorFixture {...}

fn sample_breaks_metric_soft_wrap() -> CursorDescriptorFixture {...}

fn sample_split_leaf_boundary() -> CursorDescriptorFixture {...}

fn sample_deep_tree_midpoint() -> CursorDescriptorFixture {...}

fn sample_post_edit_invalidates() -> CursorDescriptorFixture {...}

fn sample_invalid_descriptor() -> CursorDescriptorFixture {...}

#[allow(clippy::too_many_arguments)]
fn fixture_from_descriptor(
    name: &str,
    rope: &Rope,
    descriptor: CursorDescriptor<RopeInfo, String>,
    metric: DescriptorMetric,
    notes: &str,
    expect_apply: bool,
    edited_text: Option<String>,
    expect_apply_after_edit: Option<bool>,
    cursor_state_params: Option<CursorStateParams>,
) -> CursorDescriptorFixture {...}

fn build_deep_rope() -> Rope {...}

fn generate_leaf_payload() -> String {...}

fn cursor_state_snapshot(
    descriptor: &CursorDescriptor<RopeInfo, String>,
    fixture_metric: DescriptorMetric,
    params: Option<CursorStateParams>,
) -> Option<CursorStateSnapshot> {...}
```

## xi-editor-ph7/rust/rope/src/serde_fixtures/diff_regions.rs

```rust
use std::error::Error;
use std::path::{Path, PathBuf};
use std::time::{SystemTime, UNIX_EPOCH};

use serde::{Deserialize, Serialize};

use crate::delta::DeltaElement;
use crate::diff::{Diff, LineHashDiff};
use crate::rope::{Rope, RopeDelta};

use super::detect_git_commit;
use super::snapshots::RangeSnapshot;

pub const DIFF_REGIONS_FILENAME: &str = "diff_regions.json";
const DIFF_REGIONS_SCHEMA_VERSION: &str = "1.0.0";
const INSERT_PREVIEW_LIMIT: usize = 80;

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct DiffRegionsFile {
    pub metadata: DiffRegionsMetadata,
    pub diff_cases: Vec<DiffCase>,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct DiffRegionsMetadata {
    pub schema_version: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub rust_commit: Option<String>,
    pub generated_at_unix_millis: u128,
    pub case_count: usize,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct DiffCase {
    pub sample: String,
    pub base_path: String,
    pub target_path: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub base_sha: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub target_sha: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub line_count: Option<usize>,
    pub ops: Vec<DiffOpSnapshot>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub stats: Option<DiffStats>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub notes: Option<String>,
}

#[derive(Clone, Debug, Serialize, Deserialize, Default)]
pub struct DiffStats {
    pub copied_bytes: usize,
    pub inserted_bytes: usize,
    pub deleted_bytes: usize,
}

#[derive(Clone, Copy, Debug, Serialize, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "snake_case")]
pub enum DiffOpKind {
    Copy,
    Insert,
    Delete,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct DiffOpSnapshot {
    pub kind: DiffOpKind,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub base_range: Option<RangeSnapshot>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub target_range: Option<RangeSnapshot>,
    pub byte_len: usize,
    pub line_span: LineSpanSnapshot,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub insert_preview: Option<String>,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct LineSpanSnapshot {
    pub base: [usize; 2],
    pub target: [usize; 2],
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct DiffRegionsExportReport {
    pub file_path: PathBuf,
    pub case_count: usize,
}

struct DiffSample {
    name: &'static str,
    base_text: &'static str,
    target_text: &'static str,
    notes: &'static str,
}

pub fn export_diff_regions(dir: &Path) -> Result<DiffRegionsExportReport, Box<dyn Error>> {...}

fn build_diff_regions(
    dir: &Path,
    existing_timestamp: Option<u128>,
) -> Result<DiffRegionsFile, Box<dyn Error>> {...}

fn convert_delta_to_ops(
    delta: &RopeDelta,
    base_text: &str,
    target_text: &str,
) -> (Vec<DiffOpSnapshot>, DiffStats) {...}

fn emit_delete(
    ops: &mut Vec<DiffOpSnapshot>,
    stats: &mut DiffStats,
    start: usize,
    end: usize,
    base_index: &LineIndex,
    target_index: &LineIndex,
    target_cursor: usize,
) {...}

fn diff_samples() -> Vec<DiffSample> {...}

fn write_sample_file(
    dir: &Path,
    name: &str,
    suffix: &str,
    contents: &str,
) -> Result<PathBuf, Box<dyn Error>> {...}

fn relative_fixture_path(path: &Path) -> String {...}

fn line_span(index: &LineIndex, range: Option<&RangeSnapshot>, fallback: usize) -> [usize; 2] {...}

struct LineIndex {
    starts: Vec<usize>,
    len: usize,
}

impl LineIndex {
    fn new(text: &str) -> Self {...}

    fn line_of_offset(&self, offset: usize) -> usize {...}

    fn total_lines(&self) -> usize {...}
}

fn truncate_codepoints(text: &str, limit: usize) -> String {...}

fn current_millis() -> u128 {...}

fn workspace_root() -> PathBuf {...}

fn read_existing_generated_at(path: &Path) -> Option<u128> {...}
```

## xi-editor-ph7/rust/rope/src/serde_fixtures/grapheme_descriptors.rs

```rust
use std::path::{Path, PathBuf};
use std::time::{SystemTime, UNIX_EPOCH};

use serde::{Deserialize, Serialize};

use crate::helpers::string_leaf::{MAX_LEAF, MIN_LEAF};
use crate::rope::Rope;
use crate::tree::{Cursor, TreeBuilder};
use unicode_segmentation::UnicodeSegmentation;

use super::detect_git_commit;
use super::snapshots::{frames_from_descriptor, PathFrameSnapshot, RangeSnapshot};
use crate::rope::RopeInfo;

pub const GRAPHEME_DESCRIPTOR_FILENAME: &str = "grapheme_descriptors.json";
const GRAPHEME_SCHEMA_VERSION: &str = "1.0.0";
const GRAPHEME_CONTEXT_WINDOW: usize = 24;

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct GraphemeDescriptorFile {
    pub metadata: GraphemeDescriptorMetadata,
    pub grapheme_descriptors: Vec<GraphemeDescriptor>,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct GraphemeDescriptorMetadata {
    pub schema_version: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub rust_commit: Option<String>,
    pub generated_at_unix_millis: u128,
    pub descriptor_count: usize,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct GraphemeDescriptor {
    pub sample: String,
    pub cluster_index: usize,
    pub cluster: String,
    pub byte_range: RangeSnapshot,
    pub utf16_range: RangeSnapshot,
    pub scalar_count: usize,
    pub contains_zwj: bool,
    pub is_ascii: bool,
    pub crosses_leaf: bool,
    pub requires_fallback: bool,
    pub tags: Vec<String>,
    pub context: GraphemeContext,
    pub leaf: LeafSnapshot,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct GraphemeContext {
    pub before: String,
    pub after: String,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct LeafSnapshot {
    pub range: RangeSnapshot,
    pub path: Vec<PathFrameSnapshot>,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct GraphemeDescriptorExportReport {
    pub file_path: PathBuf,
    pub descriptor_count: usize,
}

struct GraphemeSample {
    name: &'static str,
    rope: Rope,
    tags: &'static [&'static str],
    max_clusters: usize,
}

pub fn export_grapheme_descriptors(
    dir: &Path,
) -> Result<GraphemeDescriptorExportReport, Box<dyn std::error::Error>> {...}

pub fn grapheme_descriptor_fixtures(existing_timestamp: Option<u128>) -> GraphemeDescriptorFile {...}

fn build_grapheme_descriptors(samples: &[GraphemeSample]) -> Vec<GraphemeDescriptor> {...}

fn snapshot_grapheme(
    sample: &GraphemeSample,
    cluster_index: usize,
    cluster_text: &str,
    start: usize,
    end: usize,
    rope: &Rope,
    rope_text: &str,
) -> GraphemeDescriptor {...}

fn capture_leaf_snapshot(rope: &Rope, offset: usize) -> LeafSnapshot {...}

fn grapheme_context_from_text(text: &str, start: usize, end: usize) -> GraphemeContext {...}

fn clamp_prev_boundary_in_text(text: &str, offset: usize) -> usize {...}

fn clamp_next_boundary_in_text(text: &str, offset: usize) -> usize {...}

fn infer_fallback(contains_zwj: bool, crosses_leaf: bool, cluster_text: &str) -> bool {...}

fn compose_grapheme_tags(
    base: &[&str],
    contains_zwj: bool,
    is_ascii: bool,
    crosses_leaf: bool,
    cluster_text: &str,
) -> Vec<String> {...}

fn grapheme_samples() -> Vec<GraphemeSample> {...}

fn build_cross_leaf_flag_sample() -> Rope {...}

fn flag_leaf_left() -> String {...}

fn flag_leaf_right() -> String {...}

fn current_millis() -> u128 {...}

fn read_existing_generated_at(path: &Path) -> Option<u128> {...}
```

## xi-editor-ph7/rust/rope/src/serde_fixtures/search_spans.rs

```rust
use std::error::Error;
use std::path::{Path, PathBuf};
use std::time::{SystemTime, UNIX_EPOCH};

use regex::RegexBuilder;
use serde::{Deserialize, Serialize};

use crate::find::{find, CaseMatching};
use crate::rope::Rope;
use crate::tree::Cursor;

use super::detect_git_commit;
use super::snapshots::RangeSnapshot;

pub const SEARCH_SPANS_FILENAME: &str = "search_spans.json";
const SEARCH_SPANS_SCHEMA_VERSION: &str = "1.0.0";
const CONTEXT_WINDOW: usize = 40;
const DEFAULT_STYLE_ID: i32 = 7;
const DEFAULT_PRIORITY: i32 = 10;

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct SearchSpansFile {
    pub metadata: SearchSpansMetadata,
    pub search_cases: Vec<SearchCaseSnapshot>,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct SearchSpansMetadata {
    pub schema_version: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub rust_commit: Option<String>,
    pub generated_at_unix_millis: u128,
    pub case_count: usize,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct SearchCaseSnapshot {
    pub sample: String,
    pub query: String,
    pub is_regex: bool,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub regex_options: Option<String>,
    pub case_matching: CaseMatchingSnapshot,
    pub text_len: usize,
    pub hits: Vec<SearchHitSnapshot>,
    #[serde(skip_serializing_if = "Vec::is_empty")]
    pub span_windows: Vec<SpanSegmentSnapshot>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub notes: Option<String>,
}

#[derive(Clone, Copy, Debug, Serialize, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "snake_case")]
pub enum CaseMatchingSnapshot {
    Exact,
    CaseInsensitive,
}

impl From<CaseMatching> for CaseMatchingSnapshot {
    fn from(value: CaseMatching) -> Self {...}
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct SearchHitSnapshot {
    pub index: usize,
    pub range: RangeSnapshot,
    pub line: usize,
    pub context_before: String,
    pub context_after: String,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct SpanSegmentSnapshot {
    pub range: RangeSnapshot,
    pub style_id: i32,
    pub style_tag: String,
    pub priority: i32,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct SearchSpansExportReport {
    pub file_path: PathBuf,
    pub case_count: usize,
}

struct SearchSample {
    name: &'static str,
    text: &'static str,
    query: &'static str,
    is_regex: bool,
    regex_flags: &'static [&'static str],
    case_matching: CaseMatching,
    notes: &'static str,
}

pub fn export_search_spans(dir: &Path) -> Result<SearchSpansExportReport, Box<dyn Error>> {...}

fn build_search_payload(
    existing_timestamp: Option<u128>,
) -> Result<SearchSpansFile, Box<dyn Error>> {...}

fn build_case_snapshot(sample: SearchSample) -> Result<SearchCaseSnapshot, Box<dyn Error>> {...}

fn search_samples() -> Vec<SearchSample> {...}

fn build_regex(pattern: &str, flags: &[&str]) -> Result<regex::Regex, Box<dyn Error>> {...}

fn context_before(text: &str, end: usize, limit: usize) -> String {...}

fn context_after(text: &str, start: usize, limit: usize) -> String {...}

fn current_millis() -> u128 {...}

fn read_existing_generated_at(path: &Path) -> Option<u128> {...}
```

## xi-editor-ph7/rust/rope/src/serde_fixtures/snapshots.rs

```rust
use serde::{Deserialize, Serialize};

use crate::rope::RopeInfo;
use crate::tree::CursorDescriptor;
#[cfg(feature = "cursor_state")]
use crate::tree::CursorState;

#[derive(Clone, Debug, Serialize, Deserialize, PartialEq, Eq)]
pub struct RangeSnapshot {
    pub start: usize,
    pub end: usize,
}

#[derive(Clone, Debug, Serialize, Deserialize, PartialEq, Eq)]
pub struct PathFrameSnapshot {
    pub node_height: usize,
    pub node_len: usize,
    pub child_index: usize,
    pub child_offset: usize,
}

pub fn frames_from_descriptor(
    descriptor: &CursorDescriptor<RopeInfo, String>,
) -> Vec<PathFrameSnapshot> {...}

#[cfg(feature = "cursor_state")]
pub fn frames_from_state(state: &CursorState<RopeInfo, String>) -> Vec<PathFrameSnapshot> {...}
```

## xi-editor-ph7/rust/rope/src/serde_impls.rs

```rust
use std::fmt;
use std::str::FromStr;

use serde::de::{
    self, Deserialize, Deserializer, EnumAccess, MapAccess, SeqAccess, VariantAccess, Visitor,
};
use serde::ser::{Serialize, SerializeSeq, SerializeStruct, SerializeTupleVariant, Serializer};

use crate::tree::Node;
use crate::{Delta, DeltaElement, Rope, RopeInfo};

const DELTA_ELEMENT_VARIANTS: &[&str] = &["copy", "insert"];
const DELTA_FIELDS: &[&str] = &["els", "base_len"];

impl Serialize for Rope {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {...}
}

impl<'de> Deserialize<'de> for Rope {
    fn deserialize<D>(deserializer: D) -> Result<Self, D::Error>
    where
        D: Deserializer<'de>,
    {...}
}

struct RopeVisitor;

impl<'de> Visitor<'de> for RopeVisitor {
    type Value = Rope;

    fn expecting(&self, f: &mut fmt::Formatter) -> fmt::Result {...}

    fn visit_str<E>(self, s: &str) -> Result<Self::Value, E>
    where
        E: de::Error,
    {...}
}

impl Serialize for DeltaElement<RopeInfo, String> {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {...}
}

#[derive(Debug)]
enum DeltaElementVariant {
    Copy,
    Insert,
}

impl<'de> Deserialize<'de> for DeltaElementVariant {
    fn deserialize<D>(deserializer: D) -> Result<Self, D::Error>
    where
        D: Deserializer<'de>,
    {...}
}

struct CopyRangeVisitor;

impl<'de> Visitor<'de> for CopyRangeVisitor {
    type Value = (usize, usize);

    fn expecting(&self, f: &mut fmt::Formatter) -> fmt::Result {...}

    fn visit_seq<A>(self, mut seq: A) -> Result<Self::Value, A::Error>
    where
        A: SeqAccess<'de>,
    {...}
}

struct DeltaElementVisitor;

impl<'de> Visitor<'de> for DeltaElementVisitor {
    type Value = DeltaElement<RopeInfo, String>;

    fn expecting(&self, f: &mut fmt::Formatter) -> fmt::Result {...}

    fn visit_enum<A>(self, data: A) -> Result<Self::Value, A::Error>
    where
        A: EnumAccess<'de>,
    {...}
}

impl<'de> Deserialize<'de> for DeltaElement<RopeInfo, String> {
    fn deserialize<D>(deserializer: D) -> Result<Self, D::Error>
    where
        D: Deserializer<'de>,
    {...}
}

struct DeltaElementsSerialize<'a> {
    delta: &'a Delta<RopeInfo, String>,
}

impl Serialize for DeltaElementsSerialize<'_> {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {...}
}

impl Serialize for Delta<RopeInfo, String> {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {...}
}

enum DeltaField {
    Els,
    BaseLen,
}

impl<'de> Deserialize<'de> for DeltaField {
    fn deserialize<D>(deserializer: D) -> Result<Self, D::Error>
    where
        D: Deserializer<'de>,
    {...}
}

struct DeltaVisitor;

impl<'de> Visitor<'de> for DeltaVisitor {
    type Value = Delta<RopeInfo, String>;

    fn expecting(&self, f: &mut fmt::Formatter) -> fmt::Result {...}

    fn visit_map<M>(self, mut map: M) -> Result<Self::Value, M::Error>
    where
        M: MapAccess<'de>,
    {...}
}

impl<'de> Deserialize<'de> for Delta<RopeInfo, String> {
    fn deserialize<D>(deserializer: D) -> Result<Self, D::Error>
    where
        D: Deserializer<'de>,
    {...}
}
```

## xi-editor-ph7/rust/rope/src/spans.rs

```rust
use std::fmt;
use std::marker::PhantomData;
use std::mem;

use crate::delta::{Delta, DeltaElement, Transformer};
use crate::interval::{Interval, IntervalBounds};
use crate::tree::{Cursor, Leaf, Node, NodeInfo, TreeBuilder};

const MIN_LEAF: usize = 32;
const MAX_LEAF: usize = 64;

pub type Spans<T> = Node<SpansInfo<T>, SpansLeaf<T>>;

#[derive(Clone)]
pub struct Span<T: Clone> {
    iv: Interval,
    data: T,
}

#[derive(Clone)]
pub struct SpansLeaf<T: Clone> {
    len: usize, // measured in base units
    spans: Vec<Span<T>>,
}

// It would be preferable to derive Default.
// This would however require T to implement Default due to an issue in Rust.
// See: https://github.com/rust-lang/rust/issues/26925
impl<T: Clone> Default for SpansLeaf<T> {
    fn default() -> Self {...}
}

#[derive(Clone)]
pub struct SpansInfo<T> {
    n_spans: usize,
    iv: Interval,
    phantom: PhantomData<T>,
}

impl<T: Clone> Leaf for SpansLeaf<T> {
    fn len(&self) -> usize {...}

    fn is_ok_child(&self) -> bool {...}

    fn push_maybe_split(&mut self, other: &Self, iv: Interval) -> Option<Self> {...}
}

impl<T: Clone> NodeInfo<SpansLeaf<T>> for SpansInfo<T> {
    fn accumulate(&mut self, other: &Self) {...}

    fn compute_info(l: &SpansLeaf<T>) -> Self {...}
}

pub struct SpansBuilder<T: Clone> {
    b: TreeBuilder<SpansInfo<T>, SpansLeaf<T>>,
    leaf: SpansLeaf<T>,
    len: usize,
    total_len: usize,
}

impl<T: Clone> SpansBuilder<T> {
    pub fn new(total_len: usize) -> Self {...}

    // Precondition: spans must be added in nondecreasing start order.
    // Maybe take Span struct instead of separate iv, data args?
    pub fn add_span<IV: IntervalBounds>(&mut self, iv: IV, data: T) {...}

    // Would make slightly more implementation sense to take total_len as an argument
    // here, but that's not quite the usual builder pattern.
    pub fn build(mut self) -> Spans<T> {...}
}

pub struct SpanIter<'a, T: 'a + Clone> {
    cursor: Cursor<'a, SpansInfo<T>, SpansLeaf<T>>,
    ix: usize,
}

impl<T: Clone> Spans<T> {
    // Note: this implementation is not efficient for very large Spans objects, as it
    // traverses all spans linearly. A more sophisticated approach would be to traverse
    // the tree, and only delve into subtrees that are transformed.
    /// Perform operational transformation on a spans object intended to be edited into
    /// a sequence at the given offset.
    pub fn transform<N, L>(
        &self,
        base_start: usize,
        base_end: usize,
        xform: &mut Transformer<'_, N, L>,
    ) -> Self
    where
        N: NodeInfo<L>,
        L: Leaf,
    {...}

    /// Creates a new Spans instance by merging spans from `other` with `self`,
    /// using a closure to transform values.
    ///
    /// New spans are created from non-overlapping regions of existing spans,
    /// and by combining overlapping regions into new spans. In all cases,
    /// new values are generated by calling a closure that transforms the
    /// value of the existing span or spans.
    ///
    /// # Panics
    ///
    /// Panics if `self` and `other` have different lengths.
    ///
    pub fn merge<F, O>(&self, other: &Self, mut f: F) -> Spans<O>
    where
        F: FnMut(&T, Option<&T>) -> O,
        O: Clone,
    {...}

    // possible future: an iterator that takes an interval, so results are the same as
    // taking a subseq on the spans object. Would require specialized Cursor.
    pub fn iter(&self) -> SpanIter<'_, T> {...}

    /// Applies a generic delta to `self`, inserting empty spans for any
    /// added regions.
    ///
    /// This is intended to be used to keep spans up to date with a `Rope`
    /// as edits occur.
    pub fn apply_shape<M: NodeInfo<L>, L: Leaf>(&mut self, delta: &Delta<M, L>) {...}

    /// Deletes all spans that intersect with `interval` and that come after.
    pub fn delete_after(&mut self, interval: Interval) {...}
}

impl<T: Clone + fmt::Debug> fmt::Debug for Spans<T> {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {...}
}

impl<'a, T: Clone> Iterator for SpanIter<'a, T> {
    type Item = (Interval, &'a T);

    fn next(&mut self) -> Option<(Interval, &'a T)> {...}
}
```

## xi-editor-ph7/rust/rope/src/test_helpers.rs

```rust
use crate::delta::{self, Delta};
use crate::interval::Interval;
use crate::multiset::{Subset, SubsetBuilder};
use crate::rope::{Rope, RopeInfo};

/// Creates a `Subset` of `s` by scanning through `substr` and finding which
/// characters of `s` are missing from it in order. Returns a `Subset` which
/// when deleted from `s` yields `substr`.
pub fn find_deletions(substr: &str, s: &str) -> Subset {...}

impl Delta<RopeInfo, String> {
    pub fn apply_to_string(&self, s: &str) -> String {...}
}

impl PartialEq for Rope {
    fn eq(&self, other: &Rope) -> bool {...}
}

pub fn parse_subset(s: &str) -> Subset {...}

pub fn parse_subset_list(s: &str) -> Vec<Subset> {...}

pub fn debug_subsets(subsets: &[Subset]) {...}

pub fn parse_delta(s: &str) -> Delta<RopeInfo, String> {...}
```

## xi-editor-ph7/rust/rope/src/tree.rs

```rust
use std::cmp::{min, Ordering};
use std::marker::PhantomData;
use std::ops::Range;
use std::sync::Arc;

use smallvec::SmallVec;

use crate::interval::{Interval, IntervalBounds};

const MIN_CHILDREN: usize = 4;
const MAX_CHILDREN: usize = 8;

pub trait NodeInfo<L: Leaf>: Clone {
    /// An operator that combines info from two subtrees. It is intended
    /// (but not strictly enforced) that this operator be associative and
    /// obey an identity property. In mathematical terms, the accumulate
    /// method is the operation of a monoid.
    fn accumulate(&mut self, other: &Self);

    /// A mapping from a leaf into the info type. It is intended (but
    /// not strictly enforced) that applying the accumulate method to
    /// the info derived from two leaves gives the same result as
    /// deriving the info from the concatenation of the two leaves. In
    /// mathematical terms, the compute_info method is a monoid
    /// homomorphism.
    fn compute_info(_: &L) -> Self;

    /// The identity of the monoid. Need not be implemented because it
    /// can be computed from the leaf default.
    ///
    /// This is here to demonstrate that this is a monoid.
    fn identity() -> Self {...}

    /// The interval covered by the first `len` base units of this node. The
    /// default impl is sufficient for most types, but interval trees may need
    /// to override it.
    fn interval(&self, len: usize) -> Interval {...}
}

/// Provides conversions between the default metric of a node and other metrics.
///
/// Implementors supply the logic used by [`Node::count`] and
/// [`Node::count_base_units`] to translate offsets between metrics.
pub trait DefaultMetricProvider<L: Leaf>: NodeInfo<L> {
    fn convert_from_default<M: Metric<Self, L>>(node: &Node<Self, L>, offset: usize) -> usize;
    fn convert_to_default<M: Metric<Self, L>>(node: &Node<Self, L>, offset: usize) -> usize;
}

/// A trait for the leaves of trees of type [Node](struct.Node.html).
///
/// Two leafs can be concatenated using `push_maybe_split`.
pub trait Leaf: Sized + Clone + Default {
    /// Measurement of leaf in base units.
    /// A 'base unit' refers to the smallest discrete unit
    /// by which a given concrete type can be indexed.
    /// Concretely, for Rust's String type the base unit is the byte.
    fn len(&self) -> usize;

    /// Generally a minimum size requirement for leaves.
    fn is_ok_child(&self) -> bool;

    /// Combine the part `other` denoted by the `Interval` `iv` into `self`,
    /// optionly splitting off a new `Leaf` if `self` would have become too big.
    /// Returns either `None` if no splitting was needed, or `Some(rest)` if
    /// `rest` was split off.
    ///
    /// Interval is in "base units".  Generally implements a maximum size.
    ///
    /// # Invariants:
    /// - If one or the other input is empty, then no split.
    /// - If either input satisfies `is_ok_child`, then, on return, `self`
    ///   satisfies this, as does the optional split.
    fn push_maybe_split(&mut self, other: &Self, iv: Interval) -> Option<Self>;

    /// Same meaning as push_maybe_split starting from an empty
    /// leaf, but maybe can be implemented more efficiently?
    ///
    // TODO: remove if it doesn't pull its weight
    fn subseq(&self, iv: Interval) -> Self {...}
}

/// A b-tree node storing leaves at the bottom, and with info
/// retained at each node. It is implemented with atomic reference counting
/// and copy-on-write semantics, so an immutable clone is a very cheap
/// operation, and nodes can be shared across threads. Even so, it is
/// designed to be updated in place, with efficiency similar to a mutable
/// data structure, using uniqueness of reference count to detect when
/// this operation is safe.
///
/// When the leaf is a string, this is a rope data structure (a persistent
/// rope in functional programming jargon). However, it is not restricted
/// to strings, and it is expected to be the basis for a number of data
/// structures useful for text processing.
/// Internal helper that wraps `Arc<NodeBody>` and centralizes copy-on-write logic.
#[derive(Clone)]
pub(crate) struct SharedNode<N: NodeInfo<L>, L: Leaf> {
    arc: Arc<NodeBody<N, L>>,
}

#[derive(Clone)]
pub struct Node<N: NodeInfo<L>, L: Leaf> {
    shared: SharedNode<N, L>,
}

#[derive(Clone)]
pub(crate) struct NodeBody<N: NodeInfo<L>, L: Leaf> {
    height: usize,
    len: usize,
    info: N,
    val: NodeVal<N, L>,
}

#[derive(Clone)]
enum NodeVal<N: NodeInfo<L>, L: Leaf> {
    Leaf(L),
    Internal(Vec<Node<N, L>>),
}

impl<N: NodeInfo<L>, L: Leaf> SharedNode<N, L> {
    #[inline]
    pub(crate) fn new(body: NodeBody<N, L>) -> Self {...}

    #[inline]
    #[allow(dead_code)]
    pub(crate) fn from_arc(arc: Arc<NodeBody<N, L>>) -> Self {...}

    #[inline]
    #[allow(dead_code)]
    pub(crate) fn into_arc(self) -> Arc<NodeBody<N, L>> {...}

    #[inline]
    #[allow(dead_code)]
    pub(crate) fn arc(&self) -> &Arc<NodeBody<N, L>> {...}

    #[inline]
    pub(crate) fn body(&self) -> &NodeBody<N, L> {...}

    #[inline]
    pub(crate) fn ensure_unique(&mut self) -> &mut NodeBody<N, L> {...}

    #[inline]
    pub(crate) fn ptr_eq(&self, other: &Self) -> bool {...}

    pub(crate) fn from_children(children: Vec<Node<N, L>>) -> Self {...}

    pub(crate) fn clone_with_children(&self, children: &[Node<N, L>]) -> SharedNode<N, L> {...}

    pub(crate) fn replace_child_range(&mut self, range: Range<usize>, replacements: &[Node<N, L>]) {...}

    fn refresh_len_info(body: &mut NodeBody<N, L>) {...}
}

// also consider making Metric a newtype for usize, so type system can
// help separate metrics

/// A trait for quickly processing attributes of a
/// [NodeInfo](struct.NodeInfo.html).
///
/// For the conceptual background see the
/// [blog post, Rope science, part 2: metrics](https://github.com/google/xi-editor/blob/master/docs/docs/rope_science_02.md).
pub trait Metric<N: NodeInfo<L>, L: Leaf> {
    /// Return the size of the
    /// leaf as measured by this metric.
    ///
    /// The usize argument is the total size/length of the node, in base units.
    ///
    /// # Examples
    /// For the [LinesMetric](../rope/struct.LinesMetric.html), this gives the number of
    /// lines in string contained in the leaf. For the
    /// [BaseMetric](../rope/struct.BaseMetric.html), this gives the size of the string
    /// in uft8 code units, that is, bytes.
    ///
    fn measure(info: &N, len: usize) -> usize;

    /// Returns the smallest offset, in base units, for an offset in measured units.
    ///
    /// # Invariants:
    ///
    /// - `from_base_units(to_base_units(x)) == x` is True for valid `x`
    fn to_base_units(l: &L, in_measured_units: usize) -> usize;

    /// Returns the smallest offset in measured units corresponding to an offset in base units.
    ///
    /// # Invariants:
    ///
    /// - `from_base_units(to_base_units(x)) == x` is True for valid `x`
    fn from_base_units(l: &L, in_base_units: usize) -> usize;

    /// Return whether the offset in base units is a boundary of this metric.
    /// If a boundary is at end of a leaf then this method must return true.
    /// However, a boundary at the beginning of a leaf is optional
    /// (the previous leaf will be queried).
    fn is_boundary(l: &L, offset: usize) -> bool;

    /// Returns the index of the boundary directly preceding offset,
    /// or None if no such boundary exists. Input and result are in base units.
    fn prev(l: &L, offset: usize) -> Option<usize>;

    /// Returns the index of the first boundary for which index > offset,
    /// or None if no such boundary exists. Input and result are in base units.
    fn next(l: &L, offset: usize) -> Option<usize>;

    /// Returns true if the measured units in this metric can span multiple
    /// leaves.  As an example, in a metric that measures lines in a rope, a
    /// line may start in one leaf and end in another; however in a metric
    /// measuring bytes, storage of a single byte cannot extend across leaves.
    fn can_fragment() -> bool;
}

impl<N: NodeInfo<L>, L: Leaf> Node<N, L> {
    #[inline]
    fn from_shared(shared: SharedNode<N, L>) -> Self {...}

    #[inline]
    pub(crate) fn shared(&self) -> &SharedNode<N, L> {...}

    #[inline]
    pub(crate) fn shared_mut(&mut self) -> &mut SharedNode<N, L> {...}

    #[inline]
    pub(crate) fn body(&self) -> &NodeBody<N, L> {...}

    pub fn from_leaf(l: L) -> Node<N, L> {...}

    /// Create a node from a vec of nodes.
    ///
    /// The input must satisfy the following balancing requirements:
    /// * The length of `nodes` must be <= MAX_CHILDREN and > 1.
    /// * All the nodes are the same height.
    /// * All the nodes must satisfy is_ok_child.
    fn from_nodes(nodes: Vec<Node<N, L>>) -> Node<N, L> {...}

    pub fn len(&self) -> usize {...}

    pub fn is_empty(&self) -> bool {...}

    /// Returns `true` if these two `Node`s share the same underlying data.
    ///
    /// This is principally intended to be used by the druid crate, without needing
    /// to actually add a feature and implement druid's `Data` trait.
    pub fn ptr_eq(&self, other: &Self) -> bool {...}

    fn height(&self) -> usize {...}

    fn is_leaf(&self) -> bool {...}

    fn interval(&self) -> Interval {...}

    fn get_children(&self) -> &[Node<N, L>] {...}

    fn get_leaf(&self) -> &L {...}

    /// Call a callback with a mutable reference to a leaf.
    ///
    /// This clones the leaf if the reference is shared. It also recomputes
    /// length and info after the leaf is mutated.
    fn with_leaf_mut<T>(&mut self, f: impl FnOnce(&mut L) -> T) -> T {...}

    fn is_ok_child(&self) -> bool {...}

    fn merge_nodes(children1: &[Node<N, L>], children2: &[Node<N, L>]) -> Node<N, L> {...}

    fn merge_leaves(mut rope1: Node<N, L>, rope2: Node<N, L>) -> Node<N, L> {...}

    pub fn concat(rope1: Node<N, L>, rope2: Node<N, L>) -> Node<N, L> {...}

    pub fn measure<M: Metric<N, L>>(&self) -> usize {...}

    pub fn subseq<T: IntervalBounds>(&self, iv: T) -> Node<N, L> {...}

    pub fn edit<T, IV>(&mut self, iv: IV, new: T)
    where
        T: Into<Node<N, L>>,
        IV: IntervalBounds,
    {...}

    // doesn't deal with endpoint, handle that specially if you need it
    pub fn convert_metrics<M1: Metric<N, L>, M2: Metric<N, L>>(&self, mut m1: usize) -> usize {...}
}

impl<N: DefaultMetricProvider<L>, L: Leaf> Node<N, L> {
    /// Measures the length of the text bounded by the default metric offset using another metric.
    ///
    /// # Examples
    /// ```
    /// use crate::xi_rope::{Rope, LinesMetric};
    ///
    /// // the default metric of Rope is BaseMetric (aka number of bytes)
    /// let my_rope = Rope::from("first line \n second line \n");
    ///
    /// // count the number of lines in my_rope
    /// let num_lines = my_rope.count::<LinesMetric>(my_rope.len());
    /// assert_eq!(2, num_lines);
    /// ```
    pub fn count<M: Metric<N, L>>(&self, offset: usize) -> usize {...}

    /// Measures the length of the text bounded by another metric using the default metric.
    ///
    /// # Examples
    /// ```
    /// use crate::xi_rope::{Rope, LinesMetric};
    ///
    /// // the default metric of Rope is BaseMetric (aka number of bytes)
    /// let my_rope = Rope::from("first line \n second line \n");
    ///
    /// // get the byte offset of the line at index 1
    /// let byte_offset = my_rope.count_base_units::<LinesMetric>(1);
    /// assert_eq!(12, byte_offset);
    /// ```
    pub fn count_base_units<M: Metric<N, L>>(&self, offset: usize) -> usize {...}
}

impl<N: NodeInfo<L>, L: Leaf> Default for Node<N, L> {
    fn default() -> Node<N, L> {...}
}

#[cfg(feature = "tree_builder_slice_trace")]
#[derive(Clone, Debug, PartialEq, Eq)]
pub enum TreeBuilderEventKind {
    PushFrame,
    ExtendFrame,
    MergePop { merged_children: usize },
    LeafSlice { interval: Interval },
    EnterChild { requested: Interval, translated: Interval },
}

#[cfg(feature = "tree_builder_slice_trace")]
#[derive(Clone, Debug, PartialEq, Eq)]
pub struct TreeBuilderEvent {
    pub kind: TreeBuilderEventKind,
    pub depth: usize,
    pub node_height: usize,
    pub node_len: usize,
    pub node_ptr: usize,
    pub reuse: bool,
}

#[cfg(feature = "tree_builder_slice_trace")]
pub trait TreeBuilderTracer<N: NodeInfo<L>, L: Leaf> {
    fn record(&mut self, event: TreeBuilderEvent);
}

#[cfg(feature = "tree_builder_slice_trace")]
#[derive(Default)]
pub struct NullTreeBuilderTracer;

#[cfg(feature = "tree_builder_slice_trace")]
impl<N: NodeInfo<L>, L: Leaf> TreeBuilderTracer<N, L> for NullTreeBuilderTracer {
    fn record(&mut self, _event: TreeBuilderEvent) {...}
}

/// A builder for creating new trees.
pub struct TreeBuilder<N: NodeInfo<L>, L: Leaf> {
    // A stack of partially built trees. These are kept in order of
    // strictly descending height, and all vectors have a length less
    // than MAX_CHILDREN and greater than zero.
    //
    // In addition, there is a balancing invariant: for each vector
    // of length greater than one, all elements satisfy `is_ok_child`.
    stack: Vec<Vec<Node<N, L>>>,
    #[cfg(feature = "tree_builder_slice_trace")]
    tracer: Option<Box<dyn TreeBuilderTracer<N, L>>>,
}

impl<N: NodeInfo<L>, L: Leaf> TreeBuilder<N, L> {
    /// A new, empty builder.
    pub fn new() -> TreeBuilder<N, L> {...}

    #[cfg(feature = "tree_builder_slice_trace")]
    /// Create a builder configured with a tracer.
    pub fn with_tracer(tracer: Box<dyn TreeBuilderTracer<N, L>>) -> TreeBuilder<N, L> {...}

    #[cfg(feature = "tree_builder_slice_trace")]
    /// Replace the tracer used by this builder.
    pub fn set_tracer(&mut self, tracer: Option<Box<dyn TreeBuilderTracer<N, L>>>) {...}

    /// Append a node to the tree being built.
    pub fn push(&mut self, n: Node<N, L>) {...}

    fn push_with_hint(&mut self, mut n: Node<N, L>, reuse_hint: bool) {...}

    #[cfg(feature = "tree_builder_slice_trace")]
    fn trace_push_frame(
        &mut self,
        node_height: usize,
        node_len: usize,
        node_ptr: usize,
        reuse: bool,
    ) {...}

    #[cfg(feature = "tree_builder_slice_trace")]
    fn trace_extend_frame(
        &mut self,
        node_height: usize,
        node_len: usize,
        node_ptr: usize,
        reuse: bool,
    ) {...}

    #[cfg(feature = "tree_builder_slice_trace")]
    fn trace_merge_pop(&mut self, node: &Node<N, L>, merged_children: usize) {...}

    #[cfg(feature = "tree_builder_slice_trace")]
    fn trace_leaf_slice(&mut self, node: &Node<N, L>, interval: Interval) {...}

    #[cfg(feature = "tree_builder_slice_trace")]
    fn trace_enter_child(&mut self, child: &Node<N, L>, requested: Interval, translated: Interval) {...}

    #[cfg(feature = "tree_builder_slice_trace")]
    fn trace_event(&mut self, event: TreeBuilderEvent) {...}

    #[cfg(feature = "tree_builder_slice_trace")]
    fn node_identity(node: &Node<N, L>) -> usize {...}

    /// Push a subsequence of a rope.
    ///
    /// Pushes the subsequence of another tree `n` defined by the interval `iv`
    /// onto the builder.
    ///
    /// This is intended as an efficient operation. It is equivalent to taking
    /// the subsequence of `n` and pushing that, but attempts to minimize the
    /// allocation of intermediate results.
    pub fn push_slice(&mut self, n: &Node<N, L>, iv: Interval) {...}

    /// Append a sequence of leaves.
    pub fn push_leaves(&mut self, leaves: impl IntoIterator<Item = L>) {...}

    /// Append a single leaf.
    pub fn push_leaf(&mut self, l: L) {...}

    /// Append a slice of a single leaf.
    pub fn push_leaf_slice(&mut self, l: &L, iv: Interval) {...}

    /// Build the final tree.
    ///
    /// The tree is the concatenation of all the nodes and leaves that have been pushed
    /// on the builder, in order.
    pub fn build(mut self) -> Node<N, L> {...}

    /// Pop the last vec-of-nodes off the stack, resulting in a node.
    fn pop(&mut self) -> Node<N, L> {...}
}

const CURSOR_CACHE_SIZE: usize = 4;

/// A cached frame representing the relationship between a parent node and the
/// child traversed by the cursor when descending the tree.
///
/// Frames are stored from root to leaf and keep enough information to rebuild
/// cached offsets without walking sibling lengths again.
#[derive(Clone)]
pub struct PathFrame<N: NodeInfo<L>, L: Leaf> {
    node: Arc<NodeBody<N, L>>,
    child_index: usize,
    child_offset: usize,
}

impl<N: NodeInfo<L>, L: Leaf> PathFrame<N, L> {
    fn new(node: &Node<N, L>, child_index: usize, child_offset: usize) -> Self {...}

    pub fn ptr_eq(&self, other: &Node<N, L>) -> bool {...}

    pub fn child_index(&self) -> usize {...}

    pub fn child_offset(&self) -> usize {...}

    pub fn node_height(&self) -> usize {...}

    pub fn node_len(&self) -> usize {...}
}

/// A borrow-free snapshot of a cursor's cached state.
///
/// The descriptor can be used to rebuild a [`Cursor`] at the same position, as
/// long as the underlying nodes are still valid (checked with `Arc::ptr_eq`).
pub struct CursorDescriptor<N: NodeInfo<L>, L: Leaf> {
    position: usize,
    offset_of_leaf: usize,
    leaf: Option<Arc<NodeBody<N, L>>>,
    frames: SmallVec<[PathFrame<N, L>; CURSOR_CACHE_SIZE]>,
}

impl<N: NodeInfo<L>, L: Leaf> CursorDescriptor<N, L> {
    fn new_invalid(position: usize) -> Self {...}

    fn new(
        position: usize,
        offset_of_leaf: usize,
        leaf: Arc<NodeBody<N, L>>,
        frames: SmallVec<[PathFrame<N, L>; CURSOR_CACHE_SIZE]>,
    ) -> Self {...}

    /// Returns the cached depth (number of parent frames) stored in the descriptor.
    pub fn depth(&self) -> usize {...}

    /// Returns whether the descriptor holds a valid leaf reference.
    pub fn is_valid(&self) -> bool {...}

    /// Returns the absolute cursor position captured by this descriptor.
    pub fn position(&self) -> usize {...}

    /// Returns the absolute offset of the current leaf within the tree.
    pub fn offset_of_leaf(&self) -> usize {...}

    /// Returns the frames describing the cached path from root to leaf.
    pub fn frames(&self) -> &[PathFrame<N, L>] {...}

    /// Returns the length of the cached leaf, if the descriptor is valid.
    pub fn leaf_len(&self) -> Option<usize> {...}

    /// Restores a [`Cursor`] from this descriptor if the cached nodes still belong to `root`.
    pub fn restore<'a>(&self, root: &'a Node<N, L>) -> Option<Cursor<'a, N, L>> {...}
}

#[cfg(feature = "cursor_state")]
#[derive(Clone)]
pub struct CursorState<N: NodeInfo<L>, L: Leaf> {
    position: usize,
    offset_of_leaf: usize,
    leaf: Option<Arc<NodeBody<N, L>>>,
    frames: SmallVec<[PathFrame<N, L>; CURSOR_CACHE_SIZE]>,
}

/// A data structure for traversing boundaries in a tree.
///
/// It is designed to be efficient both for random access and for iteration. The
/// cursor itself is agnostic to which [`Metric`] is used to determine boundaries, but
/// the methods to find boundaries are parametrized on the [`Metric`].
///
/// A cursor can be valid or invalid. It is always valid when created or after
/// [`set`](#method.set) is called, and becomes invalid after [`prev`](#method.prev)
/// or [`next`](#method.next) fails to find a boundary.
///
/// [`Metric`]: struct.Metric.html
pub struct Cursor<'a, N: NodeInfo<L> + 'a, L: Leaf> {
    /// The tree being traversed by this cursor.
    root: &'a Node<N, L>,
    /// The current position of the cursor.
    ///
    /// It is always less than or equal to the tree length.
    position: usize,
    /// The cache holds the tail of the path from the root to the current leaf.
    ///
    /// Each entry is a reference to the parent node and the index of the child. It
    /// is stored bottom-up; `cache[0]` is the parent of the leaf and the index of
    /// the leaf within that parent.
    ///
    /// The main motivation for this being a fixed-size array is to keep the cursor
    /// an allocation-free data structure.
    cache: [Option<(&'a Node<N, L>, usize)>; CURSOR_CACHE_SIZE],
    /// The leaf containing the current position, when the cursor is valid.
    ///
    /// The position is only at the end of the leaf when it is at the end of the tree.
    leaf: Option<&'a L>,
    /// The offset of `leaf` within the tree.
    offset_of_leaf: usize,
    #[cfg(feature = "cursor_state")]
    state: CursorState<N, L>,
}

impl<'a, N: NodeInfo<L>, L: Leaf> Cursor<'a, N, L> {
    /// Create a new cursor at the given position.
    pub fn new(n: &'a Node<N, L>, position: usize) -> Cursor<'a, N, L> {...}

    /// The length of the tree.
    pub fn total_len(&self) -> usize {...}

    /// Return a reference to the root node of the tree.
    pub fn root(&self) -> &'a Node<N, L> {...}

    #[cfg(feature = "cursor_state")]
    pub fn state(&self) -> CursorState<N, L> {...}

    /// Get the current leaf of the cursor.
    ///
    /// If the cursor is valid, returns the leaf containing the current position,
    /// and the offset of the current position within the leaf. That offset is equal
    /// to the leaf length only at the end, otherwise it is less than the leaf length.
    pub fn get_leaf(&self) -> Option<(&'a L, usize)> {...}

    /// Set the position of the cursor.
    ///
    /// The cursor is valid after this call.
    ///
    /// Precondition: `position` is less than or equal to the length of the tree.
    pub fn set(&mut self, position: usize) {...}

    /// Get the position of the cursor.
    pub fn pos(&self) -> usize {...}

    /// Creates a [`CursorDescriptor`] snapshot of the current cursor state.
    ///
    /// The descriptor owns all cached path information, allowing the cursor to
    /// be reconstructed later without holding borrows into the tree. When the
    /// cursor is invalid, the returned descriptor will also be marked invalid
    /// and `restore`/`apply_descriptor` will return failure.
    pub fn to_descriptor(&self) -> CursorDescriptor<N, L> {...}

    /// Attempts to repopulate the cursor's cache from a descriptor.
    ///
    /// Returns `true` if the descriptor was still valid for the current tree and
    /// the cursor was updated. On failure the cursor is left unchanged so the
    /// caller can fall back to a fresh descent.
    pub fn apply_descriptor(&mut self, descriptor: &CursorDescriptor<N, L>) -> bool {...}

    /// Determine whether the current position is a boundary.
    ///
    /// Note: the beginning and end of the tree may or may not be boundaries, depending on the
    /// metric. If the metric is not `can_fragment`, then they always are.
    pub fn is_boundary<M: Metric<N, L>>(&mut self) -> bool {...}

    /// Moves the cursor to the previous boundary.
    ///
    /// When there is no previous boundary, returns `None` and the cursor becomes invalid.
    ///
    /// Return value: the position of the boundary, if it exists.
    pub fn prev<M: Metric<N, L>>(&mut self) -> Option<usize> {...}

    /// Moves the cursor to the next boundary.
    ///
    /// When there is no next boundary, returns `None` and the cursor becomes invalid.
    ///
    /// Return value: the position of the boundary, if it exists.
    pub fn next<M: Metric<N, L>>(&mut self) -> Option<usize> {...}

    /// Returns the current position if it is a boundary in this [`Metric`],
    /// else behaves like [`next`](#method.next).
    ///
    /// [`Metric`]: struct.Metric.html
    pub fn at_or_next<M: Metric<N, L>>(&mut self) -> Option<usize> {...}

    /// Returns the current position if it is a boundary in this [`Metric`],
    /// else behaves like [`prev`](#method.prev).
    ///
    /// [`Metric`]: struct.Metric.html
    pub fn at_or_prev<M: Metric<N, L>>(&mut self) -> Option<usize> {...}

    /// Returns an iterator with this cursor over the given [`Metric`].
    ///
    /// # Examples:
    ///
    /// ```
    /// # use xi_rope::{Cursor, LinesMetric, Rope};
    /// #
    /// let text: Rope = "one line\ntwo line\nred line\nblue".into();
    /// let mut cursor = Cursor::new(&text, 0);
    /// let line_offsets = cursor.iter::<LinesMetric>().collect::<Vec<_>>();
    /// assert_eq!(line_offsets, vec![9, 18, 27]);
    ///
    /// ```
    /// [`Metric`]: struct.Metric.html
    pub fn iter<'c, M: Metric<N, L>>(&'c mut self) -> CursorIter<'c, 'a, N, L, M> {...}

    /// Tries to find the last boundary in the leaf the cursor is currently in.
    ///
    /// If the last boundary is at the end of the leaf, it is only counted if
    /// it is less than `orig_pos`.
    #[inline]
    fn last_inside_leaf<M: Metric<N, L>>(&mut self, orig_pos: usize) -> Option<usize> {...}

    /// Tries to find the next boundary in the leaf the cursor is currently in.
    #[inline]
    fn next_inside_leaf<M: Metric<N, L>>(&mut self) -> Option<usize> {...}

    /// Move to beginning of next leaf.
    ///
    /// Return value: same as [`get_leaf`](#method.get_leaf).
    pub fn next_leaf(&mut self) -> Option<(&'a L, usize)> {...}

    /// Move to beginning of previous leaf.
    ///
    /// Return value: same as [`get_leaf`](#method.get_leaf).
    pub fn prev_leaf(&mut self) -> Option<(&'a L, usize)> {...}

    /// Go to the leaf containing the current position.
    ///
    /// Sets `leaf` to the leaf containing `position`, and updates `cache` and
    /// `offset_of_leaf` to be consistent.
    fn descend(&mut self) {...}

    /// Returns the measure at the beginning of the leaf containing `pos`.
    ///
    /// This method is O(log n) no matter the current cursor state.
    fn measure_leaf<M: Metric<N, L>>(&self, mut pos: usize) -> usize {...}

    /// Find the leaf having the given measure.
    ///
    /// This function sets `self.position` to the beginning of the leaf
    /// containing the smallest offset with the given metric, and also updates
    /// state as if [`descend`](#method.descend) was called.
    ///
    /// If `measure` is greater than the measure of the whole tree, then moves
    /// to the last node.
    fn descend_metric<M: Metric<N, L>>(&mut self, mut measure: usize) {...}
    #[inline]
    fn set_leaf_from_node(&mut self, leaf_node: &'a Node<N, L>, offset: usize) {...}

    #[cfg(feature = "cursor_state")]
    fn rebuild_state(&mut self) {...}

    #[cfg(feature = "cursor_state")]
    fn update_state_position(&mut self) {...}

    #[cfg(feature = "cursor_state")]
    fn invalidate_state(&mut self) {...}
}

/// An iterator generated by a [`Cursor`], for some [`Metric`].
///
/// [`Cursor`]: struct.Cursor.html
/// [`Metric`]: struct.Metric.html
pub struct CursorIter<'c, 'a: 'c, N: NodeInfo<L> + 'a, L: Leaf, M: Metric<N, L> + 'a> {
    cursor: &'c mut Cursor<'a, N, L>,
    _metric: PhantomData<&'a M>,
}

impl<'c, 'a, N, L, M> Iterator for CursorIter<'c, 'a, N, L, M>
where
    N: NodeInfo<L> + 'a,
    L: Leaf,
    M: Metric<N, L> + 'a,
{
    type Item = usize;

    fn next(&mut self) -> Option<usize> {...}
}

impl<'c, 'a, N, L, M> CursorIter<'c, 'a, N, L, M>
where
    N: NodeInfo<L> + 'a,
    L: Leaf,
    M: Metric<N, L> + 'a,
{
    /// Returns the current position of the underlying [`Cursor`].
    ///
    /// [`Cursor`]: struct.Cursor.html
    pub fn pos(&self) -> usize {...}
}

#[cfg(feature = "cursor_state")]
impl<N: NodeInfo<L>, L: Leaf> CursorState<N, L> {
    fn new(
        position: usize,
        offset_of_leaf: usize,
        leaf: Arc<NodeBody<N, L>>,
        frames: SmallVec<[PathFrame<N, L>; CURSOR_CACHE_SIZE]>,
    ) -> Self {...}

    fn new_invalid(position: usize, offset_of_leaf: usize) -> Self {...}

    pub fn is_valid(&self) -> bool {...}

    pub fn position(&self) -> usize {...}

    pub fn offset_of_leaf(&self) -> usize {...}

    pub fn frames(&self) -> &[PathFrame<N, L>] {...}

    pub fn to_descriptor(&self) -> CursorDescriptor<N, L> {...}

    pub fn from_descriptor(descriptor: &CursorDescriptor<N, L>) -> Self {...}

    pub fn restore<'a>(&self, root: &'a Node<N, L>) -> Option<Cursor<'a, N, L>> {...}

    pub fn from_cursor<'a>(cursor: &Cursor<'a, N, L>) -> Self {...}

    fn set_position(&mut self, position: usize) {...}

    fn invalidate(&mut self, position: usize, offset_of_leaf: usize) {...}
}

type CursorDescriptorComponents<N, L> =
    (SmallVec<[PathFrame<N, L>; CURSOR_CACHE_SIZE]>, Arc<NodeBody<N, L>>, usize, usize);

fn build_descriptor_components<N: NodeInfo<L>, L: Leaf>(
    root: &Node<N, L>,
    position: usize,
) -> CursorDescriptorComponents<N, L> {...}

fn clone_node_arc<N: NodeInfo<L>, L: Leaf>(node: &Node<N, L>) -> Arc<NodeBody<N, L>> {...}
```

## xi-editor-ph7/rust/rope/tests/breaks_descriptors.rs

```rust
#[cfg(feature = "serde")]
mod serde_breaks_export {
    use std::fs;
    use std::process::Command;

    use tempfile::tempdir;
    use xi_rope::serde_fixtures::breaks_descriptors::{
        BreakMetricKind, BreaksDescriptorFile, BREAKS_DESCRIPTOR_FILENAME,
    };

    }
```

## xi-editor-ph7/rust/rope/tests/chunk_descriptor.rs

```rust
#[cfg(feature = "serde")]
mod serde_chunk_export {
    use std::fs;
    use std::process::Command;

    use tempfile::tempdir;
    use xi_rope::serde_fixtures::chunk_descriptors::{
        ChunkDescriptorFile, LineEndingKind, CHUNK_DESCRIPTOR_FILENAME,
    };

    }
```

## xi-editor-ph7/rust/rope/tests/cursor_descriptor.rs

```rust
use std::ptr;

use xi_rope::tree::{Cursor, TreeBuilder};
use xi_rope::{LinesMetric, Rope, RopeInfo};

fn build_deep_rope() -> Rope {...}

#[cfg(feature = "serde")]
#[cfg(feature = "cursor_state")]
mod cursor_state_tests {
    use super::*;
    use std::any::type_name;

    use xi_rope::rope::{BaseMetric, Utf16CodeUnitsMetric};
    use xi_rope::tree::{CursorState, Metric};

    const SAMPLE_TEXT: &str = "zero\none\u{1F600}two\nthree\u{1F4A9}four\nlast line";

    fn sample_rope() -> Rope {...}

    fn collect_test_positions<M>(rope: &Rope) -> Vec<usize>
    where
        M: Metric<RopeInfo, String>,
    {...}

    fn assert_state_navigation_parity<M>(rope: &Rope, metric_name: &str)
    where
        M: Metric<RopeInfo, String>,
    {...}

    }
```

## xi-editor-ph7/rust/rope/tests/diff_regions.rs

```rust
#[cfg(feature = "serde")]
mod serde_diff_export {
    use std::fs;
    use std::path::PathBuf;
    use std::process::Command;

    use tempfile::{tempdir_in, TempDir};
    use xi_rope::serde_fixtures::diff_regions::{
        DiffOpKind, DiffRegionsFile, DIFF_REGIONS_FILENAME,
    };

    fn workspace_root() -> PathBuf {...}

    fn temp_output_dir() -> (TempDir, PathBuf) {...}

    }
```

## xi-editor-ph7/rust/rope/tests/grapheme_descriptor.rs

```rust
#[cfg(feature = "serde")]
mod serde_grapheme_export {
    use std::fs;
    use std::process::Command;

    use tempfile::tempdir;
    use xi_rope::serde_fixtures::grapheme_descriptors::{
        GraphemeDescriptorFile, GRAPHEME_DESCRIPTOR_FILENAME,
    };

    }
```

## xi-editor-ph7/rust/rope/tests/search_spans.rs

```rust
#[cfg(feature = "serde")]
mod serde_search_export {
    use std::fs;
    use std::process::Command;

    use tempfile::tempdir;
    use xi_rope::serde_fixtures::search_spans::{SearchSpansFile, SEARCH_SPANS_FILENAME};

    }
```

## xi-editor-ph7/rust/rope/tests/tree_builder_slice_trace.rs

```rust
#![cfg(feature = "tree_builder_slice_trace")]

use std::cell::RefCell;
use std::rc::Rc;

use xi_rope::tree::{TreeBuilder, TreeBuilderEvent, TreeBuilderEventKind, TreeBuilderTracer};
use xi_rope::{Interval, Rope, RopeInfo};

struct RecordingTracer {
    events: Rc<RefCell<Vec<TreeBuilderEvent>>>,
}

impl RecordingTracer {
    fn new(store: Rc<RefCell<Vec<TreeBuilderEvent>>>) -> Self {...}
}

impl TreeBuilderTracer<RopeInfo, String> for RecordingTracer {
    fn record(&mut self, event: TreeBuilderEvent) {...}
}
```

