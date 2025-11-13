## xi-editor-ph7/rust/plugin-lib/src/base_cache.rs

```rust
use memchr::memchr;

use crate::trace::trace_block;
use crate::xi_core::plugin_rpc::{GetDataResponse, TextUnit};
use xi_rope::interval::IntervalBounds;
use xi_rope::{DeltaElement, Interval, LinesMetric, Rope, RopeDelta};

use super::{Cache, DataSource, Error};

#[cfg(not(test))]
const CHUNK_SIZE: usize = 1024 * 1024;


/// A simple cache, holding a single contiguous chunk of the document.
#[derive(Debug, Clone, Default)]
pub struct ChunkCache {
    /// The position of this chunk relative to the tracked document.
    /// All offsets are guaranteed to be valid UTF-8 character boundaries.
    pub offset: usize,
    /// A chunk of the remote buffer.
    pub contents: String,
    /// The (zero-based) line number of the line containing the start of the chunk.
    pub first_line: usize,
    /// The byte offset of the start of the chunk from the start of `first_line`.
    /// If this chunk starts at a line break, this will be 0.
    pub first_line_offset: usize,
    /// A list of indexes of newlines in this chunk.
    pub line_offsets: Vec<usize>,
    /// The total size of the tracked document.
    pub buf_size: usize,
    pub num_lines: usize,
    pub rev: u64,
}

impl Cache for ChunkCache {
    fn new(buf_size: usize, rev: u64, num_lines: usize) -> Self {...}

    /// Returns the line at `line_num` (zero-indexed). Returns an `Err(_)` if
    /// there is a problem connecting to the peer, or if the requested line
    /// is out of bounds.
    ///
    /// The `source` argument is some type that implements [`DataSource`]; in
    /// the general case this is backed by the remote peer.
    ///
    /// # Errors
    ///
    /// Returns an error if `line_num` is greater than the total number of lines
    /// in the document, or if there is a problem communicating with `source`.
    ///
    /// [`DataSource`]: trait.DataSource.html
    fn get_line<DS>(&mut self, source: &DS, line_num: usize) -> Result<&str, Error>
    where
        DS: DataSource,
    {...}

    fn get_region<DS, I>(&mut self, source: &DS, interval: I) -> Result<&str, Error>
    where
        DS: DataSource,
        I: IntervalBounds,
    {...}

    // could reimplement this with get_region, but this doesn't bloat the cache.
    // Not clear that's a win, though, since if we're using this at all caching
    // is probably worth it?
    fn get_document<DS: DataSource>(&mut self, source: &DS) -> Result<String, Error> {...}

    fn offset_of_line<DS: DataSource>(
        &mut self,
        source: &DS,
        line_num: usize,
    ) -> Result<usize, Error> {...}

    fn line_of_offset<DS: DataSource>(
        &mut self,
        source: &DS,
        offset: usize,
    ) -> Result<usize, Error> {...}

    /// Updates the chunk to reflect changes in this delta.
    fn update(&mut self, delta: Option<&RopeDelta>, new_len: usize, num_lines: usize, rev: u64) {...}

    fn clear(&mut self) {...}
}

impl ChunkCache {
    /// Returns the offset of the provided `line_num` if it can be determined
    /// without fetching data. The offset of line 0 is always 0, and there
    /// is an implicit line at the last offset in the buffer.
    fn cached_offset_of_line(&self, line_num: usize) -> Option<usize> {...}

    /// Clears anything in the cache up to `offset`, which is indexed relative
    /// to `self.contents`.
    ///
    /// # Panics
    ///
    /// Panics if `offset` is not a character boundary, or if `offset` is greater than
    /// the length of `self.content`.
    fn clear_up_to(&mut self, offset: usize) {...}

    /// Discard any existing cache, starting again with the new data.
    fn reset_chunk(&mut self, data: GetDataResponse) {...}

    /// Append to the existing cache, leaving existing data in place.
    fn append_chunk(&mut self, data: &GetDataResponse) {...}

    fn recalculate_line_offsets(&mut self) {...}

    /// Determine whether we should update our state with this delta,
    /// or if we should clear it. In the update case, also patches up
    /// offsets.
    fn should_clear(&mut self, delta: &RopeDelta) -> bool {...}

    /// Patches up `self.line_offsets` in the simple insert case.
    fn simple_insert(&mut self, text: &Rope, ins_offset: usize) {...}

    /// Patches up `self.line_offsets` in the simple delete case.
    fn simple_delete(&mut self, start: usize, end: usize) {...}

    /// Updates `self.contents` with the given delta.
    fn update_chunk(&mut self, delta: &RopeDelta) {...}
}

/// Calculates the offsets of newlines in `text`,
/// inserting the results into `storage`. The offsets are the offset
/// of the start of the line, not the line break character.
fn newline_offsets(text: &str, storage: &mut Vec<usize>) {...}
```

## xi-editor-ph7/rust/plugin-lib/src/core_proxy.rs

```rust
use crate::xi_core::plugin_rpc::Hover;
use crate::xi_core::plugins::PluginId;
use crate::xi_core::ViewId;
use xi_rpc::{RemoteError, RpcCtx, RpcPeer};

#[derive(Clone)]
pub struct CoreProxy {
    plugin_id: PluginId,
    peer: RpcPeer,
}

impl CoreProxy {
    pub fn new(plugin_id: PluginId, rpc_ctx: &RpcCtx) -> Self {...}

    pub fn add_status_item(&mut self, view_id: ViewId, key: &str, value: &str, alignment: &str) {...}

    pub fn update_status_item(&mut self, view_id: ViewId, key: &str, value: &str) {...}

    pub fn remove_status_item(&mut self, view_id: ViewId, key: &str) {...}

    pub fn display_hover(
        &mut self,
        view_id: ViewId,
        request_id: usize,
        result: &Result<Hover, RemoteError>,
    ) {...}

    pub fn schedule_idle(&mut self, view_id: ViewId) {...}
}
```

## xi-editor-ph7/rust/plugin-lib/src/dispatch.rs

```rust
use std::collections::HashMap;
use std::path::PathBuf;

use serde_json::{self, Value};

use crate::core_proxy::CoreProxy;
use crate::xi_core::plugin_rpc::{HostNotification, HostRequest, PluginBufferInfo, PluginUpdate};
use crate::xi_core::{ConfigTable, LanguageId, PluginPid, ViewId};
use xi_rpc::{Handler as RpcHandler, RemoteError, RpcCtx};

use crate::trace::{trace, trace_block, trace_block_payload};

use super::{Plugin, View};

/// Convenience for unwrapping a view, when handling RPC notifications.
macro_rules! bail {
    ($opt:expr, $method:expr, $pid:expr, $view:expr) => {
        match $opt {
            Some(t) => t,
            None => {
                warn!("{:?} missing {:?} for {:?}", $pid, $view, $method);
                return;
            }
        }
    };
}

/// Convenience for unwrapping a view when handling RPC requests.
/// Prints an error if the view is missing, and returns an appropriate error.
macro_rules! bail_err {
    ($opt:expr, $method:expr, $pid:expr, $view:expr) => {
        match $opt {
            Some(t) => t,
            None => {
                warn!("{:?} missing {:?} for {:?}", $pid, $view, $method);
                return Err(RemoteError::custom(404, "missing view", None));
            }
        }
    };
}

/// Handles raw RPCs from core, updating state and forwarding calls
/// to the plugin,
pub struct Dispatcher<'a, P: 'a + Plugin> {
    //TODO: when we add multi-view, this should be an Arc+Mutex/Rc+RefCell
    views: HashMap<ViewId, View<P::Cache>>,
    pid: Option<PluginPid>,
    plugin: &'a mut P,
}

impl<'a, P: 'a + Plugin> Dispatcher<'a, P> {
    pub(crate) fn new(plugin: &'a mut P) -> Self {...}

    fn do_initialize(
        &mut self,
        ctx: &RpcCtx,
        plugin_id: PluginPid,
        buffers: Vec<PluginBufferInfo>,
    ) {...}

    fn do_did_save(&mut self, view_id: ViewId, path: PathBuf) {...}

    fn do_config_changed(&mut self, view_id: ViewId, changes: &ConfigTable) {...}

    fn do_language_changed(&mut self, view_id: ViewId, new_lang: LanguageId) {...}

    fn do_custom_command(&mut self, view_id: ViewId, method: &str, params: Value) {...}

    fn do_new_buffer(&mut self, ctx: &RpcCtx, buffers: Vec<PluginBufferInfo>) {...}

    fn do_close(&mut self, view_id: ViewId) {...}

    fn do_shutdown(&mut self) {...}

    fn do_get_hover(&mut self, view_id: ViewId, request_id: usize, position: usize) {...}

    fn do_tracing_config(&mut self, enabled: bool) {...}

    fn do_update(&mut self, update: PluginUpdate) -> Result<Value, RemoteError> {...}

    fn do_collect_trace(&self) -> Result<Value, RemoteError> {...}
}

impl<'a, P: Plugin> RpcHandler for Dispatcher<'a, P> {
    type Notification = HostNotification;
    type Request = HostRequest;

    fn handle_notification(&mut self, ctx: &RpcCtx, rpc: Self::Notification) {...}

    fn handle_request(&mut self, _ctx: &RpcCtx, rpc: Self::Request) -> Result<Value, RemoteError> {...}

    fn idle(&mut self, _ctx: &RpcCtx, token: usize) {...}
}
```

## xi-editor-ph7/rust/plugin-lib/src/lib.rs

```rust
extern crate xi_core_lib as xi_core;
extern crate xi_rope;
extern crate xi_rpc;
#[cfg(feature = "trace")]
extern crate xi_trace;
#[macro_use]
extern crate serde_json;
extern crate bytecount;
extern crate memchr;
extern crate rand;
extern crate serde;

#[macro_use]
extern crate log;

mod base_cache;
mod core_proxy;
mod dispatch;
mod state_cache;
pub mod trace;
mod view;

use std::io;
use std::path::Path;

use crate::xi_core::plugin_rpc::{GetDataResponse, TextUnit};
use crate::xi_core::{ConfigTable, LanguageId};
use serde_json::Value;
use xi_rope::interval::IntervalBounds;
use xi_rope::RopeDelta;
use xi_rpc::{ReadError, RpcLoop};

use self::dispatch::Dispatcher;

pub use crate::base_cache::ChunkCache;
pub use crate::core_proxy::CoreProxy;
pub use crate::state_cache::StateCache;
pub use crate::view::View;
pub use crate::xi_core::plugin_rpc::{Hover, Range};

/// Abstracts getting data from the peer. Mainly exists for mocking in tests.
pub trait DataSource {
    fn get_data(
        &self,
        start: usize,
        unit: TextUnit,
        max_size: usize,
        rev: u64,
    ) -> Result<GetDataResponse, Error>;
}

/// A generic interface for types that cache a remote document.
///
/// In general, users of this library should not need to implement this trait;
/// we provide two concrete Cache implementations, [`ChunkCache`] and
/// [`StateCache`]. If however a plugin's particular needs are not met by
/// those implementations, a user may choose to implement their own.
///
/// [`ChunkCache`]: ../base_cache/struct.ChunkCache.html
/// [`StateCache`]: ../state_cache/struct.StateCache.html
pub trait Cache {
    /// Create a new instance of this type; instances are created automatically
    /// as relevant views are added.
    fn new(buf_size: usize, rev: u64, num_lines: usize) -> Self;
    /// Returns the line at `line_num` (zero-indexed). Returns an `Err(_)` if
    /// there is a problem connecting to the peer, or if the requested line
    /// is out of bounds.
    ///
    /// The `source` argument is some type that implements [`DataSource`]; in
    /// the general case this is backed by the remote peer.
    ///
    /// [`DataSource`]: trait.DataSource.html
    fn get_line<DS: DataSource>(&mut self, source: &DS, line_num: usize) -> Result<&str, Error>;

    /// Returns the specified region of the buffer. Returns an `Err(_)` if
    /// there is a problem connecting to the peer, or if the requested line
    /// is out of bounds.
    ///
    /// The `source` argument is some type that implements [`DataSource`]; in
    /// the general case this is backed by the remote peer.
    ///
    /// [`DataSource`]: trait.DataSource.html
    fn get_region<DS, I>(&mut self, source: &DS, interval: I) -> Result<&str, Error>
    where
        DS: DataSource,
        I: IntervalBounds;

    /// Returns the entire contents of the remote document, fetching as needed.
    fn get_document<DS: DataSource>(&mut self, source: &DS) -> Result<String, Error>;

    /// Returns the offset of the line at `line_num`, zero-indexed, fetching
    /// data from `source` if needed.
    ///
    /// # Errors
    ///
    /// Returns an error if `line_num` is greater than the total number of lines
    /// in the document, or if there is a problem communicating with `source`.
    fn offset_of_line<DS: DataSource>(
        &mut self,
        source: &DS,
        line_num: usize,
    ) -> Result<usize, Error>;
    /// Returns the index of the line containing `offset`, fetching
    /// data from `source` if needed.
    ///
    /// # Errors
    ///
    /// Returns an error if `offset` is greater than the total length of
    /// the document, or if there is a problem communicating with `source`.
    fn line_of_offset<DS: DataSource>(
        &mut self,
        source: &DS,
        offset: usize,
    ) -> Result<usize, Error>;
    /// Updates the cache by applying this delta.
    fn update(&mut self, delta: Option<&RopeDelta>, buf_size: usize, num_lines: usize, rev: u64);
    /// Flushes any state held by this cache.
    fn clear(&mut self);
}

/// An interface for plugins.
///
/// Users of this library must implement this trait for some type.
pub trait Plugin {
    type Cache: Cache;

    /// Called when the Plugin is initialized. The plugin receives CoreProxy
    /// object that is a wrapper around the RPC Peer and can be used to call
    /// related methods on the Core in a type-safe manner.
    #[allow(unused_variables)]
    fn initialize(&mut self, core: CoreProxy) {...}

    /// Called when an edit has occurred in the remote view. If the plugin wishes
    /// to add its own edit, it must do so using asynchronously via the edit notification.
    fn update(
        &mut self,
        view: &mut View<Self::Cache>,
        delta: Option<&RopeDelta>,
        edit_type: String,
        author: String,
    );
    /// Called when a buffer has been saved to disk. The buffer's previous
    /// path, if one existed, is passed as `old_path`.
    fn did_save(&mut self, view: &mut View<Self::Cache>, old_path: Option<&Path>);
    /// Called when a view has been closed. By the time this message is received,
    /// It is possible to send messages to this view. The plugin may wish to
    /// perform cleanup, however.
    fn did_close(&mut self, view: &View<Self::Cache>);
    /// Called when there is a new view that this buffer is interested in.
    /// This is called once per view, and is paired with a call to
    /// `Plugin::did_close` when the view is closed.
    fn new_view(&mut self, view: &mut View<Self::Cache>);

    /// Called when a config option has changed for this view. `changes`
    /// is a map of keys/values that have changed; previous values are available
    /// in the existing config, accessible through `view.get_config()`.
    fn config_changed(&mut self, view: &mut View<Self::Cache>, changes: &ConfigTable);

    /// Called when syntax language has changed for this view.
    /// New language is available in the `view`, and old language is available in `old_lang`.
    #[allow(unused_variables)]
    fn language_changed(&mut self, view: &mut View<Self::Cache>, old_lang: LanguageId) {...}

    /// Called with a custom command.
    #[allow(unused_variables)]
    fn custom_command(&mut self, view: &mut View<Self::Cache>, method: &str, params: Value) {...}

    /// Called when the runloop is idle, if the plugin has previously
    /// asked to be scheduled via `View::schedule_idle()`. Plugins that
    /// are doing things like full document analysis can use this mechanism
    /// to perform their work incrementally while remaining responsive.
    #[allow(unused_variables)]
    fn idle(&mut self, view: &mut View<Self::Cache>) {...}

    /// Language Plugins specific methods

    #[allow(unused_variables)]
    fn get_hover(&mut self, view: &mut View<Self::Cache>, request_id: usize, position: usize) {...}
}

#[derive(Debug)]
pub enum Error {
    RpcError(xi_rpc::Error),
    WrongReturnType,
    BadRequest,
    PeerDisconnect,
    // Just used in tests
    Other(String),
}

/// Run `plugin` until it exits, blocking the current thread.
pub fn mainloop<P: Plugin>(plugin: &mut P) -> Result<(), ReadError> {...}
```

## xi-editor-ph7/rust/plugin-lib/src/state_cache.rs

```rust
use rand::{thread_rng, Rng};

use crate::trace::trace_block;
use xi_rope::interval::IntervalBounds;
use xi_rope::{LinesMetric, RopeDelta};

use super::{Cache, DataSource, Error, View};
use crate::base_cache::ChunkCache;

const CACHE_SIZE: usize = 1024;

/// Number of probes for eviction logic.
const NUM_PROBES: usize = 5;

struct CacheEntry<S> {
    line_num: usize,
    offset: usize,
    user_state: Option<S>,
}

/// The caching state
#[derive(Default)]
pub struct StateCache<S> {
    pub(crate) buf_cache: ChunkCache,
    state_cache: Vec<CacheEntry<S>>,
    /// The frontier, represented as a sorted list of line numbers.
    frontier: Vec<usize>,
}

impl<S: Clone + Default> Cache for StateCache<S> {
    fn new(buf_size: usize, rev: u64, num_lines: usize) -> Self {...}

    fn get_line<DS: DataSource>(&mut self, source: &DS, line_num: usize) -> Result<&str, Error> {...}

    fn get_region<DS, I>(&mut self, source: &DS, interval: I) -> Result<&str, Error>
    where
        DS: DataSource,
        I: IntervalBounds,
    {...}

    fn get_document<DS: DataSource>(&mut self, source: &DS) -> Result<String, Error> {...}

    fn offset_of_line<DS: DataSource>(
        &mut self,
        source: &DS,
        line_num: usize,
    ) -> Result<usize, Error> {...}

    fn line_of_offset<DS: DataSource>(
        &mut self,
        source: &DS,
        offset: usize,
    ) -> Result<usize, Error> {...}

    /// Updates the cache by applying this delta.
    fn update(&mut self, delta: Option<&RopeDelta>, buf_size: usize, num_lines: usize, rev: u64) {...}

    /// Flushes any state held by this cache.
    fn clear(&mut self) {...}
}

impl<S: Clone + Default> StateCache<S> {
    /// Find an entry in the cache by line num. On return `Ok(i)` means entry
    /// at index `i` is an exact match, while `Err(i)` means the entry would be
    /// inserted at `i`.
    fn find_line(&self, line_num: usize) -> Result<usize, usize> {...}

    /// Find an entry in the cache by offset. Similar to `find_line`.
    pub fn find_offset(&self, offset: usize) -> Result<usize, usize> {...}

    /// Get the state from the nearest cache entry at or before given line number.
    /// Returns line number, offset, and user state.
    pub fn get_prev(&self, line_num: usize) -> (usize, usize, S) {...}

    /// Get the state at the given line number, if it exists in the cache.
    pub fn get(&self, line_num: usize) -> Option<&S> {...}

    /// Set the state at the given line number. Note: has no effect if line_num
    /// references the end of the partial line at EOF.
    pub fn set<DS>(&mut self, source: &DS, line_num: usize, s: S)
    where
        DS: DataSource,
    {...}

    /// Get the cache entry at the given line number, creating it if necessary.
    /// Returns None if line_num > number of newlines in doc (ie if it references
    /// the end of the partial line at EOF).
    fn get_entry<DS>(&mut self, source: &DS, line_num: usize) -> Option<&mut CacheEntry<S>>
    where
        DS: DataSource,
    {...}

    /// Insert a new entry into the cache, returning its index.
    fn insert_entry(&mut self, line_num: usize, offset: usize, user_state: Option<S>) -> usize {...}

    /// Evict one cache entry.
    fn evict(&mut self) {...}

    fn choose_victim(&self) -> usize {...}

    /// Compute the gap that would result after deleting the given entry.
    fn compute_gap(&self, ix: usize) -> usize {...}

    /// Release all state _after_ the given offset.
    fn truncate_cache(&mut self, offset: usize) {...}

    pub(crate) fn truncate_frontier(&mut self, line_num: usize) {...}

    /// Updates the line cache to reflect this delta.
    fn update_line_cache(&mut self, delta: &RopeDelta) {...}

    fn line_cache_simple_insert(&mut self, start: usize, new_len: usize, newline_num: usize) {...}

    fn line_cache_simple_delete(&mut self, start: usize, end: usize) {...}

    fn patchup_frontier(&mut self, cache_idx: usize, nl_count_delta: isize) {...}

    /// Clears any cached text and anything in the state cache before `start`.
    fn clear_to_start(&mut self, start: usize) {...}

    /// Clear all state and reset frontier to start.
    pub fn reset(&mut self) {...}

    /// The frontier keeps track of work needing to be done. A typical
    /// user will call `get_frontier` to get a line number, do the work
    /// on that line, insert state for the next line, and then call either
    /// `update_frontier` or `close_frontier` depending on whether there
    /// is more work to be done at that location.
    pub fn get_frontier(&self) -> Option<usize> {...}

    /// Updates the frontier. This can go backward, but most typically
    /// goes forward by 1 line (compared to the `get_frontier` result).
    pub fn update_frontier(&mut self, new_frontier: usize) {...}

    /// Closes the current frontier. This is the correct choice to handle
    /// EOF.
    pub fn close_frontier(&mut self) {...}
}

/// StateCache specific extensions on `View`
impl<S: Default + Clone> View<StateCache<S>> {
    pub fn get_frontier(&self) -> Option<usize> {...}

    pub fn get_prev(&self, line_num: usize) -> (usize, usize, S) {...}

    pub fn get(&self, line_num: usize) -> Option<&S> {...}

    pub fn set(&mut self, line_num: usize, s: S) {...}

    pub fn update_frontier(&mut self, new_frontier: usize) {...}

    pub fn close_frontier(&mut self) {...}

    pub fn reset(&mut self) {...}

    pub fn find_offset(&self, offset: usize) -> Result<usize, usize> {...}
}

fn count_newlines(s: &str) -> usize {...}
```

## xi-editor-ph7/rust/plugin-lib/src/trace.rs

```rust
#[cfg(feature = "trace")]
pub use xi_trace::{
    chrome_trace_dump, disable_tracing, enable_tracing, is_enabled, samples_cloned_unsorted, trace,
    trace_block, trace_block_payload, trace_payload, Sample, SampleGuard,
};

#[cfg(not(feature = "trace"))]
mod shim {
    use serde_json::Value;
    use std::io::Write;
    use std::marker::PhantomData;

    #[derive(Clone, Debug, Default, PartialEq, Eq, PartialOrd, Ord)]
    pub struct Sample;

    #[derive(Debug, Default)]
    pub struct SampleGuard<'a>(PhantomData<&'a ()>);

    impl<'a> Drop for SampleGuard<'a> {
        fn drop(&mut self) {...}
    }

    pub fn trace<S, C>(_name: S, _categories: C) {...}

    pub fn trace_payload<S, C, P>(_name: S, _categories: C, _payload: P) {...}

    pub fn trace_block<'a, S, C>(_name: S, _categories: C) -> SampleGuard<'a> {...}

    pub fn trace_block_payload<'a, S, C, P>(
        _name: S,
        _categories: C,
        _payload: P,
    ) -> SampleGuard<'a> {...}

    pub fn enable_tracing() {...}

    pub fn disable_tracing() {...}

    pub fn is_enabled() -> bool {...}

    pub fn samples_cloned_unsorted() -> Vec<Sample> {...}

    pub mod chrome_trace_dump {
        use super::Sample;
        use serde_json::Value;
        use std::io::Write;

        #[derive(Clone, Debug)]
        pub struct Error;

        pub fn decode(_samples: Value) -> Result<Vec<Sample>, Error> {...}

        pub fn serialize<W>(_samples: &Vec<Sample>, _output: W) -> Result<(), Error>
        where
            W: Write,
        {...}

        pub fn to_value(_samples: &Vec<Sample>) -> Result<Value, Error> {...}
    }

    pub use chrome_trace_dump;
    pub use SampleGuard;
}

#[cfg(not(feature = "trace"))]
pub use shim::{
    chrome_trace_dump, disable_tracing, enable_tracing, is_enabled, samples_cloned_unsorted, trace,
    trace_block, trace_block_payload, trace_payload, Sample, SampleGuard,
};
```

## xi-editor-ph7/rust/plugin-lib/src/view.rs

```rust
use serde::Deserialize;
use serde_json::{self, Value};
use std::path::{Path, PathBuf};

use crate::trace::trace_block;
use crate::xi_core::plugin_rpc::{
    GetDataResponse, PluginBufferInfo, PluginEdit, ScopeSpan, TextUnit,
};
use crate::xi_core::{BufferConfig, ConfigTable, LanguageId, PluginPid, ViewId};
use xi_core_lib::annotations::AnnotationType;
use xi_core_lib::plugin_rpc::DataSpan;
use xi_rope::interval::IntervalBounds;
use xi_rope::RopeDelta;

use xi_rpc::RpcPeer;

use super::{Cache, DataSource, Error};

/// A type that acts as a proxy for a remote view. Provides access to
/// a document cache, and implements various methods for querying and modifying
/// view state.
pub struct View<C> {
    pub(crate) cache: C,
    pub(crate) peer: RpcPeer,
    pub(crate) path: Option<PathBuf>,
    pub(crate) config: BufferConfig,
    pub(crate) config_table: ConfigTable,
    plugin_id: PluginPid,
    // TODO: this is only public to avoid changing the syntect impl
    // this should go away with async edits
    pub rev: u64,
    pub undo_group: Option<usize>,
    buf_size: usize,
    pub(crate) view_id: ViewId,
    pub(crate) language_id: LanguageId,
}

impl<C: Cache> View<C> {
    pub(crate) fn new(peer: RpcPeer, plugin_id: PluginPid, info: PluginBufferInfo) -> Self {...}

    pub(crate) fn update(
        &mut self,
        delta: Option<&RopeDelta>,
        new_len: usize,
        new_num_lines: usize,
        rev: u64,
        undo_group: Option<usize>,
    ) {...}

    pub(crate) fn set_language(&mut self, new_language_id: LanguageId) {...}

    //NOTE: (discuss in review) this feels bad, but because we're mutating cache,
    // which we own, we can't just pass in a reference to something else we own;
    // so we create this on each call. The `clone`is only cloning an `Arc`,
    // but we could maybe use a RefCell or something and make this cleaner.
    /// Returns a `FetchCtx`, a thin wrapper around an RpcPeer that implements
    /// the `DataSource` trait and can be used when updating a cache.
    pub(crate) fn make_ctx(&self) -> FetchCtx {...}

    /// Returns the length of the view's buffer, in bytes.
    pub fn get_buf_size(&self) -> usize {...}

    pub fn get_path(&self) -> Option<&Path> {...}

    pub fn get_language_id(&self) -> &LanguageId {...}

    pub fn get_config(&self) -> &BufferConfig {...}

    pub fn get_cache(&mut self) -> &mut C {...}

    pub fn get_id(&self) -> ViewId {...}

    pub fn get_line(&mut self, line_num: usize) -> Result<&str, Error> {...}

    /// Returns a region of the view's buffer.
    pub fn get_region<I: IntervalBounds>(&mut self, interval: I) -> Result<&str, Error> {...}

    pub fn get_document(&mut self) -> Result<String, Error> {...}

    pub fn offset_of_line(&mut self, line_num: usize) -> Result<usize, Error> {...}

    pub fn line_of_offset(&mut self, offset: usize) -> Result<usize, Error> {...}

    pub fn add_scopes(&self, scopes: &[Vec<String>]) {...}

    pub fn edit(
        &self,
        delta: RopeDelta,
        priority: u64,
        after_cursor: bool,
        new_undo_group: bool,
        author: String,
    ) {...}

    pub fn update_spans(&self, start: usize, len: usize, spans: &[ScopeSpan]) {...}

    pub fn update_annotations(
        &self,
        start: usize,
        len: usize,
        annotation_spans: &[DataSpan],
        annotation_type: &AnnotationType,
    ) {...}

    pub fn schedule_idle(&self) {...}

    /// Returns `true` if an incoming RPC is pending. This is intended
    /// to reduce latency for bulk operations done in the background.
    pub fn request_is_pending(&self) -> bool {...}

    pub fn add_status_item(&self, key: &str, value: &str, alignment: &str) {...}

    pub fn update_status_item(&self, key: &str, value: &str) {...}

    pub fn remove_status_item(&self, key: &str) {...}
}

/// A simple wrapper type that acts as a `DataSource`.
pub struct FetchCtx {
    plugin_id: PluginPid,
    view_id: ViewId,
    peer: RpcPeer,
}

impl DataSource for FetchCtx {
    fn get_data(
        &self,
        start: usize,
        unit: TextUnit,
        max_size: usize,
        rev: u64,
    ) -> Result<GetDataResponse, Error> {...}
}
```

