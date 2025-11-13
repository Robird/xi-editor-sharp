## xi-editor-ph7/rust/rope/examples/ropetoy.rs

```rust
extern crate xi_rope;

use xi_rope::Rope;

fn main() {...}
```

## xi-editor-ph7/rust/rope/src/breaks.rs

```rust
use crate::interval::Interval;
use crate::tree::{DefaultMetricProvider, Leaf, Metric, Node, NodeInfo, TreeBuilder};
use std::cmp::min;
use std::mem;

/// A set of indexes. A motivating use is storing line breaks.
pub type Breaks = Node<BreaksInfo>;

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

impl NodeInfo for BreaksInfo {
    type L = BreaksLeaf;

    fn accumulate(&mut self, other: &Self) {...}

    fn compute_info(l: &BreaksLeaf) -> BreaksInfo {...}
}

impl DefaultMetricProvider for BreaksInfo {
    fn convert_from_default<M: Metric<Self>>(node: &Node<Self>, offset: usize) -> usize {...}

    fn convert_to_default<M: Metric<Self>>(node: &Node<Self>, offset: usize) -> usize {...}
}

impl BreaksLeaf {
    /// Exposed for testing.
    #[doc(hidden)]
    pub fn get_data_cloned(&self) -> Vec<usize> {...}
}

#[derive(Copy, Clone)]
pub struct BreaksMetric(());

impl Metric<BreaksInfo> for BreaksMetric {
    fn measure(info: &BreaksInfo, _: usize) -> usize {...}

    fn to_base_units(l: &BreaksLeaf, in_measured_units: usize) -> usize {...}

    fn from_base_units(l: &BreaksLeaf, in_base_units: usize) -> usize {...}

    fn is_boundary(l: &BreaksLeaf, offset: usize) -> bool {...}

    fn prev(l: &BreaksLeaf, offset: usize) -> Option<usize> {...}

    fn next(l: &BreaksLeaf, offset: usize) -> Option<usize> {...}

    fn can_fragment() -> bool {...}
}

#[derive(Copy, Clone)]
pub struct BreaksBaseMetric(());

impl Metric<BreaksInfo> for BreaksBaseMetric {
    fn measure(_: &BreaksInfo, len: usize) -> usize {...}

    fn to_base_units(_: &BreaksLeaf, in_measured_units: usize) -> usize {...}

    fn from_base_units(_: &BreaksLeaf, in_base_units: usize) -> usize {...}

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
}

pub struct BreakBuilder {
    b: TreeBuilder<BreaksInfo>,
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
    base: Cursor<'a, RopeInfo>,
    target: Cursor<'a, RopeInfo>,
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
use crate::tree::{Node, NodeInfo, TreeBuilder};
use std::cmp::min;
use std::fmt;
use std::ops::Deref;
use std::slice;

#[derive(Clone)]
pub enum DeltaElement<N: NodeInfo> {
    /// Represents a range of text in the base document. Includes beginning, excludes end.
    Copy(usize, usize), // note: for now, we lose open/closed info at interval endpoints
    Insert(Node<N>),
}

/// Represents changes to a document by describing the new document as a
/// sequence of sections copied from the old document and of new inserted
/// text. Deletions are represented by gaps in the ranges copied from the old
/// document.
///
/// For example, Editing "abcd" into "acde" could be represented as:
/// `[Copy(0,1),Copy(2,4),Insert("e")]`
#[derive(Clone)]
pub struct Delta<N: NodeInfo> {
    pub els: Vec<DeltaElement<N>>,
    pub base_len: usize,
}

/// A struct marking that a Delta contains only insertions. That is, it copies
/// all of the old document in the same order. It has a `Deref` impl so all
/// normal `Delta` methods can also be used on it.
#[derive(Clone)]
pub struct InsertDelta<N: NodeInfo>(Delta<N>);

impl<N: NodeInfo> Delta<N> {
    pub fn simple_edit<T: IntervalBounds>(interval: T, rope: Node<N>, base_len: usize) -> Delta<N> {...}

    /// If this delta represents a simple insertion, returns the inserted node.
    pub fn as_simple_insert(&self) -> Option<&Node<N>> {...}

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
    pub fn apply(&self, base: &Node<N>) -> Node<N> {...}

    /// Factor the delta into an insert-only delta and a subset representing deletions.
    /// Applying the insert then the delete yields the same result as the original delta:
    ///
    /// ```no_run
    /// # use xi_rope::rope::{Rope, RopeInfo};
    /// # use xi_rope::delta::Delta;
    /// # use std::str::FromStr;
    /// fn test_factor(d : &Delta<RopeInfo>, r : &Rope) {
    ///     let (ins, del) = d.clone().factor();
    ///     let del2 = del.transform_expand(&ins.inserted_subset());
    ///     assert_eq!(String::from(del2.delete_from(&ins.apply(r))), String::from(d.apply(r)));
    /// }
    /// ```
    pub fn factor(self) -> (InsertDelta<N>, Subset) {...}

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
    /// fn test_synthesize(d : &Delta<RopeInfo>, r : &Rope) {
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
    pub fn synthesize(tombstones: &Node<N>, from_dels: &Subset, to_dels: &Subset) -> Delta<N> {...}

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

    fn total_element_len(els: &[DeltaElement<N>]) -> usize {...}

    /// Returns the sum length of the inserts of the delta.
    pub fn inserts_len(&self) -> usize {...}

    /// Iterates over all the inserts of the delta.
    pub fn iter_inserts(&self) -> InsertsIter<'_, N> {...}

    /// Iterates over all the deletions of the delta.
    pub fn iter_deletions(&self) -> DeletionsIter<'_, N> {...}
}

impl<N: NodeInfo> fmt::Debug for Delta<N>
where
    Node<N>: fmt::Debug,
{
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {...}
}

impl<N: NodeInfo> fmt::Debug for InsertDelta<N>
where
    Node<N>: fmt::Debug,
{
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {...}
}

impl<N: NodeInfo> InsertDelta<N> {
    #![allow(clippy::many_single_char_names)]
    /// Do a coordinate transformation on an insert-only delta. The `after` parameter
    /// controls whether the insertions in `self` come after those specific in the
    /// coordinate transform.
    //
    // TODO: write accurate equations
    pub fn transform_expand(&self, xform: &Subset, after: bool) -> InsertDelta<N> {...}

    // TODO: it is plausible this method also works on Deltas with deletes
    /// Shrink a delta through a deletion of some of its copied regions with
    /// the same base. For example, if `self` applies to a union string, and
    /// `xform` is the deletions from that union, the resulting Delta will
    /// apply to the text.
    pub fn transform_shrink(&self, xform: &Subset) -> InsertDelta<N> {...}

    /// Return a Subset containing the inserted ranges.
    ///
    /// `d.inserted_subset().delete_from_string(d.apply_to_string(s)) == s`
    pub fn inserted_subset(&self) -> Subset {...}
}

/// An InsertDelta is a certain kind of Delta, and anything that applies to a
/// Delta that may include deletes also applies to one that definitely
/// doesn't. This impl allows implicit use of those methods.
impl<N: NodeInfo> Deref for InsertDelta<N> {
    type Target = Delta<N>;

    fn deref(&self) -> &Delta<N> {...}
}

// TODO: this doesn't need the new strings, so it should either be based on a new structure
/// A mapping from coordinates in the source sequence to coordinates in the sequence after
/// the delta is applied.
// like Delta but missing the strings, or perhaps the two subsets it's synthesized from.
pub struct Transformer<'a, N: NodeInfo + 'a> {
    delta: &'a Delta<N>,
}

impl<'a, N: NodeInfo + 'a> Transformer<'a, N> {
    /// Create a new transformer from a delta.
    pub fn new(delta: &'a Delta<N>) -> Self {...}

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
pub struct Builder<N: NodeInfo> {
    delta: Delta<N>,
    last_offset: usize,
}

impl<N: NodeInfo> Builder<N> {
    /// Creates a new builder, applicable to a base rope of length `base_len`.
    pub fn new(base_len: usize) -> Builder<N> {...}

    /// Deletes the given interval. Panics if interval is not properly sorted.
    pub fn delete<T: IntervalBounds>(&mut self, interval: T) {...}

    /// Replaces the given interval with the new rope. Panics if interval
    /// is not properly sorted.
    pub fn replace<T: IntervalBounds>(&mut self, interval: T, rope: Node<N>) {...}

    /// Determines if delta would be a no-op transformation if built.
    pub fn is_empty(&self) -> bool {...}

    /// Builds the `Delta`.
    pub fn build(mut self) -> Delta<N> {...}
}

pub struct InsertsIter<'a, N: NodeInfo + 'a> {
    pos: usize,
    last_end: usize,
    els_iter: slice::Iter<'a, DeltaElement<N>>,
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

impl<'a, N: NodeInfo> Iterator for InsertsIter<'a, N> {
    type Item = DeltaRegion;

    fn next(&mut self) -> Option<Self::Item> {...}
}

pub struct DeletionsIter<'a, N: NodeInfo + 'a> {
    pos: usize,
    last_end: usize,
    base_len: usize,
    els_iter: slice::Iter<'a, DeltaElement<N>>,
}

impl<'a, N: NodeInfo> Iterator for DeletionsIter<'a, N> {
    type Item = DeltaRegion;

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
use crate::tree::{Node, NodeInfo};

/// A trait implemented by various diffing strategies.
pub trait Diff<N: NodeInfo> {
    fn compute_delta(base: &Node<N>, target: &Node<N>) -> Delta<N>;
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

impl Diff<RopeInfo> for LineHashDiff {
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
#[cfg(feature = "serde")]
#[allow(clippy::non_local_definitions)]
#[derive(Serialize, Deserialize)]
pub struct Engine {
    /// The session ID used to create new `RevId`s for edits made on this device
    #[cfg_attr(feature = "serde", serde(default = "default_session", skip_serializing))]
    session: SessionId,
    /// The incrementing revision number counter for this session used for `RevId`s
    #[cfg_attr(feature = "serde", serde(default = "initial_revision_counter", skip_serializing))]
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
#[cfg(feature = "serde")]
#[allow(clippy::non_local_definitions)]
#[derive(Serialize, Deserialize)]
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
#[cfg(feature = "serde")]
#[allow(clippy::non_local_definitions)]
#[derive(Serialize, Deserialize)]
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
#[cfg(feature = "serde")]
#[allow(clippy::non_local_definitions)]
#[derive(Serialize, Deserialize)]
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

/// for single user cases, used by serde and ::empty
fn default_session() -> (u64, u32) {...}

/// Revision 0 is always an Undo of the empty set of groups
#[cfg(feature = "serde")]
fn initial_revision_counter() -> u32 {...}

impl RevId {
    /// Returns a u64 that will be equal for equivalent revision IDs and
    /// should be as unlikely to collide as two random u64s.
    pub fn token(&self) -> RevToken {...}

    pub fn session_id(&self) -> SessionId {...}
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
    fn deletes_from_union_before_index(&self, rev_index: usize, invert_undos: bool) -> Cow<'_, Subset> {...}

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
    pub fn try_delta_rev_head(&self, base_rev: RevToken) -> Result<Delta<RopeInfo>, Error> {...}

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
        delta: Delta<RopeInfo>,
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
        delta: Delta<RopeInfo>,
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
        delta: Delta<RopeInfo>,
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
    inserts: InsertDelta<RopeInfo>,
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
    cursor: &mut Cursor<RopeInfo>,
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
    cursor: &mut Cursor<RopeInfo>,
    lines: &mut LinesRaw,
    cm: CaseMatching,
    pat: &str,
    num_steps: usize,
    regex: Option<&Regex>,
) -> FindResult {...}

// Run the core repeatedly until there is a result, up to a certain number of steps.
fn find_progress_iter(
    cursor: &mut Cursor<RopeInfo>,
    lines: &mut LinesRaw,
    pat: &str,
    scanner: impl Fn(&str) -> Option<usize>,
    matcher: impl Fn(&mut Cursor<RopeInfo>, &mut LinesRaw, &str) -> Option<usize>,
    num_steps: usize,
) -> FindResult {...}

// The core of the find algorithm. It takes a "scanner", which quickly
// scans through a single leaf searching for some prefix of the pattern,
// then a "matcher" which confirms that such a candidate actually matches
// in the full rope.
fn find_core(
    cursor: &mut Cursor<RopeInfo>,
    lines: &mut LinesRaw,
    pat: &str,
    scanner: impl Fn(&str) -> Option<usize>,
    matcher: impl Fn(&mut Cursor<RopeInfo>, &mut LinesRaw, &str) -> Option<usize>,
) -> FindResult {...}

/// Compare whether the substring beginning at the current cursor location
/// is equal to the provided string. Leaves the cursor at an indeterminate
/// position on failure, but the end of the string on success. Returns the
/// start position of the match.
pub fn compare_cursor_str(
    cursor: &mut Cursor<RopeInfo>,
    _lines: &mut LinesRaw,
    mut pat: &str,
) -> Option<usize> {...}

/// Like `compare_cursor_str` but case invariant (using to_lowercase() to
/// normalize both strings before comparison). Returns the start position
/// of the match.
pub fn compare_cursor_str_casei(
    cursor: &mut Cursor<RopeInfo>,
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
    cursor: &mut Cursor<RopeInfo>,
    lines: &mut LinesRaw,
    pat: &str,
    regex: &Regex,
) -> Option<usize> {...}

/// Checks if a regular expression can match multiple lines.
pub fn is_multiline_regex(regex: &str) -> bool {...}

/// Scan for a codepoint that, after conversion to lowercase, matches the probe.
fn scan_lowercase(probe: char, s: &str) -> Option<usize> {...}
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
#[macro_use]
extern crate serde;


pub mod breaks;
pub mod compare;
pub mod delta;
pub mod diff;
pub mod engine;
pub mod find;
pub mod interval;
pub mod multiset;
pub mod rope;
#[cfg(feature = "serde")]
mod serde_impls;
pub mod spans;
pub mod tree;

pub use crate::delta::{Builder as DeltaBuilder, Delta, DeltaElement, Transformer};
pub use crate::interval::Interval;
pub use crate::rope::{LinesMetric, Rope, RopeDelta, RopeInfo};
pub use crate::tree::{Cursor, Metric};
```

## xi-editor-ph7/rust/rope/src/multiset.rs

```rust
use std::cmp;

// These two imports are for the `apply` method only.
use crate::interval::Interval;
use crate::tree::{Node, NodeInfo, TreeBuilder};
use std::fmt;
use std::slice;

#[derive(Clone, PartialEq, Eq, Debug)]
#[cfg(feature = "serde")]
#[allow(clippy::non_local_definitions)]
#[derive(Serialize, Deserialize)]
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
#[cfg(feature = "serde")]
#[allow(clippy::non_local_definitions)]
#[derive(Serialize, Deserialize)]
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
    pub fn delete_from<N: NodeInfo>(&self, s: &Node<N>) -> Node<N> {...}

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

    /// Find the complement of this Subset. Every 0-count element will have a
    /// count of 1 and every non-zero element will have a count of 0.
    pub fn complement(&self) -> Subset {...}

    /// Return a `Mapper` that can be use to map coordinates in the document to coordinates
    /// in this `Subset`, but only in non-decreasing order for performance reasons.
    pub fn mapper(&self, matcher: CountMatcher) -> Mapper<'_> {...}
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
use std::cmp::{max, min, Ordering};
use std::fmt;
use std::ops::Add;
use std::str::{self, FromStr};
use std::string::ParseError;

use crate::delta::{Delta, DeltaElement};
use crate::interval::{Interval, IntervalBounds};
use crate::tree::{Cursor, DefaultMetricProvider, Leaf, Metric, Node, NodeInfo, TreeBuilder};

use memchr::{memchr, memrchr};
use unicode_segmentation::{GraphemeCursor, GraphemeIncomplete};

const MIN_LEAF: usize = 511;
const MAX_LEAF: usize = 1024;

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
pub type Rope = Node<RopeInfo>;

/// Represents a transform from one rope to another.
pub type RopeDelta = Delta<RopeInfo>;

/// An element in a `RopeDelta`.
pub type RopeDeltaElement = DeltaElement<RopeInfo>;

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

impl NodeInfo for RopeInfo {
    type L = String;

    fn accumulate(&mut self, other: &Self) {...}

    fn compute_info(s: &String) -> Self {...}

    fn identity() -> Self {...}
}

impl DefaultMetricProvider for RopeInfo {
    fn convert_from_default<M: Metric<Self>>(node: &Node<Self>, offset: usize) -> usize {...}

    fn convert_to_default<M: Metric<Self>>(node: &Node<Self>, offset: usize) -> usize {...}
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

impl Metric<RopeInfo> for BaseMetric {
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
impl Metric<RopeInfo> for LinesMetric {
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

impl Metric<RopeInfo> for Utf16CodeUnitsMetric {
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

fn count_utf16_code_units(s: &str) -> usize {...}

fn find_leaf_split_for_bulk(s: &str) -> usize {...}

fn find_leaf_split_for_merge(s: &str) -> usize {...}

// Try to split at newline boundary (leaning left), if not, then split at codepoint
fn find_leaf_split(s: &str, minsplit: usize) -> usize {...}

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
    cursor: Cursor<'a, RopeInfo>,
    end: usize,
}

impl<'a> Iterator for ChunkIter<'a> {
    type Item = &'a str;

    fn next(&mut self) -> Option<&'a str> {...}
}

impl TreeBuilder<RopeInfo> {
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

impl<'a> Cursor<'a, RopeInfo> {
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

## xi-editor-ph7/rust/rope/src/serde_impls.rs

```rust
use std::fmt;
use std::str::FromStr;

use serde::de::{self, Deserialize, Deserializer, Visitor};
use serde::ser::{Serialize, SerializeStruct, SerializeTupleVariant, Serializer};

use crate::tree::Node;
use crate::{Delta, DeltaElement, Rope, RopeInfo};

// Interim serializable types used for (de)serializing `Delta<RopeInfo>`.
// These are defined at module-level so derive macros generate impls at the
// correct (non-nested) scope; this avoids `non_local_definitions` lint
// failures when serde attributes are used inside function bodies.
#[cfg(feature = "serde")]
#[derive(Serialize, Deserialize)]
#[serde(rename_all = "snake_case")]
#[allow(clippy::non_local_definitions)]
enum RopeDeltaElement_ {
    Copy(usize, usize),
    Insert(Node<RopeInfo>),
}

#[cfg(feature = "serde")]
#[derive(Serialize, Deserialize)]
#[allow(clippy::non_local_definitions)]
struct RopeDelta_ {
    els: Vec<RopeDeltaElement_>,
    base_len: usize,
}

impl From<RopeDeltaElement_> for DeltaElement<RopeInfo> {
    fn from(elem: RopeDeltaElement_) -> DeltaElement<RopeInfo> {...}
}

impl From<RopeDelta_> for Delta<RopeInfo> {
    fn from(mut delta: RopeDelta_) -> Delta<RopeInfo> {...}
}

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

impl Serialize for DeltaElement<RopeInfo> {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {...}
}

impl Serialize for Delta<RopeInfo> {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {...}
}

impl<'de> Deserialize<'de> for Delta<RopeInfo> {
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

pub type Spans<T> = Node<SpansInfo<T>>;

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

impl<T: Clone> NodeInfo for SpansInfo<T> {
    type L = SpansLeaf<T>;

    fn accumulate(&mut self, other: &Self) {...}

    fn compute_info(l: &SpansLeaf<T>) -> Self {...}
}

pub struct SpansBuilder<T: Clone> {
    b: TreeBuilder<SpansInfo<T>>,
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
    cursor: Cursor<'a, SpansInfo<T>>,
    ix: usize,
}

impl<T: Clone> Spans<T> {
    // Note: this implementation is not efficient for very large Spans objects, as it
    // traverses all spans linearly. A more sophisticated approach would be to traverse
    // the tree, and only delve into subtrees that are transformed.
    /// Perform operational transformation on a spans object intended to be edited into
    /// a sequence at the given offset.
    pub fn transform<N: NodeInfo>(
        &self,
        base_start: usize,
        base_end: usize,
        xform: &mut Transformer<N>,
    ) -> Self {...}

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
    pub fn apply_shape<M: NodeInfo>(&mut self, delta: &Delta<M>) {...}

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

impl Delta<RopeInfo> {
    pub fn apply_to_string(&self, s: &str) -> String {...}
}

impl PartialEq for Rope {
    fn eq(&self, other: &Rope) -> bool {...}
}

pub fn parse_subset(s: &str) -> Subset {...}

pub fn parse_subset_list(s: &str) -> Vec<Subset> {...}

pub fn debug_subsets(subsets: &[Subset]) {...}

pub fn parse_delta(s: &str) -> Delta<RopeInfo> {...}
```

## xi-editor-ph7/rust/rope/src/tree.rs

```rust
use std::cmp::{min, Ordering};
use std::marker::PhantomData;
use std::sync::Arc;

use crate::interval::{Interval, IntervalBounds};

const MIN_CHILDREN: usize = 4;
const MAX_CHILDREN: usize = 8;

pub trait NodeInfo: Clone {
    /// The type of the leaf.
    ///
    /// A given `NodeInfo` is for exactly one type of leaf. That is why
    /// the leaf type is an associated type rather than a type parameter.
    type L: Leaf;

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
    fn compute_info(_: &Self::L) -> Self;

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
pub trait DefaultMetricProvider: NodeInfo {
    fn convert_from_default<M: Metric<Self>>(node: &Node<Self>, offset: usize) -> usize;
    fn convert_to_default<M: Metric<Self>>(node: &Node<Self>, offset: usize) -> usize;
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
#[derive(Clone)]
pub struct Node<N: NodeInfo>(Arc<NodeBody<N>>);

#[derive(Clone)]
struct NodeBody<N: NodeInfo> {
    height: usize,
    len: usize,
    info: N,
    val: NodeVal<N>,
}

#[derive(Clone)]
enum NodeVal<N: NodeInfo> {
    Leaf(N::L),
    Internal(Vec<Node<N>>),
}

// also consider making Metric a newtype for usize, so type system can
// help separate metrics

/// A trait for quickly processing attributes of a
/// [NodeInfo](struct.NodeInfo.html).
///
/// For the conceptual background see the
/// [blog post, Rope science, part 2: metrics](https://github.com/google/xi-editor/blob/master/docs/docs/rope_science_02.md).
pub trait Metric<N: NodeInfo> {
    /// Return the size of the
    /// [NodeInfo::L](trait.NodeInfo.html#associatedtype.L), as measured by this
    /// metric.
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
    fn to_base_units(l: &N::L, in_measured_units: usize) -> usize;

    /// Returns the smallest offset in measured units corresponding to an offset in base units.
    ///
    /// # Invariants:
    ///
    /// - `from_base_units(to_base_units(x)) == x` is True for valid `x`
    fn from_base_units(l: &N::L, in_base_units: usize) -> usize;

    /// Return whether the offset in base units is a boundary of this metric.
    /// If a boundary is at end of a leaf then this method must return true.
    /// However, a boundary at the beginning of a leaf is optional
    /// (the previous leaf will be queried).
    fn is_boundary(l: &N::L, offset: usize) -> bool;

    /// Returns the index of the boundary directly preceding offset,
    /// or None if no such boundary exists. Input and result are in base units.
    fn prev(l: &N::L, offset: usize) -> Option<usize>;

    /// Returns the index of the first boundary for which index > offset,
    /// or None if no such boundary exists. Input and result are in base units.
    fn next(l: &N::L, offset: usize) -> Option<usize>;

    /// Returns true if the measured units in this metric can span multiple
    /// leaves.  As an example, in a metric that measures lines in a rope, a
    /// line may start in one leaf and end in another; however in a metric
    /// measuring bytes, storage of a single byte cannot extend across leaves.
    fn can_fragment() -> bool;
}

impl<N: NodeInfo> Node<N> {
    pub fn from_leaf(l: N::L) -> Node<N> {...}

    /// Create a node from a vec of nodes.
    ///
    /// The input must satisfy the following balancing requirements:
    /// * The length of `nodes` must be <= MAX_CHILDREN and > 1.
    /// * All the nodes are the same height.
    /// * All the nodes must satisfy is_ok_child.
    fn from_nodes(nodes: Vec<Node<N>>) -> Node<N> {...}

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

    fn get_children(&self) -> &[Node<N>] {...}

    fn get_leaf(&self) -> &N::L {...}

    /// Call a callback with a mutable reference to a leaf.
    ///
    /// This clones the leaf if the reference is shared. It also recomputes
    /// length and info after the leaf is mutated.
    fn with_leaf_mut<T>(&mut self, f: impl FnOnce(&mut N::L) -> T) -> T {...}

    fn is_ok_child(&self) -> bool {...}

    fn merge_nodes(children1: &[Node<N>], children2: &[Node<N>]) -> Node<N> {...}

    fn merge_leaves(mut rope1: Node<N>, rope2: Node<N>) -> Node<N> {...}

    pub fn concat(rope1: Node<N>, rope2: Node<N>) -> Node<N> {...}

    pub fn measure<M: Metric<N>>(&self) -> usize {...}

    pub fn subseq<T: IntervalBounds>(&self, iv: T) -> Node<N> {...}

    pub fn edit<T, IV>(&mut self, iv: IV, new: T)
    where
        T: Into<Node<N>>,
        IV: IntervalBounds,
    {...}

    // doesn't deal with endpoint, handle that specially if you need it
    pub fn convert_metrics<M1: Metric<N>, M2: Metric<N>>(&self, mut m1: usize) -> usize {...}
}

impl<N: DefaultMetricProvider> Node<N> {
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
    pub fn count<M: Metric<N>>(&self, offset: usize) -> usize {...}

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
    pub fn count_base_units<M: Metric<N>>(&self, offset: usize) -> usize {...}
}

impl<N: NodeInfo> Default for Node<N> {
    fn default() -> Node<N> {...}
}

/// A builder for creating new trees.
pub struct TreeBuilder<N: NodeInfo> {
    // A stack of partially built trees. These are kept in order of
    // strictly descending height, and all vectors have a length less
    // than MAX_CHILDREN and greater than zero.
    //
    // In addition, there is a balancing invariant: for each vector
    // of length greater than one, all elements satisfy `is_ok_child`.
    stack: Vec<Vec<Node<N>>>,
}

impl<N: NodeInfo> TreeBuilder<N> {
    /// A new, empty builder.
    pub fn new() -> TreeBuilder<N> {...}

    /// Append a node to the tree being built.
    pub fn push(&mut self, mut n: Node<N>) {...}

    /// Push a subsequence of a rope.
    ///
    /// Pushes the subsequence of another tree `n` defined by the interval `iv`
    /// onto the builder.
    ///
    /// This is intended as an efficient operation. It is equivalent to taking
    /// the subsequence of `n` and pushing that, but attempts to minimize the
    /// allocation of intermediate results.
    pub fn push_slice(&mut self, n: &Node<N>, iv: Interval) {...}

    /// Append a sequence of leaves.
    pub fn push_leaves(&mut self, leaves: impl IntoIterator<Item = N::L>) {...}

    /// Append a single leaf.
    pub fn push_leaf(&mut self, l: N::L) {...}

    /// Append a slice of a single leaf.
    pub fn push_leaf_slice(&mut self, l: &N::L, iv: Interval) {...}

    /// Build the final tree.
    ///
    /// The tree is the concatenation of all the nodes and leaves that have been pushed
    /// on the builder, in order.
    pub fn build(mut self) -> Node<N> {...}

    /// Pop the last vec-of-nodes off the stack, resulting in a node.
    fn pop(&mut self) -> Node<N> {...}
}

const CURSOR_CACHE_SIZE: usize = 4;

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
pub struct Cursor<'a, N: 'a + NodeInfo> {
    /// The tree being traversed by this cursor.
    root: &'a Node<N>,
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
    cache: [Option<(&'a Node<N>, usize)>; CURSOR_CACHE_SIZE],
    /// The leaf containing the current position, when the cursor is valid.
    ///
    /// The position is only at the end of the leaf when it is at the end of the tree.
    leaf: Option<&'a N::L>,
    /// The offset of `leaf` within the tree.
    offset_of_leaf: usize,
}

impl<'a, N: NodeInfo> Cursor<'a, N> {
    /// Create a new cursor at the given position.
    pub fn new(n: &'a Node<N>, position: usize) -> Cursor<'a, N> {...}

    /// The length of the tree.
    pub fn total_len(&self) -> usize {...}

    /// Return a reference to the root node of the tree.
    pub fn root(&self) -> &'a Node<N> {...}

    /// Get the current leaf of the cursor.
    ///
    /// If the cursor is valid, returns the leaf containing the current position,
    /// and the offset of the current position within the leaf. That offset is equal
    /// to the leaf length only at the end, otherwise it is less than the leaf length.
    pub fn get_leaf(&self) -> Option<(&'a N::L, usize)> {...}

    /// Set the position of the cursor.
    ///
    /// The cursor is valid after this call.
    ///
    /// Precondition: `position` is less than or equal to the length of the tree.
    pub fn set(&mut self, position: usize) {...}

    /// Get the position of the cursor.
    pub fn pos(&self) -> usize {...}

    /// Determine whether the current position is a boundary.
    ///
    /// Note: the beginning and end of the tree may or may not be boundaries, depending on the
    /// metric. If the metric is not `can_fragment`, then they always are.
    pub fn is_boundary<M: Metric<N>>(&mut self) -> bool {...}

    /// Moves the cursor to the previous boundary.
    ///
    /// When there is no previous boundary, returns `None` and the cursor becomes invalid.
    ///
    /// Return value: the position of the boundary, if it exists.
    pub fn prev<M: Metric<N>>(&mut self) -> Option<usize> {...}

    /// Moves the cursor to the next boundary.
    ///
    /// When there is no next boundary, returns `None` and the cursor becomes invalid.
    ///
    /// Return value: the position of the boundary, if it exists.
    pub fn next<M: Metric<N>>(&mut self) -> Option<usize> {...}

    /// Returns the current position if it is a boundary in this [`Metric`],
    /// else behaves like [`next`](#method.next).
    ///
    /// [`Metric`]: struct.Metric.html
    pub fn at_or_next<M: Metric<N>>(&mut self) -> Option<usize> {...}

    /// Returns the current position if it is a boundary in this [`Metric`],
    /// else behaves like [`prev`](#method.prev).
    ///
    /// [`Metric`]: struct.Metric.html
    pub fn at_or_prev<M: Metric<N>>(&mut self) -> Option<usize> {...}

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
    pub fn iter<'c, M: Metric<N>>(&'c mut self) -> CursorIter<'c, 'a, N, M> {...}

    /// Tries to find the last boundary in the leaf the cursor is currently in.
    ///
    /// If the last boundary is at the end of the leaf, it is only counted if
    /// it is less than `orig_pos`.
    #[inline]
    fn last_inside_leaf<M: Metric<N>>(&mut self, orig_pos: usize) -> Option<usize> {...}

    /// Tries to find the next boundary in the leaf the cursor is currently in.
    #[inline]
    fn next_inside_leaf<M: Metric<N>>(&mut self) -> Option<usize> {...}

    /// Move to beginning of next leaf.
    ///
    /// Return value: same as [`get_leaf`](#method.get_leaf).
    pub fn next_leaf(&mut self) -> Option<(&'a N::L, usize)> {...}

    /// Move to beginning of previous leaf.
    ///
    /// Return value: same as [`get_leaf`](#method.get_leaf).
    pub fn prev_leaf(&mut self) -> Option<(&'a N::L, usize)> {...}

    /// Go to the leaf containing the current position.
    ///
    /// Sets `leaf` to the leaf containing `position`, and updates `cache` and
    /// `offset_of_leaf` to be consistent.
    fn descend(&mut self) {...}

    /// Returns the measure at the beginning of the leaf containing `pos`.
    ///
    /// This method is O(log n) no matter the current cursor state.
    fn measure_leaf<M: Metric<N>>(&self, mut pos: usize) -> usize {...}

    /// Find the leaf having the given measure.
    ///
    /// This function sets `self.position` to the beginning of the leaf
    /// containing the smallest offset with the given metric, and also updates
    /// state as if [`descend`](#method.descend) was called.
    ///
    /// If `measure` is greater than the measure of the whole tree, then moves
    /// to the last node.
    fn descend_metric<M: Metric<N>>(&mut self, mut measure: usize) {...}
}

/// An iterator generated by a [`Cursor`], for some [`Metric`].
///
/// [`Cursor`]: struct.Cursor.html
/// [`Metric`]: struct.Metric.html
pub struct CursorIter<'c, 'a: 'c, N: 'a + NodeInfo, M: 'a + Metric<N>> {
    cursor: &'c mut Cursor<'a, N>,
    _metric: PhantomData<&'a M>,
}

impl<'c, 'a, N: NodeInfo, M: Metric<N>> Iterator for CursorIter<'c, 'a, N, M> {
    type Item = usize;

    fn next(&mut self) -> Option<usize> {...}
}

impl<'c, 'a, N: NodeInfo, M: Metric<N>> CursorIter<'c, 'a, N, M> {
    /// Returns the current position of the underlying [`Cursor`].
    ///
    /// [`Cursor`]: struct.Cursor.html
    pub fn pos(&self) -> usize {...}
}
```

