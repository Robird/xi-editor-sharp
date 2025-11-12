## xi-editor-ph7/rust/trace/src/chrome_trace_dump.rs

```rust
#![allow(
    clippy::if_same_then_else,
    clippy::needless_bool,
    clippy::needless_pass_by_value,
    clippy::ptr_arg
)]

// We do not use the nightly test harness in this crate.  Criterion benches
// live in `benches/` and exercise the public API.  The `test` crate and
// #[bench] attributes are not used to stay compatible with stable toolchains.

use std::io::{Error as IOError, ErrorKind as IOErrorKind, Read, Write};

use super::Sample;

#[derive(Debug)]
pub enum Error {
    Io(IOError),
    Json(serde_json::Error),
    DecodingFormat(String),
}

impl From<IOError> for Error {
    fn from(e: IOError) -> Error {...}
}

impl From<serde_json::Error> for Error {
    fn from(e: serde_json::Error) -> Error {...}
}

impl From<String> for Error {
    fn from(e: String) -> Error {...}
}

impl Error {
    pub fn already_exists() -> Error {...}
}

#[allow(dead_code)]
#[derive(Clone, Debug, Deserialize)]
#[serde(untagged)]
enum ChromeTraceArrayEntries {
    Array(Vec<Sample>),
}

/// This serializes the samples into the [Chrome trace event format](https://www.google.com/url?sa=t&rct=j&q=&esrc=s&source=web&cd=1&ved=0ahUKEwiJlZmDguXYAhUD4GMKHVmEDqIQFggpMAA&url=https%3A%2F%2Fdocs.google.com%2Fdocument%2Fd%2F1CvAClvFfyA5R-PhYUmn5OOQtYMH4h6I0nSsKchNAySU%2Fpreview&usg=AOvVaw0tBFlVbDVBikdzLqgrWK3g).
///
/// # Arguments
/// `samples` - Something that can be converted into an iterator of sample
/// references.
/// `format` - Which trace format to save the data in.  There are four total
/// formats described in the document.
/// `output` - Where to write the serialized result.
///
/// # Returns
/// A `Result<(), Error>` that indicates if serialization was successful or the
/// details of any error that occured.
///
/// # Examples
/// ```no_run
/// # use xi_trace::chrome_trace_dump::serialize;
/// let samples = xi_trace::samples_cloned_sorted();
/// let mut serialized = Vec::<u8>::new();
/// serialize(&samples, &mut serialized);
/// ```
pub fn serialize<W>(samples: &Vec<Sample>, output: W) -> Result<(), Error>
where
    W: Write,
{...}

pub fn to_value(samples: &Vec<Sample>) -> Result<serde_json::Value, Error> {...}

pub fn decode(samples: serde_json::Value) -> Result<Vec<Sample>, Error> {...}

pub fn deserialize<R>(input: R) -> Result<Vec<Sample>, Error>
where
    R: Read,
{...}
```

## xi-editor-ph7/rust/trace/src/fixed_lifo_deque.rs

```rust
use std::cmp::{self, Ordering};
use std::collections::vec_deque::{Drain, IntoIter, Iter, IterMut, VecDeque};
use std::hash::{Hash, Hasher};
use std::ops::{Index, IndexMut, RangeBounds};

/// Provides fixed size ring buffer that overwrites elements in FIFO order on
/// insertion when full.  API provided is similar to VecDeque & uses a VecDeque
/// internally. One distinction is that only append-like insertion is allowed.
/// This means that insert & push_front are not allowed.  The reasoning is that
/// there is ambiguity on how such functions should operate since it would be
/// pretty impossible to maintain a FIFO ordering.
///
/// All operations that would cause growth beyond the limit drop the appropriate
/// number of elements from the front.  For example, on a full buffer push_front
/// replaces the first element.
///
/// The removal of elements on operation that would cause excess beyond the
/// limit happens first to make sure the space is available in the underlying
/// VecDeque, thus guaranteeing O(1) operations always.
#[derive(Clone, Debug)]
pub struct FixedLifoDeque<T> {
    storage: VecDeque<T>,
    limit: usize,
}

impl<T> FixedLifoDeque<T> {
    /// Constructs a ring buffer that will reject all insertions as no-ops.
    /// This also construct the underlying VecDeque with_capacity(0) which
    /// in the current stdlib implementation allocates 2 Ts.
    #[inline]
    pub fn new() -> Self {...}

    /// Constructs a fixed size ring buffer with the given number of elements.
    /// Attempts to insert more than this number of elements will cause excess
    /// elements to first be evicted in FIFO order (i.e. from the front).
    pub fn with_limit(n: usize) -> Self {...}

    /// This sets a new limit on the container.  Excess elements are dropped in
    /// FIFO order.  The new capacity is reset to the requested limit which will
    /// likely result in re-allocation + copies/clones even if the limit
    /// shrinks.
    pub fn reset_limit(&mut self, n: usize) {...}

    /// Returns the current limit this ring buffer is configured with.
    #[inline]
    pub fn limit(&self) -> usize {...}

    #[inline]
    pub fn get(&self, index: usize) -> Option<&T> {...}

    #[inline]
    pub fn get_mut(&mut self, index: usize) -> Option<&mut T> {...}

    #[inline]
    pub fn swap(&mut self, i: usize, j: usize) {...}

    #[inline]
    pub fn capacity(&self) -> usize {...}

    #[inline]
    pub fn iter(&self) -> Iter<'_, T> {...}

    #[inline]
    pub fn iter_mut(&mut self) -> IterMut<'_, T> {...}

    /// Returns a tuple of 2 slices that represents the ring buffer. [0] is the
    /// beginning of the buffer to the physical end of the array or the last
    /// element (whichever comes first).  [1] is the continuation of [0] if the
    /// ring buffer has wrapped the contiguous storage.
    #[inline]
    pub fn as_slices(&self) -> (&[T], &[T]) {...}

    #[inline]
    pub fn as_mut_slices(&mut self) -> (&mut [T], &mut [T]) {...}

    #[inline]
    pub fn len(&self) -> usize {...}

    #[inline]
    pub fn is_empty(&self) -> bool {...}

    #[inline]
    pub fn drain<R>(&mut self, range: R) -> Drain<'_, T>
    where
        R: RangeBounds<usize>,
    {...}

    #[inline]
    pub fn clear(&mut self) {...}

    #[inline]
    pub fn contains(&self, x: &T) -> bool
    where
        T: PartialEq<T>,
    {...}

    #[inline]
    pub fn front(&self) -> Option<&T> {...}

    #[inline]
    pub fn front_mut(&mut self) -> Option<&mut T> {...}

    #[inline]
    pub fn back(&self) -> Option<&T> {...}

    #[inline]
    pub fn back_mut(&mut self) -> Option<&mut T> {...}

    #[inline]
    fn drop_excess_for_inserting(&mut self, n_to_be_inserted: usize) {...}

    /// Always an O(1) operation.  Memory is never reclaimed.
    #[inline]
    pub fn pop_front(&mut self) -> Option<T> {...}

    /// Always an O(1) operation.  If the number of elements is at the limit,
    /// the element at the front is overwritten.
    ///
    /// Post condition: The number of elements is <= limit
    pub fn push_back(&mut self, value: T) {...}

    /// Always an O(1) operation.  Memory is never reclaimed.
    #[inline]
    pub fn pop_back(&mut self) -> Option<T> {...}

    #[inline]
    pub fn swap_remove_back(&mut self, index: usize) -> Option<T> {...}

    #[inline]
    pub fn swap_remove_front(&mut self, index: usize) -> Option<T> {...}

    /// Always an O(1) operation.
    #[inline]
    pub fn remove(&mut self, index: usize) -> Option<T> {...}

    pub fn split_off(&mut self, at: usize) -> FixedLifoDeque<T> {...}

    /// Always an O(m) operation where m is the length of `other'.
    pub fn append(&mut self, other: &mut VecDeque<T>) {...}

    #[inline]
    pub fn retain<F>(&mut self, f: F)
    where
        F: FnMut(&T) -> bool,
    {...}
}

impl<T: Clone> FixedLifoDeque<T> {
    /// Resizes a fixed queue.  This doesn't change the limit so the resize is
    /// capped to the limit.  Additionally, resizing drops the elements from the
    /// front unlike with a regular VecDeque.
    pub fn resize(&mut self, new_len: usize, value: T) {...}
}

impl<A: PartialEq> PartialEq for FixedLifoDeque<A> {
    #[inline]
    fn eq(&self, other: &FixedLifoDeque<A>) -> bool {...}
}

impl<A: Eq> Eq for FixedLifoDeque<A> {}

impl<A: PartialOrd> PartialOrd for FixedLifoDeque<A> {
    #[inline]
    fn partial_cmp(&self, other: &FixedLifoDeque<A>) -> Option<Ordering> {...}
}

impl<A: Ord> Ord for FixedLifoDeque<A> {
    #[inline]
    fn cmp(&self, other: &FixedLifoDeque<A>) -> Ordering {...}
}

impl<A: Hash> Hash for FixedLifoDeque<A> {
    #[inline]
    fn hash<H: Hasher>(&self, state: &mut H) {...}
}

impl<A> Index<usize> for FixedLifoDeque<A> {
    type Output = A;

    #[inline]
    fn index(&self, index: usize) -> &A {...}
}

impl<A> IndexMut<usize> for FixedLifoDeque<A> {
    #[inline]
    fn index_mut(&mut self, index: usize) -> &mut A {...}
}

impl<T> IntoIterator for FixedLifoDeque<T> {
    type Item = T;
    type IntoIter = IntoIter<T>;

    /// Consumes the list into a front-to-back iterator yielding elements by
    /// value.
    #[inline]
    fn into_iter(self) -> IntoIter<T> {...}
}

impl<'a, T> IntoIterator for &'a FixedLifoDeque<T> {
    type Item = &'a T;
    type IntoIter = Iter<'a, T>;

    #[inline]
    fn into_iter(self) -> Iter<'a, T> {...}
}

impl<'a, T> IntoIterator for &'a mut FixedLifoDeque<T> {
    type Item = &'a mut T;
    type IntoIter = IterMut<'a, T>;

    #[inline]
    fn into_iter(self) -> IterMut<'a, T> {...}
}

impl<A> Extend<A> for FixedLifoDeque<A> {
    fn extend<T: IntoIterator<Item = A>>(&mut self, iter: T) {...}
}

impl<'a, T: 'a + Copy> Extend<&'a T> for FixedLifoDeque<T> {
    fn extend<I: IntoIterator<Item = &'a T>>(&mut self, iter: I) {...}
}
```

## xi-editor-ph7/rust/trace/src/lib.rs

```rust
#![allow(clippy::identity_op, clippy::new_without_default, clippy::trivially_copy_pass_by_ref)]

#[macro_use]
extern crate lazy_static;
// Use std::time::Instant for monotonic timestamps instead of the deprecated
// `time::precise_time_ns` function from the time crate.
use std::time::Instant;

#[macro_use]
extern crate serde_derive;

extern crate serde;

#[macro_use]
extern crate log;

extern crate libc;


mod fixed_lifo_deque;
mod sys_pid;
mod sys_tid;

#[cfg(feature = "chrome_trace_event")]
pub mod chrome_trace_dump;

use crate::fixed_lifo_deque::FixedLifoDeque;
use std::borrow::Cow;
use std::cmp;
use std::collections::HashMap;
use std::fmt;
use std::fs;
use std::hash::{Hash, Hasher};
use std::mem::size_of;
use std::path::Path;
use std::string::ToString;
use std::sync::atomic::{AtomicBool, Ordering as AtomicOrdering};
use std::sync::Mutex;

pub type StrCow = Cow<'static, str>;

#[derive(Clone, Debug)]
pub enum CategoriesT {
    StaticArray(&'static [&'static str]),
    DynamicArray(Vec<String>),
}

trait StringArrayEq<Rhs: ?Sized = Self> {
    fn arr_eq(&self, other: &Rhs) -> bool;
}

impl StringArrayEq<[&'static str]> for Vec<String> {
    fn arr_eq(&self, other: &[&'static str]) -> bool {...}
}

impl StringArrayEq<Vec<String>> for &'static [&'static str] {
    fn arr_eq(&self, other: &Vec<String>) -> bool {...}
}

impl PartialEq for CategoriesT {
    fn eq(&self, other: &CategoriesT) -> bool {...}
}

impl Eq for CategoriesT {}

impl serde::Serialize for CategoriesT {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: serde::Serializer,
    {...}
}

lazy_static! {
    // Reference instant used for producing process-local monotonic timestamps.
    static ref START_INSTANT: Instant = Instant::now();
}

#[inline]
fn now_ns() -> u64 {...}

impl<'de> serde::Deserialize<'de> for CategoriesT {
    fn deserialize<D>(deserializer: D) -> Result<CategoriesT, D::Error>
    where
        D: serde::Deserializer<'de>,
    {...}
}

impl CategoriesT {
    pub fn join(&self, sep: &str) -> String {...}
}

macro_rules! categories_from_constant_array {
    ($num_args: expr) => {
        impl From<&'static [&'static str; $num_args]> for CategoriesT {
            fn from(c: &'static [&'static str; $num_args]) -> CategoriesT {...}
        }
    };
}

categories_from_constant_array!(0);
categories_from_constant_array!(1);
categories_from_constant_array!(2);
categories_from_constant_array!(3);
categories_from_constant_array!(4);
categories_from_constant_array!(5);
categories_from_constant_array!(6);
categories_from_constant_array!(7);
categories_from_constant_array!(8);
categories_from_constant_array!(9);
categories_from_constant_array!(10);

impl From<Vec<String>> for CategoriesT {
    fn from(c: Vec<String>) -> CategoriesT {...}
}

#[cfg(not(feature = "json_payload"))]
pub type TracePayloadT = StrCow;

#[cfg(feature = "json_payload")]
pub type TracePayloadT = serde_json::Value;

/// How tracing should be configured.
#[derive(Copy, Clone)]
pub struct Config {
    sample_limit_count: usize,
}

impl Config {
    /// The maximum number of bytes the tracing data should take up.  This limit
    /// won't be exceeded by the underlying storage itself (i.e. rounds down).
    pub fn with_limit_bytes(size: usize) -> Self {...}

    /// The maximum number of entries the tracing data should allow.  Total
    /// storage allocated will be limit * size_of<Sample>
    pub fn with_limit_count(limit: usize) -> Self {...}

    // Note: prefer `Default` trait impl for the type; keep the inherent
    // method removed to avoid ambiguity and clippy `should_implement_trait`.
    /// The default amount of storage to allocate for tracing.  Currently 1 MB.

    /// The maximum amount of space the tracing data will take up.  This does
    /// not account for any overhead of storing the data itself (i.e. pointer to
    /// the heap, counters, etc); just the data itself.
    pub fn max_size_in_bytes(self) -> usize {...}

    /// The maximum number of samples that should be stored.
    pub fn max_samples(self) -> usize {...}
}

impl Default for Config {
    fn default() -> Self {...}
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum SampleEventType {
    DurationBegin,
    DurationEnd,
    CompleteDuration,
    Instant,
    AsyncStart,
    AsyncInstant,
    AsyncEnd,
    FlowStart,
    FlowInstant,
    FlowEnd,
    ObjectCreated,
    ObjectSnapshot,
    ObjectDestroyed,
    Metadata,
}

impl SampleEventType {
    // TODO(vlovich): Replace all of this with serde flatten + rename once
    // https://github.com/serde-rs/serde/issues/1189 is fixed.
    #[inline]
    fn into_chrome_id(self) -> char {...}

    #[inline]
    fn from_chrome_id(symbol: char) -> Self {...}
}

#[derive(Clone, Debug, PartialEq, Eq)]
enum MetadataType {
    ProcessName {
        name: String,
    },
    #[allow(dead_code)]
    ProcessLabels {
        labels: String,
    },
    #[allow(dead_code)]
    ProcessSortIndex {
        sort_index: i32,
    },
    ThreadName {
        name: String,
    },
    #[allow(dead_code)]
    ThreadSortIndex {
        sort_index: i32,
    },
}

impl MetadataType {
    fn sample_name(&self) -> &'static str {...}

    fn consume(self) -> (Option<String>, Option<i32>) {...}
}

#[derive(Serialize, Deserialize, Clone, Debug, PartialEq)]
pub struct SampleArgs {
    /// An arbitrary payload to associate with the sample.  The type is
    /// controlled by features (default string).
    #[serde(rename = "xi_payload")]
    #[serde(skip_serializing_if = "Option::is_none")]
    pub payload: Option<TracePayloadT>,

    /// The name to associate with the pid/tid.  Whether it's associated with
    /// the pid or the tid depends on the name of the event
    /// via process_name/thread_name respectively.
    #[serde(rename = "name")]
    #[serde(skip_serializing_if = "Option::is_none")]
    pub metadata_name: Option<StrCow>,

    /// Sorting priority between processes/threads in the view.
    #[serde(rename = "sort_index")]
    #[serde(skip_serializing_if = "Option::is_none")]
    pub metadata_sort_index: Option<i32>,
}

#[inline]
fn ns_to_us(ns: u64) -> u64 {...}

//NOTE: serde requires this to take a reference
fn serialize_event_type<S>(ph: &SampleEventType, s: S) -> Result<S::Ok, S::Error>
where
    S: serde::Serializer,
{...}

fn deserialize_event_type<'de, D>(d: D) -> Result<SampleEventType, D::Error>
where
    D: serde::Deserializer<'de>,
{...}

/// Stores the relevant data about a sample for later serialization.
/// The payload associated with any sample is by default a string but may be
/// configured via the `json_payload` feature (there is an
/// associated performance hit across the board for turning it on).
#[derive(Serialize, Deserialize, Clone, Debug)]
pub struct Sample {
    /// The name of the event to be shown.
    pub name: StrCow,
    /// List of categories the event applies to.
    #[serde(rename = "cat")]
    #[serde(skip_serializing_if = "Option::is_none")]
    pub categories: Option<CategoriesT>,
    /// When was the sample started.
    #[serde(rename = "ts")]
    pub timestamp_us: u64,
    /// What kind of sample this is.
    #[serde(rename = "ph")]
    #[serde(serialize_with = "serialize_event_type")]
    #[serde(deserialize_with = "deserialize_event_type")]
    pub event_type: SampleEventType,
    #[serde(rename = "dur")]
    #[serde(skip_serializing_if = "Option::is_none")]
    pub duration_us: Option<u64>,
    /// The process the sample was captured in.
    pub pid: u64,
    /// The thread the sample was captured on.  Omitted for Metadata events that
    /// want to set the process name (if provided then sets the thread name).
    pub tid: u64,
    #[serde(skip_serializing)]
    pub thread_name: Option<StrCow>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub args: Option<SampleArgs>,
}

fn to_cow_str<S>(s: S) -> StrCow
where
    S: Into<StrCow>,
{...}

impl Sample {
    fn thread_name() -> Option<StrCow> {...}

    /// Constructs a Begin or End sample.  Should not be used directly.  Instead
    /// should be constructed via SampleGuard.
    pub fn new_duration_marker<S, C>(
        name: S,
        categories: C,
        payload: Option<TracePayloadT>,
        event_type: SampleEventType,
    ) -> Self
    where
        S: Into<StrCow>,
        C: Into<CategoriesT>,
    {...}

    /// Constructs a Duration sample.  For use via xi_trace::closure.
    pub fn new_duration<S, C>(
        name: S,
        categories: C,
        payload: Option<TracePayloadT>,
        start_ns: u64,
        duration_ns: u64,
    ) -> Self
    where
        S: Into<StrCow>,
        C: Into<CategoriesT>,
    {...}

    /// Constructs an instantaneous sample.
    pub fn new_instant<S, C>(name: S, categories: C, payload: Option<TracePayloadT>) -> Self
    where
        S: Into<StrCow>,
        C: Into<CategoriesT>,
    {...}

    fn new_metadata(timestamp_ns: u64, meta: MetadataType, tid: u64) -> Self {...}
}

impl PartialEq for Sample {
    fn eq(&self, other: &Sample) -> bool {...}
}

impl Eq for Sample {}

impl PartialOrd for Sample {
    fn partial_cmp(&self, other: &Sample) -> Option<cmp::Ordering> {...}
}

impl Ord for Sample {
    fn cmp(&self, other: &Sample) -> cmp::Ordering {...}
}

impl Hash for Sample {
    fn hash<H: Hasher>(&self, state: &mut H) {...}
}

#[must_use]
pub struct SampleGuard<'a> {
    sample: Option<Sample>,
    trace: Option<&'a Trace>,
}

impl<'a> SampleGuard<'a> {
    #[inline]
    pub fn new_disabled() -> Self {...}

    #[inline]
    fn new<S, C>(trace: &'a Trace, name: S, categories: C, payload: Option<TracePayloadT>) -> Self
    where
        S: Into<StrCow>,
        C: Into<CategoriesT>,
    {...}
}

impl<'a> Drop for SampleGuard<'a> {
    fn drop(&mut self) {...}
}

/// Returns the file name of the EXE if possible, otherwise the full path, or
/// None if an irrecoverable error occured.
fn exe_name() -> Option<String> {...}

/// Stores the tracing data.
pub struct Trace {
    enabled: AtomicBool,
    samples: Mutex<FixedLifoDeque<Sample>>,
}

impl Trace {
    pub fn disabled() -> Self {...}

    pub fn enabled(config: Config) -> Self {...}

    pub fn disable(&self) {...}

    #[inline]
    pub fn enable(&self) {...}

    pub fn enable_config(&self, config: Config) {...}

    /// Generally racy since the underlying storage might be mutated in a separate thread.
    /// Exposed for unit tests.
    pub fn get_samples_count(&self) -> usize {...}

    /// Exposed for unit tests only.
    pub fn get_samples_limit(&self) -> usize {...}

    #[inline]
    pub(crate) fn record(&self, sample: Sample) {...}

    pub fn is_enabled(&self) -> bool {...}

    pub fn instant<S, C>(&self, name: S, categories: C)
    where
        S: Into<StrCow>,
        C: Into<CategoriesT>,
    {...}

    pub fn instant_payload<S, C, P>(&self, name: S, categories: C, payload: P)
    where
        S: Into<StrCow>,
        C: Into<CategoriesT>,
        P: Into<TracePayloadT>,
    {...}

    pub fn block<S, C>(&self, name: S, categories: C) -> SampleGuard<'_>
    where
        S: Into<StrCow>,
        C: Into<CategoriesT>,
    {...}

    pub fn block_payload<S, C, P>(&self, name: S, categories: C, payload: P) -> SampleGuard<'_>
    where
        S: Into<StrCow>,
        C: Into<CategoriesT>,
        P: Into<TracePayloadT>,
    {...}

    pub fn closure<S, C, F, R>(&self, name: S, categories: C, closure: F) -> R
    where
        S: Into<StrCow>,
        C: Into<CategoriesT>,
        F: FnOnce() -> R,
    {...}

    pub fn closure_payload<S, C, P, F, R>(
        &self,
        name: S,
        categories: C,
        closure: F,
        payload: P,
    ) -> R
    where
        S: Into<StrCow>,
        C: Into<CategoriesT>,
        P: Into<TracePayloadT>,
        F: FnOnce() -> R,
    {...}

    pub fn samples_cloned_unsorted(&self) -> Vec<Sample> {...}

    #[inline]
    pub fn samples_cloned_sorted(&self) -> Vec<Sample> {...}

    pub fn save<P: AsRef<Path>>(
        &self,
        path: P,
        sort: bool,
    ) -> Result<(), chrome_trace_dump::Error> {...}
}

lazy_static! {
    static ref TRACE: Trace = Trace::disabled();
}

/// Enable tracing with the default configuration.  See Config::default.
/// Tracing is disabled initially on program launch.
#[inline]
pub fn enable_tracing() {...}

/// Enable tracing with a specific configuration. Tracing is disabled initially
/// on program launch.
#[inline]
pub fn enable_tracing_with_config(config: Config) {...}

/// Disable tracing.  This clears all trace data (& frees the memory).
#[inline]
pub fn disable_tracing() {...}

/// Is tracing enabled.  Technically doesn't guarantee any samples will be
/// stored as tracing could still be enabled but set with a limit of 0.
#[inline]
pub fn is_enabled() -> bool {...}

/// Create an instantaneous sample without any payload.  This is the lowest
/// overhead tracing routine available.
///
/// # Performance
/// The `json_payload` feature makes this ~1.3-~1.5x slower.
/// See `trace_payload` for a more complete discussion.
///
/// # Arguments
///
/// * `name` - A string that provides some meaningful name to this sample.
///   Usage of static strings is encouraged for best performance to avoid copies.
///   However, anything that can be converted into a Cow string can be passed as
///   an argument.
///
/// * `categories` - A static array of static strings that tags the samples in
///   some way.
///
/// # Examples
///
/// ```
/// xi_trace::trace("something happened", &["rpc", "response"]);
/// ```
#[inline]
pub fn trace<S, C>(name: S, categories: C)
where
    S: Into<StrCow>,
    C: Into<CategoriesT>,
{...}

/// Create an instantaneous sample with a payload.  The type the payload
/// conforms to is currently determined by the feature this library is compiled
/// with.  By default, the type is string-like just like name.  If compiled with
/// the `json_payload` then a `serde_json::Value` is expected and  the library
/// acquires a dependency on the `serde_json` crate.
///
/// # Performance
/// A static string has the lowest overhead as no copies are necessary, roughly
/// equivalent performance to a regular trace.  A string that needs to be copied
/// first can make it ~1.7x slower than a regular trace.
///
/// When compiling with `json_payload`, this is ~2.1x slower than a string that
/// needs to be copied (or ~4.5x slower than a static string)
///
/// # Arguments
///
/// * `name` - A string that provides some meaningful name to this sample.
///   Usage of static strings is encouraged for best performance to avoid copies.
///   However, anything that can be converted into a Cow string can be passed as
/// an argument.
///
/// * `categories` - A static array of static strings that tags the samples in
///   some way.
///
/// # Examples
///
/// ```
/// xi_trace::trace_payload("something happened", &["rpc", "response"], "a note about this");
/// ```
///
/// With `json_payload` feature:
///
/// ```rust,ignore
/// xi_trace::trace_payload("my event", &["rpc", "response"], json!({"key": "value"}));
/// ```
#[inline]
pub fn trace_payload<S, C, P>(name: S, categories: C, payload: P)
where
    S: Into<StrCow>,
    C: Into<CategoriesT>,
    P: Into<TracePayloadT>,
{...}

/// Creates a duration sample.  The sample is finalized (end_ns set) when the
/// returned value is dropped.  `trace_closure` may be prettier to read.
///
/// # Performance
/// See `trace_payload` for a more complete discussion.
///
/// # Arguments
///
/// * `name` - A string that provides some meaningful name to this sample.
///   Usage of static strings is encouraged for best performance to avoid copies.
///   However, anything that can be converted into a Cow string can be passed as
/// an argument.
///
/// * `categories` - A static array of static strings that tags the samples in
///   some way.
///
/// # Returns
/// A guard that when dropped will update the Sample with the timestamp & then
/// record it.
///
/// # Examples
///
/// ```
/// fn something_expensive() {
/// }
///
/// fn something_else_expensive() {
/// }
///
/// let trace_guard = xi_trace::trace_block("something_expensive", &["rpc", "request"]);
/// something_expensive();
/// std::mem::drop(trace_guard); // finalize explicitly if
///
/// {
///     let _guard = xi_trace::trace_block("something_else_expensive", &["rpc", "response"]);
///     something_else_expensive();
/// }
/// ```
#[inline]
pub fn trace_block<'a, S, C>(name: S, categories: C) -> SampleGuard<'a>
where
    S: Into<StrCow>,
    C: Into<CategoriesT>,
{...}

/// See `trace_block` for how the block works and `trace_payload` for a
/// discussion on payload.
#[inline]
pub fn trace_block_payload<'a, S, C, P>(name: S, categories: C, payload: P) -> SampleGuard<'a>
where
    S: Into<StrCow>,
    C: Into<CategoriesT>,
    P: Into<TracePayloadT>,
{...}

/// Creates a duration sample that measures how long the closure took to execute.
///
/// # Performance
/// See `trace_payload` for a more complete discussion.
///
/// # Arguments
///
/// * `name` - A string that provides some meaningful name to this sample.
///   Usage of static strings is encouraged for best performance to avoid copies.
/// However, anything that can be converted into a Cow string can be passed as
/// an argument.
///
/// * `categories` - A static array of static strings that tags the samples in
///   some way.
///
/// # Returns
/// The result of the closure.
///
/// # Examples
///
/// ```
/// fn something_expensive() -> u32 {
///     0
/// }
///
/// fn something_else_expensive(value: u32) {
/// }
///
/// let result = xi_trace::trace_closure("something_expensive", &["rpc", "request"], || {
///     something_expensive()
/// });
/// xi_trace::trace_closure("something_else_expensive", &["rpc", "response"], || {
///     something_else_expensive(result);
/// });
/// ```
#[inline]
pub fn trace_closure<S, C, F, R>(name: S, categories: C, closure: F) -> R
where
    S: Into<StrCow>,
    C: Into<CategoriesT>,
    F: FnOnce() -> R,
{...}

/// See `trace_closure` for how the closure works and `trace_payload` for a
/// discussion on payload.
#[inline]
pub fn trace_closure_payload<S, C, P, F, R>(name: S, categories: C, closure: F, payload: P) -> R
where
    S: Into<StrCow>,
    C: Into<CategoriesT>,
    P: Into<TracePayloadT>,
    F: FnOnce() -> R,
{...}

#[inline]
pub fn samples_len() -> usize {...}

/// Returns all the samples collected so far.  There is no guarantee that the
/// samples are ordered chronologically for several reasons:
///
/// 1. Samples that span sections of code may be inserted on end instead of
///    beginning.
/// 2. Performance optimizations might have per-thread buffers.  Keeping all
///    that sorted would be prohibitively expensive.
/// 3. You may not care about them always being sorted if you're merging samples
///    from multiple distributed sources (i.e. you want to sort the merged result
///    rather than just this processe's samples).
#[inline]
pub fn samples_cloned_unsorted() -> Vec<Sample> {...}

/// Returns all the samples collected so far ordered chronologically by
/// creation.  Roughly corresponds to start_ns but instead there's a
/// monotonically increasing single global integer (when tracing) per creation
/// of Sample that determines order.
#[inline]
pub fn samples_cloned_sorted() -> Vec<Sample> {...}

/// Save tracing data to to supplied path, using the Trace Viewer format. Trace file can be opened
/// using the Chrome browser by visiting the URL `about:tracing`. If `sorted_chronologically` is
/// true then sort output traces chronologically by each trace's time of creation.
#[inline]
pub fn save<P: AsRef<Path>>(path: P, sort: bool) -> Result<(), chrome_trace_dump::Error> {...}
```

## xi-editor-ph7/rust/trace/src/sys_pid.rs

```rust
#[cfg(all(target_family = "unix", not(target_os = "fuchsia")))]
#[inline]
pub fn current_pid() -> u64 {...}

#[cfg(target_os = "fuchsia")]
pub fn current_pid() -> u64 {...}

#[cfg(target_family = "windows")]
#[inline]
pub fn current_pid() -> u64 {...}
```

## xi-editor-ph7/rust/trace/src/sys_tid.rs

```rust
#[cfg(any(target_os = "macos", target_os = "ios"))]
#[inline]
pub fn current_tid() -> Result<u64, libc::c_int> {...}

#[cfg(target_os = "fuchsia")]
#[inline]
pub fn current_tid() -> Result<u64, libc::c_int> {
    // TODO: fill in for fuchsia.  This is the native C API but maybe there are
    // rust-specific bindings already.
    /*
    extern {
        fn thrd_get_zx_handle(thread: thrd_t) -> zx_handle_t;
        fn thrd_current() -> thrd_t;
    }

    Ok(thrd_get_zx_handle(thrd_current()) as u64)
    */
    Ok(0)
}

#[cfg(any(target_os = "linux", target_os = "android"))]
#[inline]
pub fn current_tid() -> Result<u64, libc::c_int> {...}

// TODO: maybe use https://github.com/alexcrichton/cfg-if to simplify this?
// pthread-based fallback
#[cfg(all(
    target_family = "unix",
    not(any(
        target_os = "macos",
        target_os = "ios",
        target_os = "linux",
        target_os = "android",
        target_os = "fuchsia"
    ))
))]
pub fn current_tid() -> Result<u64, libc::c_int> {...}

#[cfg(target_os = "windows")]
#[inline]
pub fn current_tid() -> Result<u64, libc::c_int> {...}
```

