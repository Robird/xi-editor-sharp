## xi-editor-ph7/rust/core-lib/src/annotations.rs

```rust
use serde::de::{Deserialize, Deserializer};
use serde::ser::{Serialize, SerializeSeq, Serializer};
use serde_json::{self, Value};

use std::collections::HashMap;

use crate::line_offset::LineOffset;
use crate::plugins::PluginId;
use crate::view::View;
use crate::xi_rope::spans::Spans;
use crate::xi_rope::{Interval, Rope};

#[derive(Serialize, Deserialize, Debug, Clone, PartialEq)]
pub enum AnnotationType {
    Selection,
    Find,
    Other(String),
}

impl AnnotationType {
    fn as_str(&self) -> &str {...}
}

/// Location and range of an annotation ([start_line, start_col, end_line, end_col]).
/// Location and range of an annotation
#[derive(Debug, Default, Clone, Copy, PartialEq)]
pub struct AnnotationRange {
    pub start_line: usize,
    pub start_col: usize,
    pub end_line: usize,
    pub end_col: usize,
}

impl Serialize for AnnotationRange {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {...}
}

impl<'de> Deserialize<'de> for AnnotationRange {
    fn deserialize<D>(deserializer: D) -> Result<AnnotationRange, D::Error>
    where
        D: Deserializer<'de>,
    {...}
}

/// A set of annotations of a given type.
#[derive(Debug, Clone)]
pub struct Annotations {
    pub items: Spans<Value>,
    pub annotation_type: AnnotationType,
}

impl Annotations {
    /// Update the annotations in `interval` with the provided `items`.
    pub fn update(&mut self, interval: Interval, items: Spans<Value>) {...}

    /// Remove annotations intersecting `interval`.
    pub fn invalidate(&mut self, interval: Interval) {...}
}

/// A region of an `Annotation`.
#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct AnnotationSlice {
    annotation_type: AnnotationType,
    /// Annotation occurrences, guaranteed non-descending start order.
    ranges: Vec<AnnotationRange>,
    /// If present, one payload per range.
    payloads: Option<Vec<Value>>,
}

impl AnnotationSlice {
    pub fn new(
        annotation_type: AnnotationType,
        ranges: Vec<AnnotationRange>,
        payloads: Option<Vec<Value>>,
    ) -> Self {...}

    /// Returns json representation.
    pub fn to_json(&self) -> Value {...}
}

/// A trait for types (like `Selection`) that have a distinct representation
/// in core but are presented to the frontend as annotations.
pub trait ToAnnotation {
    /// Returns annotations that overlap the provided interval.
    fn get_annotations(&self, interval: Interval, view: &View, text: &Rope) -> AnnotationSlice;
}

/// All the annotations for a given view
pub struct AnnotationStore {
    store: HashMap<PluginId, Vec<Annotations>>,
}

impl AnnotationStore {
    pub fn new() -> Self {...}

    /// Invalidates and removes all annotations in the range of the interval.
    pub fn invalidate(&mut self, interval: Interval) {...}

    /// Applies an update from a plugin to a set of annotations
    pub fn update(&mut self, source: PluginId, interval: Interval, item: Annotations) {...}

    /// Returns an iterator which produces, for each type of annotation,
    /// those annotations which intersect the given interval.
    pub fn iter_range<'c>(
        &'c self,
        view: &'c View,
        text: &'c Rope,
        interval: Interval,
    ) -> impl Iterator<Item = AnnotationSlice> + 'c {...}

    /// Removes any annotations provided by this plugin
    pub fn clear(&mut self, plugin: PluginId) {...}
}
```

## xi-editor-ph7/rust/core-lib/src/backspace.rs

```rust
use xi_rope::{Cursor, Rope};

use crate::config::BufferItems;
use crate::line_offset::{LineOffset, LogicalLines};
use crate::selection::SelRegion;
use xi_unicode::*;

#[allow(clippy::cognitive_complexity)]
pub fn offset_for_delete_backwards(region: &SelRegion, text: &Rope, config: &BufferItems) -> usize {...}
```

## xi-editor-ph7/rust/core-lib/src/client.rs

```rust
use std::time::Instant;

use serde_json::{self, Value};
use xi_rpc::{self, RpcPeer};

use crate::config::Table;
use crate::plugins::rpc::ClientPluginInfo;
use crate::plugins::Command;
use crate::styles::ThemeSettings;
use crate::syntax::LanguageId;
use crate::tabs::ViewId;
use crate::width_cache::{WidthReq, WidthResponse};

/// An interface to the frontend.
pub struct Client(RpcPeer);

impl Client {
    pub fn new(peer: RpcPeer) -> Self {...}

    pub fn update_view(&self, view_id: ViewId, update: &Update) {...}

    pub fn scroll_to(&self, view_id: ViewId, line: usize, col: usize) {...}

    pub fn config_changed(&self, view_id: ViewId, changes: &Table) {...}

    pub fn available_themes(&self, theme_names: Vec<String>) {...}

    pub fn available_languages(&self, languages: Vec<LanguageId>) {...}

    pub fn theme_changed(&self, name: &str, theme: &ThemeSettings) {...}

    pub fn language_changed(&self, view_id: ViewId, new_lang: &LanguageId) {...}

    /// Notify the client that a plugin has started.
    pub fn plugin_started(&self, view_id: ViewId, plugin: &str) {...}

    /// Notify the client that a plugin has stopped.
    ///
    /// `code` is not currently used; in the future may be used to
    /// pass an exit code.
    pub fn plugin_stopped(&self, view_id: ViewId, plugin: &str, code: i32) {...}

    /// Notify the client of the available plugins.
    pub fn available_plugins(&self, view_id: ViewId, plugins: &[ClientPluginInfo]) {...}

    pub fn update_cmds(&self, view_id: ViewId, plugin: &str, cmds: &[Command]) {...}

    pub fn def_style(&self, style: &Value) {...}

    pub fn find_status(&self, view_id: ViewId, queries: &Value) {...}

    pub fn replace_status(&self, view_id: ViewId, replace: &Value) {...}

    /// Ask front-end to measure widths of strings.
    pub fn measure_width(&self, reqs: &[WidthReq]) -> Result<WidthResponse, xi_rpc::Error> {...}

    pub fn alert<S: AsRef<str>>(&self, msg: S) {...}

    pub fn add_status_item(
        &self,
        view_id: ViewId,
        source: &str,
        key: &str,
        value: &str,
        alignment: &str,
    ) {...}

    pub fn update_status_item(&self, view_id: ViewId, key: &str, value: &str) {...}

    pub fn remove_status_item(&self, view_id: ViewId, key: &str) {...}

    pub fn show_hover(&self, view_id: ViewId, request_id: usize, result: String) {...}

    pub fn schedule_idle(&self, token: usize) {...}

    pub fn schedule_timer(&self, timeout: Instant, token: usize) {...}
}

#[derive(Debug, Serialize)]
pub struct Update {
    pub(crate) ops: Vec<UpdateOp>,
    pub(crate) pristine: bool,
    pub(crate) annotations: Vec<Value>,
}

#[derive(Debug, Serialize)]
pub(crate) struct UpdateOp {
    op: OpType,
    n: usize,
    #[serde(skip_serializing_if = "Option::is_none")]
    lines: Option<Vec<Value>>,
    #[serde(rename = "ln")]
    #[serde(skip_serializing_if = "Option::is_none")]
    first_line_number: Option<usize>,
}

impl UpdateOp {
    pub(crate) fn invalidate(n: usize) -> Self {...}

    pub(crate) fn skip(n: usize) -> Self {...}

    pub(crate) fn copy(n: usize, line: usize) -> Self {...}

    pub(crate) fn insert(lines: Vec<Value>) -> Self {...}

    pub(crate) fn update(lines: Vec<Value>, line_opt: Option<usize>) -> Self {...}
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "lowercase")]
enum OpType {
    #[serde(rename = "ins")]
    Insert,
    Skip,
    Invalidate,
    Copy,
    Update,
}
```

## xi-editor-ph7/rust/core-lib/src/config.rs

```rust
use std::collections::HashMap;
use std::error::Error;
use std::fmt;
use std::fs;
use std::io::{self, Read};
use std::path::{Path, PathBuf};
use std::sync::Arc;

use serde::de::{self, Deserialize};
use serde_json::{self, Value};

use crate::syntax::{LanguageId, Languages};
use crate::tabs::{BufferId, ViewId};

/// Loads the included base config settings.
fn load_base_config() -> Table {...}

/// A map of config keys to settings
pub type Table = serde_json::Map<String, Value>;

/// A `ConfigDomain` describes a level or category of user settings.
#[derive(Debug, Clone, PartialEq, Eq, Hash, Serialize, Deserialize)]
#[serde(rename_all = "snake_case")]
pub enum ConfigDomain {
    /// The general user preferences
    General,
    /// The overrides for a particular syntax.
    Language(LanguageId),
    /// The user overrides for a particular buffer
    UserOverride(BufferId),
    /// The system's overrides for a particular buffer. Only used internally.
    #[serde(skip_deserializing)]
    SysOverride(BufferId),
}

/// The external RPC sends `ViewId`s, which we convert to `BufferId`s
/// internally.
#[derive(Debug, Clone, PartialEq, Eq, Hash, Serialize, Deserialize)]
#[serde(rename_all = "snake_case")]
pub enum ConfigDomainExternal {
    General,
    //TODO: remove this old name
    Syntax(LanguageId),
    Language(LanguageId),
    UserOverride(ViewId),
}

/// The errors that can occur when managing configs.
#[derive(Debug)]
pub enum ConfigError {
    /// The config domain was not recognized.
    UnknownDomain(String),
    /// A file-based config could not be loaded or parsed.
    Parse(PathBuf, toml::de::Error),
    /// The config table contained unexpected values
    UnexpectedItem(serde_json::Error),
    /// An Io Error
    Io(io::Error),
}

/// Represents the common pattern of default settings masked by
/// user settings.
#[derive(Debug)]
pub struct ConfigPair {
    /// A static default configuration, which will never change.
    base: Option<Arc<Table>>,
    /// A variable, user provided configuration. Items here take
    /// precedence over items in `base`.
    user: Option<Arc<Table>>,
    /// A snapshot of base + user.
    cache: Arc<Table>,
}

/// The language associated with a given buffer; this is always detected
/// but can also be manually set by the user.
#[derive(Debug, Clone)]
struct LanguageTag {
    detected: LanguageId,
    user: Option<LanguageId>,
}

#[derive(Debug)]
pub struct ConfigManager {
    /// A map of `ConfigPairs` (defaults + overrides) for all in-use domains.
    configs: HashMap<ConfigDomain, ConfigPair>,
    /// The currently loaded `Languages`.
    languages: Languages,
    /// The language assigned to each buffer.
    buffer_tags: HashMap<BufferId, LanguageTag>,
    /// The configs for any open buffers
    buffer_configs: HashMap<BufferId, BufferConfig>,
    /// If using file-based config, this is the base config directory
    /// (perhaps `$HOME/.config/xi`, by default).
    config_dir: Option<PathBuf>,
    /// An optional client-provided path for bundled resources, such
    /// as plugins and themes.
    extras_dir: Option<PathBuf>,
}

/// A collection of config tables representing a hierarchy, with each
/// table's keys superseding keys in preceding tables.
#[derive(Debug, Clone, Default)]
struct TableStack(Vec<Arc<Table>>);

/// A frozen collection of settings, and their sources.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Config<T> {
    /// The underlying set of config tables that contributed to this
    /// `Config` instance. Used for diffing.
    #[serde(skip)]
    source: TableStack,
    /// The settings themselves, deserialized into some concrete type.
    pub items: T,
}

fn deserialize_tab_size<'de, D>(deserializer: D) -> Result<usize, D::Error>
where
    D: serde::Deserializer<'de>,
{...}

/// The concrete type for buffer-related settings.
#[derive(Debug, Clone, Deserialize, Serialize, PartialEq)]
pub struct BufferItems {
    pub line_ending: String,
    #[serde(deserialize_with = "deserialize_tab_size")]
    pub tab_size: usize,
    pub translate_tabs_to_spaces: bool,
    pub use_tab_stops: bool,
    pub font_face: String,
    pub font_size: f32,
    pub auto_indent: bool,
    pub scroll_past_end: bool,
    pub wrap_width: usize,
    pub word_wrap: bool,
    pub autodetect_whitespace: bool,
    pub surrounding_pairs: Vec<(String, String)>,
    pub save_with_newline: bool,
}

pub type BufferConfig = Config<BufferItems>;

impl ConfigPair {
    /// Creates a new `ConfigPair` with the provided base config.
    fn with_base<T: Into<Option<Table>>>(table: T) -> Self {...}

    /// Returns a new `ConfigPair` with the provided base and the current
    /// user config.
    fn new_with_base<T: Into<Option<Table>>>(&self, table: T) -> Self {...}

    fn set_table(&mut self, user: Table) {...}

    /// Returns the `Table` produced by updating `self.user` with the contents
    /// of `user`, deleting null entries.
    fn table_for_update(&self, user: Table) -> Table {...}

    fn rebuild(&mut self) {...}
}

impl ConfigManager {
    pub fn new(config_dir: Option<PathBuf>, extras_dir: Option<PathBuf>) -> Self {...}

    /// The path of the user's config file, if present.
    pub(crate) fn base_config_file_path(&self) -> Option<PathBuf> {...}

    pub(crate) fn get_plugin_paths(&self) -> Vec<PathBuf> {...}

    /// Adds a new buffer to the config manager, and returns the initial config
    /// `Table` for that buffer. The `path` argument is used to determine
    /// the buffer's default language.
    ///
    /// # Note: The caller is responsible for ensuring the config manager is
    /// notified every time a buffer is added or removed.
    ///
    /// # Panics:
    ///
    /// Panics if `id` already exists.
    pub(crate) fn add_buffer(&mut self, id: BufferId, path: Option<&Path>) -> Table {...}

    /// Updates the default language for the given buffer.
    ///
    /// # Panics:
    ///
    /// Panics if `id` does not exist.
    pub(crate) fn update_buffer_path(&mut self, id: BufferId, path: &Path) -> Option<Table> {...}

    /// Instructs the `ConfigManager` to stop tracking a given buffer.
    ///
    /// # Panics:
    ///
    /// Panics if `id` does not exist.
    pub(crate) fn remove_buffer(&mut self, id: BufferId) {...}

    /// Sets a specific language for the given buffer. This is used if the
    /// user selects a specific language in the frontend, for instance.
    pub(crate) fn override_language(
        &mut self,
        id: BufferId,
        new_lang: LanguageId,
    ) -> Option<Table> {...}

    fn update_buffer_config(&mut self, id: BufferId) -> Option<Table> {...}

    fn update_all_buffer_configs(&mut self) -> Vec<(BufferId, Table)> {...}

    fn generate_buffer_config(&mut self, id: BufferId) -> BufferConfig {...}

    /// Returns a reference to the `BufferConfig` for this buffer.
    ///
    /// # Panics:
    ///
    /// Panics if `id` does not exist. The caller is responsible for ensuring
    /// that the `ConfigManager` is kept up to date as buffers are added/removed.
    pub(crate) fn get_buffer_config(&self, id: BufferId) -> &BufferConfig {...}

    /// Returns the language associated with this buffer.
    ///
    /// # Panics:
    ///
    /// Panics if `id` does not exist.
    pub(crate) fn get_buffer_language(&self, id: BufferId) -> LanguageId {...}

    /// Set the available `LanguageDefinition`s. Overrides any previous values.
    pub fn set_languages(&mut self, languages: Languages) {...}

    fn load_user_config_file(&self, domain: &ConfigDomain) -> Option<Table> {...}

    pub fn language_for_path(&self, path: &Path) -> Option<LanguageId> {...}

    /// Sets the config for the given domain, removing any existing config.
    /// Returns a `Vec` of individual buffer config changes that result from
    /// this update, or a `ConfigError` if `config` is poorly formed.
    pub fn set_user_config(
        &mut self,
        domain: ConfigDomain,
        config: Table,
    ) -> Result<Vec<(BufferId, Table)>, ConfigError> {...}

    /// Returns the `Table` produced by applying `changes` to the current user
    /// config for the given `ConfigDomain`.
    ///
    /// # Note:
    ///
    /// When the user modifys a config _file_, the whole file is read,
    /// and we can just overwrite any existing user config with the newly
    /// loaded one.
    ///
    /// When the client modifies a config via the RPC mechanism, however,
    /// this isn't the case. Instead of sending all config settings with
    /// each update, the client just sends the keys/values they would like
    /// to change. When they would like to remove a previously set key,
    /// they send `Null` as the value for that key.
    ///
    /// This function creates a new table which is the product of updating
    /// any existing table by applying the client's changes. This new table can
    /// then be passed to `Self::set_user_config(..)`, as if it were loaded
    /// from disk.
    pub(crate) fn table_for_update(&mut self, domain: ConfigDomain, changes: Table) -> Table {...}

    /// Returns the `ConfigDomain` relevant to a given file, if one exists.
    pub fn domain_for_path(&self, path: &Path) -> Option<ConfigDomain> {...}

    fn check_table(&self, table: &Table) -> Result<(), ConfigError> {...}

    /// Path to themes sub directory inside config directory.
    /// Creates one if not present.
    pub(crate) fn get_themes_dir(&self) -> Option<PathBuf> {...}

    /// Path to plugins sub directory inside config directory.
    /// Creates one if not present.
    pub(crate) fn get_plugins_dir(&self) -> Option<PathBuf> {...}
}

impl TableStack {
    /// Create a single table representing the final config values.
    fn collate(&self) -> Table {...}

    /// Converts the underlying tables into a static `Config` instance.
    fn into_config<T>(self) -> Config<T>
    where
        for<'de> T: Deserialize<'de>,
    {...}

    /// Walks the tables in priority order, returning the first
    /// occurance of `key`.
    fn get<S: AsRef<str>>(&self, key: S) -> Option<&Value> {...}

    /// Returns a new `Table` containing only those keys and values in `self`
    /// which have changed from `other`.
    fn diff(&self, other: &TableStack) -> Option<Table> {...}
}

impl<T> Config<T> {
    pub fn to_table(&self) -> Table {...}
}

impl<'de, T: Deserialize<'de>> Config<T> {
    /// Returns a `Table` of all the items in `self` which have different
    /// values than in `other`.
    pub fn changes_from(&self, other: Option<&Config<T>>) -> Option<Table> {...}
}

impl ConfigDomain {
    fn file_stem(&self) -> &str {...}
}

impl LanguageTag {
    fn new(detected: LanguageId) -> Self {...}

    fn resolve(&self) -> LanguageId {...}

    /// Set the detected language. Returns `true` if this changes the resolved
    /// language.
    fn set_detected(&mut self, detected: LanguageId) -> bool {...}

    /// Set the user-specified language. Returns `true` if this changes
    /// the resolved language.
    #[allow(dead_code)]
    fn set_user(&mut self, new_lang: Option<LanguageId>) -> bool {...}
}

impl<T: PartialEq> PartialEq for Config<T> {
    fn eq(&self, other: &Config<T>) -> bool {...}
}

impl From<LanguageId> for ConfigDomain {
    fn from(src: LanguageId) -> ConfigDomain {...}
}

impl From<BufferId> for ConfigDomain {
    fn from(src: BufferId) -> ConfigDomain {...}
}

impl fmt::Display for ConfigError {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {...}
}

impl Error for ConfigError {}

impl From<io::Error> for ConfigError {
    fn from(src: io::Error) -> ConfigError {...}
}

impl From<serde_json::Error> for ConfigError {
    fn from(src: serde_json::Error) -> ConfigError {...}
}

/// Creates initial config directory structure
pub(crate) fn init_config_dir(dir: &Path) -> io::Result<()> {...}

/// Attempts to load a config from a file. The config's domain is determined
/// by the file name.
pub(crate) fn try_load_from_file(path: &Path) -> Result<Table, ConfigError> {...}

pub(crate) fn table_from_toml_str(s: &str) -> Result<Table, toml::de::Error> {...}

//adapted from https://docs.rs/crate/config/0.7.0/source/src/file/format/toml.rs
/// Converts between toml (used to write config files) and json
/// (used to store config values internally).
fn from_toml_value(value: toml::Value) -> Value {...}
```

## xi-editor-ph7/rust/core-lib/src/core.rs

```rust
use std::io;
use std::sync::{Arc, Mutex, MutexGuard, Weak};

use serde_json::Value;

use xi_rpc::{Error as RpcError, Handler, ReadError, RemoteError, RpcCtx};

use crate::plugin_rpc::{PluginCommand, PluginNotification, PluginRequest};
use crate::plugins::{Plugin, PluginId};
use crate::rpc::*;
use crate::tabs::{CoreState, ViewId};

/// A reference to the main core state.
///
/// # Note
///
/// Various items of initial setup are dependent on how the client
/// is configured, so we defer instantiating state until we have that
/// information.
pub enum XiCore {
    // TODO: profile startup, and determine what things (such as theme loading)
    // we should be doing before client_init.
    Waiting,
    Running(Arc<Mutex<CoreState>>),
}

/// A weak reference to the main state. This is passed to plugin threads.
#[derive(Clone)]
pub struct WeakXiCore(Weak<Mutex<CoreState>>);

#[allow(dead_code)]
impl XiCore {
    pub fn new() -> Self {...}

    /// Returns `true` if the `client_started` has not been received.
    fn is_waiting(&self) -> bool {...}

    /// Returns a guard to the core state. A convenience around `Mutex::lock`.
    ///
    /// # Panics
    ///
    /// Panics if core has not yet received the `client_started` message.
    pub fn inner(&self) -> MutexGuard<'_, CoreState> {...}

    /// Returns a new reference to the core state, if core is running.
    fn weak_self(&self) -> Option<WeakXiCore> {...}
}

/// Handler for messages originating with the frontend.
impl Handler for XiCore {
    type Notification = CoreNotification;
    type Request = CoreRequest;

    fn handle_notification(&mut self, ctx: &RpcCtx, rpc: Self::Notification) {...}

    fn handle_request(&mut self, _ctx: &RpcCtx, rpc: Self::Request) -> Result<Value, RemoteError> {...}

    fn idle(&mut self, _ctx: &RpcCtx, token: usize) {...}
}

impl WeakXiCore {
    /// Attempts to upgrade the weak reference. Essentially a wrapper
    /// for `Arc::upgrade`.
    fn upgrade(&self) -> Option<XiCore> {...}

    /// Called immediately after attempting to start a plugin,
    /// from the plugin's thread.
    pub fn plugin_connect(&self, plugin: Result<Plugin, io::Error>) {...}

    /// Called from a plugin runloop thread when the runloop exits.
    pub fn plugin_exit(&self, plugin: PluginId, error: Result<(), ReadError>) {...}

    /// Handles the result of an update sent to a plugin.
    ///
    /// All plugins must acknowledge when they are sent a new update, so that
    /// core can track which revisions are still 'live', that is can still
    /// be the base revision for a delta. Once a plugin has acknowledged a new
    /// revision, it can no longer send deltas against any older revision.
    pub fn handle_plugin_update(
        &self,
        plugin: PluginId,
        view: ViewId,
        response: Result<Value, RpcError>,
    ) {...}
}

/// Handler for messages originating from plugins.
impl Handler for WeakXiCore {
    type Notification = PluginCommand<PluginNotification>;
    type Request = PluginCommand<PluginRequest>;

    fn handle_notification(&mut self, ctx: &RpcCtx, rpc: Self::Notification) {...}

    fn handle_request(&mut self, ctx: &RpcCtx, rpc: Self::Request) -> Result<Value, RemoteError> {...}
}
```

## xi-editor-ph7/rust/core-lib/src/edit_ops.rs

```rust
use std::borrow::Cow;
use std::collections::BTreeSet;

use xi_rope::{Cursor, DeltaBuilder, Interval, LinesMetric, Rope, RopeDelta};

use crate::backspace::offset_for_delete_backwards;
use crate::config::BufferItems;
use crate::line_offset::{LineOffset, LogicalLines};
use crate::linewrap::Lines;
use crate::movement::{region_movement, Movement};
use crate::selection::{SelRegion, Selection};
use crate::word_boundaries::WordCursor;

#[derive(Debug, Copy, Clone)]
pub enum IndentDirection {
    In,
    Out,
}

/// Replaces the selection with the text `T`.
pub fn insert<T: Into<Rope>>(base: &Rope, regions: &[SelRegion], text: T) -> RopeDelta {...}

/// Leaves the current selection untouched, but surrounds it with two insertions.
pub fn surround<BT, AT>(
    base: &Rope,
    regions: &[SelRegion],
    before_text: BT,
    after_text: AT,
) -> RopeDelta
where
    BT: Into<Rope>,
    AT: Into<Rope>,
{...}

pub fn duplicate_line(base: &Rope, regions: &[SelRegion], config: &BufferItems) -> RopeDelta {...}

/// Used when the user presses the backspace key. If no delta is returned, then nothing changes.
pub fn delete_backward(base: &Rope, regions: &[SelRegion], config: &BufferItems) -> RopeDelta {...}

/// Common logic for a number of delete methods. For each region in the
/// selection, if the selection is a caret, delete the region between
/// the caret and the movement applied to the caret, otherwise delete
/// the region.
///
/// If `save` is set, the tuple will contain a rope with the deleted text.
///
/// # Arguments
///
/// * `height` - viewport height
pub(crate) fn delete_by_movement(
    base: &Rope,
    regions: &[SelRegion],
    lines: &Lines,
    movement: Movement,
    height: usize,
    save: bool,
) -> (RopeDelta, Option<Rope>) {...}

/// Deletes the given regions.
pub(crate) fn delete_sel_regions(base: &Rope, sel_regions: &[SelRegion]) -> RopeDelta {...}

/// Extracts non-caret selection regions into a string,
/// joining multiple regions with newlines.
pub(crate) fn extract_sel_regions<'a>(
    base: &'a Rope,
    sel_regions: &[SelRegion],
) -> Option<Cow<'a, str>> {...}

pub fn insert_newline(base: &Rope, regions: &[SelRegion], config: &BufferItems) -> RopeDelta {...}

pub fn insert_tab(base: &Rope, regions: &[SelRegion], config: &BufferItems) -> RopeDelta {...}

/// Indents or outdents lines based on selection and user's tab settings.
/// Uses a BTreeSet to holds the collection of lines to modify.
/// Preserves cursor position and current selection as much as possible.
/// Tries to have behavior consistent with other editors like Atom,
/// Sublime and VSCode, with non-caret selections not being modified.
pub fn modify_indent(
    base: &Rope,
    regions: &[SelRegion],
    config: &BufferItems,
    direction: IndentDirection,
) -> RopeDelta {...}

fn indent(base: &Rope, lines: BTreeSet<usize>, tab_text: &str) -> RopeDelta {...}

fn outdent(base: &Rope, lines: BTreeSet<usize>, tab_text: &str) -> RopeDelta {...}

pub fn transpose(base: &Rope, regions: &[SelRegion]) -> RopeDelta {...}

pub fn transform_text<F: Fn(&str) -> String>(
    base: &Rope,
    regions: &[SelRegion],
    transform_function: F,
) -> RopeDelta {...}

/// Changes the number(s) under the cursor(s) with the `transform_function`.
/// If there is a number next to or on the beginning of the region, then
/// this number will be replaced with the result of `transform_function` and
/// the cursor will be placed at the end of the number.
/// Some Examples with a increment `transform_function`:
///
/// "|1234" -> "1235|"
/// "12|34" -> "1235|"
/// "-|12" -> "-11|"
/// "another number is 123|]" -> "another number is 124"
///
/// This function also works fine with multiple regions.
pub fn change_number<F: Fn(i128) -> Option<i128>>(
    base: &Rope,
    regions: &[SelRegion],
    transform_function: F,
) -> RopeDelta {...}

// capitalization behaviour is similar to behaviour in XCode
pub fn capitalize_text(base: &Rope, regions: &[SelRegion]) -> (RopeDelta, Selection) {...}

fn sel_region_to_interval_and_rope(base: &Rope, region: SelRegion) -> (Interval, Rope) {...}

fn last_selection_region(regions: &[SelRegion]) -> Option<&SelRegion> {...}

fn get_tab_text(config: &BufferItems, tab_size: Option<usize>) -> &'static str {...}

fn n_spaces(n: usize) -> &'static str {...}
```

## xi-editor-ph7/rust/core-lib/src/edit_types.rs

```rust
use crate::movement::Movement;
use crate::rpc::{
    EditNotification, FindQuery, GestureType, LineRange, MouseAction, Position,
    SelectionGranularity, SelectionModifier,
};
use crate::view::Size;

/// Events that only modify view state
#[derive(Debug, PartialEq, Clone)]
pub(crate) enum ViewEvent {
    Move(Movement),
    ModifySelection(Movement),
    SelectAll,
    Scroll(LineRange),
    AddSelectionAbove,
    AddSelectionBelow,
    Click(MouseAction),
    Drag(MouseAction),
    Gesture { line: u64, col: u64, ty: GestureType },
    GotoLine { line: u64 },
    Find { chars: String, case_sensitive: bool, regex: bool, whole_words: bool },
    MultiFind { queries: Vec<FindQuery> },
    FindNext { wrap_around: bool, allow_same: bool, modify_selection: SelectionModifier },
    FindPrevious { wrap_around: bool, allow_same: bool, modify_selection: SelectionModifier },
    FindAll,
    HighlightFind { visible: bool },
    SelectionForFind { case_sensitive: bool },
    Replace { chars: String, preserve_case: bool },
    SelectionForReplace,
    SelectionIntoLines,
    CollapseSelections,
}

/// Events that modify the buffer
#[derive(Debug, PartialEq, Clone)]
pub(crate) enum BufferEvent {
    Delete { movement: Movement, kill: bool },
    Backspace,
    Transpose,
    Undo,
    Redo,
    Uppercase,
    Lowercase,
    Capitalize,
    Indent,
    Outdent,
    Insert(String),
    Paste(String),
    InsertNewline,
    InsertTab,
    Yank,
    ReplaceNext,
    ReplaceAll,
    DuplicateLine,
    IncreaseNumber,
    DecreaseNumber,
}

/// An event that needs special handling
#[derive(Debug, PartialEq, Clone)]
pub(crate) enum SpecialEvent {
    DebugRewrap,
    DebugWrapWidth,
    DebugPrintSpans,
    Resize(Size),
    RequestLines(LineRange),
    RequestHover { request_id: usize, position: Option<Position> },
    DebugToggleComment,
    Reindent,
    ToggleRecording(Option<String>),
    PlayRecording(String),
    ClearRecording(String),
}

#[derive(Debug, PartialEq, Clone)]
pub(crate) enum EventDomain {
    View(ViewEvent),
    Buffer(BufferEvent),
    Special(SpecialEvent),
}

impl From<BufferEvent> for EventDomain {
    fn from(src: BufferEvent) -> EventDomain {...}
}

impl From<ViewEvent> for EventDomain {
    fn from(src: ViewEvent) -> EventDomain {...}
}

impl From<SpecialEvent> for EventDomain {
    fn from(src: SpecialEvent) -> EventDomain {...}
}

#[rustfmt::skip]
impl From<EditNotification> for EventDomain {
    fn from(src: EditNotification) -> EventDomain {...}
}
```

## xi-editor-ph7/rust/core-lib/src/editor.rs

```rust
use std::borrow::{Borrow, Cow};
use std::cmp::min;
use std::collections::BTreeSet;

use serde_json::Value;

use crate::trace::{trace_block, trace_payload};
use xi_rope::diff::{Diff, LineHashDiff};
use xi_rope::engine::{Engine, RevId, RevToken};
use xi_rope::rope::count_newlines;
use xi_rope::spans::SpansBuilder;
use xi_rope::{DeltaBuilder, Interval, LinesMetric, Rope, RopeDelta, Transformer};

use crate::annotations::{AnnotationType, Annotations};
use crate::config::BufferItems;
use crate::edit_ops::{self, IndentDirection};
use crate::edit_types::BufferEvent;
use crate::event_context::MAX_SIZE_LIMIT;
use crate::layers::Layers;
use crate::line_offset::{LineOffset, LogicalLines};
use crate::movement::Movement;
use crate::plugins::rpc::{DataSpan, GetDataResponse, PluginEdit, ScopeSpan, TextUnit};
use crate::plugins::PluginId;
use crate::rpc::SelectionModifier;
use crate::selection::{InsertDrift, SelRegion, Selection};
use crate::styles::ThemeStyleMap;
use crate::view::{Replace, View};

#[cfg(not(feature = "ledger"))]
pub struct SyncStore;
#[cfg(feature = "ledger")]
use fuchsia::sync::SyncStore;

// TODO This could go much higher without issue but while developing it is
// better to keep it low to expose bugs in the GC during casual testing.
const MAX_UNDOS: usize = 20;

pub struct Editor {
    /// The contents of the buffer.
    text: Rope,
    /// The CRDT engine, which tracks edit history and manages concurrent edits.
    engine: Engine,

    /// The most recent revision.
    last_rev_id: RevId,
    /// The revision of the last save.
    pristine_rev_id: RevId,
    undo_group_id: usize,
    /// Undo groups that may still be toggled
    live_undos: Vec<usize>,
    /// The index of the current undo; subsequent undos are currently 'undone'
    /// (but may be redone)
    cur_undo: usize,
    /// undo groups that are undone
    undos: BTreeSet<usize>,
    /// undo groups that are no longer live and should be gc'ed
    gc_undos: BTreeSet<usize>,
    force_undo_group: bool,

    this_edit_type: EditType,
    last_edit_type: EditType,

    revs_in_flight: usize,

    /// Used only on Fuchsia for syncing
    #[allow(dead_code)]
    sync_store: Option<SyncStore>,
    #[allow(dead_code)]
    last_synced_rev: RevId,

    layers: Layers,
}

impl Editor {
    /// Creates a new `Editor` with a new empty buffer.
    pub fn new() -> Editor {...}

    /// Creates a new `Editor`, loading text into a new buffer.
    pub fn with_text<T: Into<Rope>>(text: T) -> Editor {...}

    pub(crate) fn get_buffer(&self) -> &Rope {...}

    pub(crate) fn get_layers(&self) -> &Layers {...}

    pub(crate) fn get_layers_mut(&mut self) -> &mut Layers {...}

    pub(crate) fn get_head_rev_token(&self) -> u64 {...}

    pub(crate) fn get_edit_type(&self) -> EditType {...}

    pub(crate) fn get_active_undo_group(&self) -> usize {...}

    pub(crate) fn update_edit_type(&mut self) {...}

    pub(crate) fn set_pristine(&mut self) {...}

    pub(crate) fn is_pristine(&self) -> bool {...}

    /// Set whether or not edits are forced into the same undo group rather than being split by
    /// their EditType.
    ///
    /// This is used for things such as recording playback, where you don't want the
    /// individual events to be undoable, but instead the entire playback should be.
    pub(crate) fn set_force_undo_group(&mut self, force_undo_group: bool) {...}

    /// Sets this Editor's contents to `text`, preserving undo state and cursor
    /// position when possible.
    pub fn reload(&mut self, text: Rope) {...}

    // each outstanding plugin edit represents a rev_in_flight.
    pub fn increment_revs_in_flight(&mut self) {...}

    // GC of CRDT engine is deferred until all plugins have acknowledged the new rev,
    // so when the ack comes back, potentially trigger GC.
    pub fn dec_revs_in_flight(&mut self) {...}

    /// Applies a delta to the text, and updates undo state.
    ///
    /// Records the delta into the CRDT engine so that it can be undone. Also
    /// contains the logic for merging edits into the same undo group. At call
    /// time, self.this_edit_type should be set appropriately.
    ///
    /// This method can be called multiple times, accumulating deltas that will
    /// be committed at once with `commit_delta`. Note that it does not update
    /// the views. Thus, view-associated state such as the selection and line
    /// breaks are to be considered invalid after this method, until the
    /// `commit_delta` call.
    fn add_delta(&mut self, delta: RopeDelta) {...}

    pub(crate) fn calculate_undo_group(&mut self) -> usize {...}

    /// generates a delta from a plugin's response and applies it to the buffer.
    pub fn apply_plugin_edit(&mut self, edit: PluginEdit) {...}

    /// Commits the current delta. If the buffer has changed, returns
    /// a 3-tuple containing the delta representing the changes, the previous
    /// buffer, and an `InsertDrift` enum describing the correct selection update
    /// behaviour.
    pub(crate) fn commit_delta(&mut self) -> Option<(RopeDelta, Rope, InsertDrift)> {...}

    /// Attempts to find the delta from head for the given `RevToken`. Returns
    /// `None` if the revision is not found, so this result should be checked if
    /// the revision is coming from a plugin.
    pub(crate) fn delta_rev_head(&self, target_rev_id: RevToken) -> Option<RopeDelta> {...}

    #[cfg(not(target_os = "fuchsia"))]
    fn gc_undos(&mut self) {...}

    #[cfg(target_os = "fuchsia")]
    fn gc_undos(&mut self) {...}

    pub fn merge_new_state(&mut self, new_engine: Engine) {...}

    /// See `Engine::set_session_id`. Only useful for Fuchsia sync.
    pub fn set_session_id(&mut self, session: (u64, u32)) {...}

    #[cfg(feature = "ledger")]
    pub fn set_sync_store(&mut self, sync_store: SyncStore) {...}

    #[cfg(not(feature = "ledger"))]
    pub fn sync_state_changed(&mut self) {...}

    #[cfg(feature = "ledger")]
    pub fn sync_state_changed(&mut self) {...}

    #[cfg(feature = "ledger")]
    pub fn transaction_ready(&mut self) {...}

    fn do_insert(&mut self, view: &View, config: &BufferItems, chars: &str) {...}

    fn do_paste(&mut self, view: &View, chars: &str) {...}

    pub(crate) fn do_cut(&mut self, view: &mut View) -> Value {...}

    pub(crate) fn do_copy(&self, view: &View) -> Value {...}

    fn do_undo(&mut self) {...}

    fn do_redo(&mut self) {...}

    fn update_undos(&mut self) {...}

    fn do_replace(&mut self, view: &mut View, replace_all: bool) {...}

    fn do_delete_by_movement(
        &mut self,
        view: &View,
        movement: Movement,
        save: bool,
        kill_ring: &mut Rope,
    ) {...}

    fn do_delete_backward(&mut self, view: &View, config: &BufferItems) {...}

    fn do_transpose(&mut self, view: &View) {...}

    fn do_transform_text<F: Fn(&str) -> String>(&mut self, view: &View, transform_function: F) {...}

    fn do_capitalize_text(&mut self, view: &mut View) {...}

    fn do_modify_indent(&mut self, view: &View, config: &BufferItems, direction: IndentDirection) {...}

    fn do_insert_newline(&mut self, view: &View, config: &BufferItems) {...}

    fn do_insert_tab(&mut self, view: &View, config: &BufferItems) {...}

    fn do_yank(&mut self, view: &View, kill_ring: &Rope) {...}

    fn do_duplicate_line(&mut self, view: &View, config: &BufferItems) {...}

    fn do_change_number<F: Fn(i128) -> Option<i128>>(
        &mut self,
        view: &View,
        transform_function: F,
    ) {...}

    pub(crate) fn do_edit(
        &mut self,
        view: &mut View,
        kill_ring: &mut Rope,
        config: &BufferItems,
        cmd: BufferEvent,
    ) {...}

    pub fn theme_changed(&mut self, style_map: &ThemeStyleMap) {...}

    pub fn plugin_n_lines(&self) -> usize {...}

    pub fn update_spans(
        &mut self,
        view: &mut View,
        plugin: PluginId,
        start: usize,
        len: usize,
        spans: Vec<ScopeSpan>,
        rev: RevToken,
    ) {...}

    pub fn update_annotations(
        &mut self,
        view: &mut View,
        plugin: PluginId,
        start: usize,
        len: usize,
        annotation_spans: Vec<DataSpan>,
        annotation_type: AnnotationType,
        rev: RevToken,
    ) {...}

    pub(crate) fn get_rev(&self, rev: RevToken) -> Option<Cow<'_, Rope>> {...}

    pub fn plugin_get_data(
        &self,
        start: usize,
        unit: TextUnit,
        max_size: usize,
        rev: RevToken,
    ) -> Option<GetDataResponse> {...}
}

#[derive(PartialEq, Eq, Clone, Copy, Debug, Serialize, Deserialize)]
#[serde(rename_all = "snake_case")]
pub enum EditType {
    /// A catchall for edits that don't fit elsewhere, and which should
    /// always have their own undo groups; used for things like cut/copy/paste.
    Other,
    /// An insert from the keyboard/IME (not a paste or a yank).
    #[serde(rename = "insert")]
    InsertChars,
    #[serde(rename = "newline")]
    InsertNewline,
    /// An indentation adjustment.
    Indent,
    Delete,
    Undo,
    Redo,
    Transpose,
    Surround,
}

impl EditType {
    /// Checks whether a new undo group should be created between two edits.
    fn breaks_undo_group(self, previous: EditType) -> bool {...}
}

fn last_selection_region(regions: &[SelRegion]) -> Option<&SelRegion> {...}

/// Counts the number of lines in the string, not including any trailing newline.
fn count_lines(s: &str) -> usize {...}
```

## xi-editor-ph7/rust/core-lib/src/event_context.rs

```rust
use std::cell::RefCell;
use std::iter;
use std::ops::Range;
use std::path::Path;
use std::time::{Duration, Instant};

use serde_json::{self, Value};

use crate::trace::trace_block;
use xi_rope::{Cursor, Interval, LinesMetric, Rope, RopeDelta};
use xi_rpc::{Error as RpcError, RemoteError};

use crate::plugins::rpc::{
    ClientPluginInfo, Hover, PluginBufferInfo, PluginNotification, PluginRequest, PluginUpdate,
};
use crate::rpc::{EditNotification, EditRequest, LineRange, Position as ClientPosition};

use crate::client::Client;
use crate::config::{BufferItems, Table};
use crate::edit_types::{EventDomain, SpecialEvent};
use crate::editor::Editor;
use crate::file::FileInfo;
use crate::line_offset::LineOffset;
use crate::plugins::Plugin;
use crate::recorder::Recorder;
use crate::selection::InsertDrift;
use crate::styles::ThemeStyleMap;
use crate::syntax::LanguageId;
use crate::tabs::{
    BufferId, PluginId, ViewId, FIND_VIEW_IDLE_MASK, RENDER_VIEW_IDLE_MASK, REWRAP_VIEW_IDLE_MASK,
};
use crate::view::View;
use crate::width_cache::WidthCache;
use crate::WeakXiCore;

// Maximum returned result from plugin get_data RPC.
pub const MAX_SIZE_LIMIT: usize = 1024 * 1024;

//TODO: tune this. a few ms can make a big difference. We may in the future
//want to make this tuneable at runtime, or to be configured by the client.
/// The render delay after an edit occurs; plugin updates received in this
/// window will be sent to the view along with the edit.
const RENDER_DELAY: Duration = Duration::from_millis(2);

/// A collection of all the state relevant for handling a particular event.
///
/// This is created dynamically for each event that arrives to the core,
/// such as a user-initiated edit or style updates from a plugin.
pub struct EventContext<'a> {
    pub(crate) view_id: ViewId,
    pub(crate) buffer_id: BufferId,
    pub(crate) editor: &'a RefCell<Editor>,
    pub(crate) info: Option<&'a FileInfo>,
    pub(crate) config: &'a BufferItems,
    pub(crate) recorder: &'a RefCell<Recorder>,
    pub(crate) language: LanguageId,
    pub(crate) view: &'a RefCell<View>,
    pub(crate) siblings: Vec<&'a RefCell<View>>,
    pub(crate) plugins: Vec<&'a Plugin>,
    pub(crate) client: &'a Client,
    pub(crate) style_map: &'a RefCell<ThemeStyleMap>,
    pub(crate) width_cache: &'a RefCell<WidthCache>,
    pub(crate) kill_ring: &'a RefCell<Rope>,
    pub(crate) weak_core: &'a WeakXiCore,
}

impl<'a> EventContext<'a> {
    /// Executes a closure with mutable references to the editor and the view,
    /// common in edit actions that modify the text.
    pub(crate) fn with_editor<R, F>(&mut self, f: F) -> R
    where
        F: FnOnce(&mut Editor, &mut View, &mut Rope, &BufferItems) -> R,
    {...}

    /// Executes a closure with a mutable reference to the view and a reference
    /// to the current text. This is common to most edits that just modify
    /// selection or viewport state.
    fn with_view<R, F>(&mut self, f: F) -> R
    where
        F: FnOnce(&mut View, &Rope) -> R,
    {...}

    fn with_each_plugin<F: FnMut(&&Plugin)>(&self, f: F) {...}

    pub(crate) fn do_edit(&mut self, cmd: EditNotification) {...}

    fn dispatch_event(&mut self, event: EventDomain) {...}

    fn do_special(&mut self, cmd: SpecialEvent) {...}

    pub(crate) fn do_edit_sync(&mut self, cmd: EditRequest) -> Result<Value, RemoteError> {...}

    pub(crate) fn do_plugin_cmd(&mut self, plugin: PluginId, cmd: PluginNotification) {...}

    pub(crate) fn do_plugin_cmd_sync(&mut self, _plugin: PluginId, cmd: PluginRequest) -> Value {...}

    /// Commits any changes to the buffer, updating views and plugins as needed.
    /// This only updates internal state; it does not update the client.
    fn after_edit(&mut self, author: &str) {...}

    fn update_views(&self, ed: &Editor, delta: &RopeDelta, last_text: &Rope, drift: InsertDrift) {...}

    fn update_plugins(&self, ed: &mut Editor, delta: RopeDelta, author: &str) {...}

    /// Renders the view, if a render has not already been scheduled.
    pub(crate) fn render_if_needed(&mut self) {...}

    pub(crate) fn _finish_delayed_render(&mut self) {...}

    /// Flushes any changes in the views out to the frontend.
    fn render(&mut self) {...}
}

/// Helpers related to specific commands.
///
/// Certain events and actions don't generalize well; handling these
/// requires access to particular combinations of state. We isolate such
/// special cases here.
impl<'a> EventContext<'a> {
    pub(crate) fn view_init(&mut self) {...}

    pub(crate) fn finish_init(&mut self, config: &Table) {...}

    pub(crate) fn after_save(&mut self, path: &Path) {...}

    /// Returns `true` if this was the last view
    pub(crate) fn close_view(&self) -> bool {...}

    pub(crate) fn config_changed(&mut self, changes: &Table) {...}

    pub(crate) fn language_changed(&mut self, new_language_id: &LanguageId) {...}

    pub(crate) fn reload(&mut self, text: Rope) {...}

    pub(crate) fn plugin_info(&mut self) -> PluginBufferInfo {...}

    pub(crate) fn plugin_started(&self, plugin: &Plugin) {...}

    pub(crate) fn plugin_stopped(&mut self, plugin: &Plugin) {...}

    pub(crate) fn do_plugin_update(&mut self, update: Result<Value, RpcError>) {...}

    /// Returns the text to be saved, appending a newline if necessary.
    pub(crate) fn text_for_save(&mut self) -> Rope {...}

    /// Called after anything changes that effects word wrap, such as the size of
    /// the window or the user's wrap settings. `rewrap_immediately` should be `true`
    /// except in the resize case; during live resize we want to delay recalculation
    /// to avoid unnecessary work.
    fn update_wrap_settings(&mut self, rewrap_immediately: bool) {...}

    /// Tells the view to rewrap a batch of lines, if needed. This guarantees that
    /// the currently visible region will be correctly wrapped; the caller should
    /// check if additional wrapping is necessary and schedule that if so.
    fn rewrap(&mut self) {...}

    /// Does incremental find.
    pub(crate) fn do_incremental_find(&mut self) {...}

    fn schedule_find(&self) {...}

    /// Tells the view to execute find on a batch of lines, if needed.
    fn find(&mut self) {...}

    /// Does a rewrap batch, and schedules follow-up work if needed.
    pub(crate) fn do_rewrap_batch(&mut self) {...}

    fn schedule_rewrap(&self) {...}

    fn do_request_lines(&mut self, first: usize, last: usize) {...}

    fn selected_line_ranges(&mut self) -> Vec<(usize, usize)> {...}

    fn do_reindent(&mut self) {...}

    fn do_debug_toggle_comment(&mut self) {...}

    fn do_request_hover(&mut self, request_id: usize, position: Option<ClientPosition>) {...}

    fn do_show_hover(&mut self, request_id: usize, hover: Result<Hover, RemoteError>) {...}

    /// Gives the requested position in UTF-8 offset format to be sent to plugin
    /// If position is `None`, it tries to get the current Caret Position and use
    /// that instead
    fn get_resolved_position(&mut self, position: Option<ClientPosition>) -> Option<usize> {...}
}
```

## xi-editor-ph7/rust/core-lib/src/file.rs

```rust
use std::collections::HashMap;
use std::ffi::OsString;
use std::fmt;
use std::fs::{self, File};
use std::io::{self, Read, Write};
use std::path::{Path, PathBuf};
use std::str;
use std::time::SystemTime;

use xi_rope::Rope;
use xi_rpc::RemoteError;

use crate::tabs::BufferId;

#[cfg(feature = "notify")]
use crate::tabs::OPEN_FILE_EVENT_TOKEN;
#[cfg(feature = "notify")]
use crate::watcher::FileWatcher;
#[cfg(target_family = "unix")]
use std::{fs::Permissions, os::unix::fs::PermissionsExt};

const UTF8_BOM: &str = "\u{feff}";

/// Tracks all state related to open files.
pub struct FileManager {
    open_files: HashMap<PathBuf, BufferId>,
    file_info: HashMap<BufferId, FileInfo>,
    /// A monitor of filesystem events, for things like reloading changed files.
    #[cfg(feature = "notify")]
    watcher: FileWatcher,
}

#[derive(Debug)]
pub struct FileInfo {
    pub encoding: CharacterEncoding,
    pub path: PathBuf,
    pub mod_time: Option<SystemTime>,
    pub has_changed: bool,
    #[cfg(target_family = "unix")]
    pub permissions: Option<u32>,
}

pub enum FileError {
    Io(io::Error, PathBuf),
    UnknownEncoding(PathBuf),
    HasChanged(PathBuf),
}

#[derive(Debug, Clone, Copy)]
pub enum CharacterEncoding {
    Utf8,
    Utf8WithBom,
}

impl FileManager {
    #[cfg(feature = "notify")]
    pub fn new(watcher: FileWatcher) -> Self {...}

    #[cfg(not(feature = "notify"))]
    pub fn new() -> Self {...}

    #[cfg(feature = "notify")]
    pub fn watcher(&mut self) -> &mut FileWatcher {...}

    pub fn get_info(&self, id: BufferId) -> Option<&FileInfo> {...}

    pub fn get_editor(&self, path: &Path) -> Option<BufferId> {...}

    /// Returns `true` if this file is open and has changed on disk.
    /// This state is stashed.
    pub fn check_file(&mut self, path: &Path, id: BufferId) -> bool {...}

    pub fn open(&mut self, path: &Path, id: BufferId) -> Result<Rope, FileError> {...}

    pub fn close(&mut self, id: BufferId) {...}

    pub fn save(&mut self, path: &Path, text: &Rope, id: BufferId) -> Result<(), FileError> {...}

    fn save_new(&mut self, path: &Path, text: &Rope, id: BufferId) -> Result<(), FileError> {...}

    fn save_existing(&mut self, path: &Path, text: &Rope, id: BufferId) -> Result<(), FileError> {...}
}

fn try_load_file<P>(path: P) -> Result<(Rope, FileInfo), FileError>
where
    P: AsRef<Path>,
{...}

#[allow(unused)]
fn try_save(
    path: &Path,
    text: &Rope,
    encoding: CharacterEncoding,
    file_info: Option<&FileInfo>,
) -> io::Result<()> {...}

fn try_decode(bytes: Vec<u8>, encoding: CharacterEncoding, path: &Path) -> Result<Rope, FileError> {...}

impl CharacterEncoding {
    fn guess(s: &[u8]) -> Self {...}
}

/// Returns the modification timestamp for the file at a given path,
/// if present.
fn get_mod_time<P: AsRef<Path>>(path: P) -> Option<SystemTime> {...}

/// Returns the file permissions for the file at a given path on UNIXy systems,
/// if present.
#[cfg(target_family = "unix")]
fn get_permissions<P: AsRef<Path>>(path: P) -> Option<u32> {...}

impl From<FileError> for RemoteError {
    fn from(src: FileError) -> RemoteError {...}
}

impl FileError {
    fn error_code(&self) -> i64 {...}
}

impl fmt::Display for FileError {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {...}
}
```

## xi-editor-ph7/rust/core-lib/src/find.rs

```rust
use std::cmp::{max, min};

use crate::annotations::{AnnotationRange, AnnotationSlice, AnnotationType, ToAnnotation};
use crate::line_offset::LineOffset;
use crate::selection::{InsertDrift, SelRegion, Selection};
use crate::view::View;
use crate::word_boundaries::WordCursor;
use regex::{Regex, RegexBuilder};
use xi_rope::delta::DeltaRegion;
use xi_rope::find::{find, is_multiline_regex, CaseMatching};
use xi_rope::{Cursor, Interval, LinesMetric, Metric, Rope, RopeDelta};

const REGEX_SIZE_LIMIT: usize = 1000000;

/// Information about search queries and number of matches for find
#[derive(Serialize, Deserialize, Debug)]
pub struct FindStatus {
    /// Identifier for the current search query.
    id: usize,

    /// The current search query.
    chars: Option<String>,

    /// Whether the active search is case matching.
    case_sensitive: Option<bool>,

    /// Whether the search query is considered as regular expression.
    is_regex: Option<bool>,

    /// Query only matches whole words.
    whole_words: Option<bool>,

    /// Total number of matches.
    matches: usize,

    /// Line numbers which have find results.
    lines: Vec<usize>,
}

/// Contains logic to search text
pub struct Find {
    /// Uniquely identifies this search query.
    id: usize,

    /// The occurrences, which determine the highlights, have been updated.
    hls_dirty: bool,

    /// The currently active search string.
    search_string: Option<String>,

    /// The case matching setting for the currently active search.
    case_matching: CaseMatching,

    /// The search query should be considered as regular expression.
    regex: Option<Regex>,

    /// Query matches only whole words.
    whole_words: bool,

    /// The set of all known find occurrences (highlights).
    occurrences: Selection,
}

impl Find {
    pub fn new(id: usize) -> Find {...}

    pub fn id(&self) -> usize {...}

    pub fn occurrences(&self) -> &Selection {...}

    pub fn hls_dirty(&self) -> bool {...}

    pub fn find_status(&self, view: &View, text: &Rope, matches_only: bool) -> FindStatus {...}

    pub fn set_hls_dirty(&mut self, is_dirty: bool) {...}

    pub fn update_highlights(&mut self, text: &Rope, delta: &RopeDelta) {...}

    /// Returns `true` if the search query is a multi-line regex.
    pub(crate) fn is_multiline_regex(&self) -> bool {...}

    /// Unsets the search and removes all highlights from the view.
    pub fn unset(&mut self) {...}

    /// Sets find parameters and search query. Returns `true` if parameters have been updated.
    /// Returns `false` to indicate that parameters haven't change.
    pub(crate) fn set_find(
        &mut self,
        search_string: &str,
        case_sensitive: bool,
        is_regex: bool,
        whole_words: bool,
    ) -> bool {...}

    /// Execute the search on the provided text in the range provided by `start` and `end`.
    pub fn update_find(&mut self, text: &Rope, start: usize, end: usize, include_slop: bool) {...}

    /// Return the occurrence closest to the provided selection `sel`. If searched is reversed then
    /// the occurrence closest to the start of the selection is returned. `wrapped` indicates that
    /// if the end of the text is reached the search continues from the start.
    pub fn next_occurrence(
        &self,
        text: &Rope,
        reverse: bool,
        wrapped: bool,
        sel: &Selection,
    ) -> Option<SelRegion> {...}

    /// Checks if the start and end of a match is matching whole words.
    fn is_matching_whole_words(&self, text: &Rope, start: usize, end: usize) -> bool {...}
}

/// Implementing the `ToAnnotation` trait allows to convert finds to annotations.
impl ToAnnotation for Find {
    fn get_annotations(&self, interval: Interval, view: &View, text: &Rope) -> AnnotationSlice {...}
}
```

## xi-editor-ph7/rust/core-lib/src/fuchsia/ledger.rs

```rust
use apps_ledger_services_public::*;
use fidl::Error;
use fuchsia::read_entire_vmo;
use magenta::{self, Vmo};
use sha2::{Digest, Sha256};

// Rust emits a warning if matched-on constants aren't all-caps
pub const OK: Status = Status_Ok;
pub const KEY_NOT_FOUND: Status = Status_KeyNotFound;
pub const NEEDS_FETCH: Status = Status_NeedsFetch;
pub const RESULT_COMPLETED: ResultState = ResultState_Completed;

pub fn ledger_crash_callback(res: Result<Status, Error>) {...}

#[derive(Debug)]
pub enum ValueError {
    NeedsFetch,
    LedgerFail(Status),
    Vmo(magenta::Status),
}

/// Convert the low level result of getting a key from the ledger into a
/// higher level Rust representation.
pub fn value_result(res: (Status, Option<Vmo>)) -> Result<Option<Vec<u8>>, ValueError> {...}

/// Ledger page ids are exactly 16 bytes, so we need a way of determining
/// a unique 16 byte ID that won't collide based on some data we have
pub fn gen_page_id(input_data: &[u8]) -> [u8; 16] {...}
```

## xi-editor-ph7/rust/core-lib/src/fuchsia/mod.rs

```rust
pub mod ledger;
pub mod sync;

use magenta::{Status, Vmo};

// TODO: move this into magenta-rs?
pub fn read_entire_vmo(vmo: &Vmo) -> Result<Vec<u8>, Status> {...}
```

## xi-editor-ph7/rust/core-lib/src/fuchsia/sync.rs

```rust
use std::io::Write;
use std::sync::mpsc::{Receiver, RecvError, Sender};

use log;

use apps_ledger_services_public::*;
use fidl::{self, Future, Promise};
use fuchsia::read_entire_vmo;
use magenta::{Channel, ChannelOpts, HandleBase};
use serde_json;

use super::ledger::{self, ledger_crash_callback};
use tabs::{BufferContainerRef, BufferIdentifier};
use xi_rope::engine::Engine;

// TODO switch these to bincode
fn state_to_buf(state: &Engine) -> Vec<u8> {...}

fn buf_to_state(buf: &[u8]) -> Result<Engine, serde_json::Error> {...}

/// Stores state needed by the container to perform synchronization.
pub struct SyncStore {
    page: Page_Proxy,
    key: Vec<u8>,
    updates: Sender<SyncMsg>,
    transaction_pending: bool,
    buffer: BufferIdentifier,
}

impl SyncStore {
    /// - `page` is a reference to the Ledger page to store data under.
    /// - `key` is the key the `Syncable` managed by this `SyncStore` will be stored under.
    ///    This example only supports storing things under a single key per page.
    /// - `updates` is a channel to a `SyncUpdater` that will handle events.
    ///
    /// Returns a sync store and schedules the loading of initial
    /// state and subscribes to state updates for this document.
    pub fn new(
        mut page: Page_Proxy,
        key: Vec<u8>,
        updates: Sender<SyncMsg>,
        buffer: BufferIdentifier,
    ) -> SyncStore {...}

    /// Called whenever this app changed its own state and would like to
    /// persist the changes to the ledger. Changes can't be committed
    /// immediately since we have to wait for PageWatcher changes that may not
    /// have arrived yet.
    pub fn state_changed(&mut self) {...}

    /// Should be called in SyncContainer::transaction_ready to persist the current state.
    pub fn commit_transaction(&mut self, state: &Engine) {...}
}

/// All the different asynchronous events the updater thread needs to listen for and act on
pub enum SyncMsg {
    NewState {
        buffer: BufferIdentifier,
        new_buf: Vec<u8>,
        done: Option<Promise<Option<PageSnapshot_Server>, fidl::Error>>,
    },
    TransactionReady {
        buffer: BufferIdentifier,
    },
    /// Shut down the updater thread
    Stop,
}

/// We want to be able to register to receive events from inside the
/// `SyncStore`/`SyncContainer` but from there we don't have access to the
/// Mutex that holds the container, so we give channel Senders to all the
/// futures so that they can all trigger events in one place that does have
/// the right reference.
///
/// Additionally, the individual `Editor`s aren't wrapped in a `Mutex` so we
/// have to hold a `BufferContainerRef` and use `BufferIdentifier`s with one
/// `SyncUpdater` for all buffers.
pub struct SyncUpdater<W: Write> {
    container_ref: BufferContainerRef<W>,
    chan: Receiver<SyncMsg>,
}

impl<W: Write + Send + 'static> SyncUpdater<W> {
    pub fn new(container_ref: BufferContainerRef<W>, chan: Receiver<SyncMsg>) -> SyncUpdater<W> {...}

    /// Run this in a thread, it will return when it encounters an error
    /// reading the channel or when the `Stop` message is recieved.
    pub fn work(&self) -> Result<(), RecvError> {...}
}

struct PageWatcherServer {
    updates: Sender<SyncMsg>,
    buffer: BufferIdentifier,
}

impl PageWatcher for PageWatcherServer {
    fn on_change(
        &mut self,
        page_change: PageChange,
        result_state: ResultState,
    ) -> Future<Option<PageSnapshot_Server>, fidl::Error> {...}
}

impl PageWatcher_Stub for PageWatcherServer {
    // Use default dispatching, but we could override it here.
}
impl_fidl_stub!(PageWatcherServer: PageWatcher_Stub);

// ============= Conflict resolution

pub fn start_conflict_resolver_factory(ledger: &mut Ledger_Proxy, key: Vec<u8>) {...}

struct ConflictResolverFactoryServer {
    key: Vec<u8>,
}

impl ConflictResolverFactory for ConflictResolverFactoryServer {
    fn get_policy(&mut self, _page_id: Vec<u8>) -> Future<MergePolicy, ::fidl::Error> {...}

    /// Our resolvers are the same for every page
    fn new_conflict_resolver(&mut self, _page_id: Vec<u8>, resolver: ConflictResolver_Server) {...}
}

impl ConflictResolverFactory_Stub for ConflictResolverFactoryServer {
    // Use default dispatching, but we could override it here.
}
impl_fidl_stub!(ConflictResolverFactoryServer: ConflictResolverFactory_Stub);

fn state_from_snapshot<F>(
    snapshot: ::fidl::InterfacePtr<PageSnapshot_Client>,
    key: Vec<u8>,
    done: F,
) where
    F: Send + FnOnce(Result<Option<Engine>, ()>) + 'static,
{...}

struct ConflictResolverServer {
    key: Vec<u8>,
}

impl ConflictResolver for ConflictResolverServer {
    fn resolve(
        &mut self,
        left: ::fidl::InterfacePtr<PageSnapshot_Client>,
        right: ::fidl::InterfacePtr<PageSnapshot_Client>,
        _common_version: Option<::fidl::InterfacePtr<PageSnapshot_Client>>,
        result_provider: ::fidl::InterfacePtr<MergeResultProvider_Client>,
    ) {...}
}

impl ConflictResolver_Stub for ConflictResolverServer {
    // Use default dispatching, but we could override it here.
}
impl_fidl_stub!(ConflictResolverServer: ConflictResolver_Stub);
```

## xi-editor-ph7/rust/core-lib/src/index_set.rs

```rust
use std::cmp::{max, min, Ordering};
use xi_rope::{RopeDelta, Transformer};

pub struct IndexSet {
    ranges: Vec<(usize, usize)>,
}

pub fn remove_n_at<T: Clone>(v: &mut Vec<T>, index: usize, n: usize) {...}

impl IndexSet {
    /// Create a new, empty set.
    pub fn new() -> IndexSet {...}

    /// Clear the set.
    pub fn clear(&mut self) {...}

    /// Add the range start..end to the set.
    pub fn union_one_range(&mut self, start: usize, end: usize) {...}

    /// Deletes the given range from the set.
    pub fn delete_range(&mut self, start: usize, end: usize) {...}

    /// Return an iterator that yields start..end minus the coverage in this set.
    pub fn minus_one_range(&self, start: usize, end: usize) -> MinusIter<'_> {...}

    /// Computes a new set based on applying a delta to the old set. Collapsed regions are removed
    /// and contiguous regions are combined.
    pub fn apply_delta(&self, delta: &RopeDelta) -> IndexSet {...}

    }

/// The iterator generated by `minus_one_range`.
pub struct MinusIter<'a> {
    ranges: &'a [(usize, usize)],
    start: usize,
    end: usize,
}

impl<'a> Iterator for MinusIter<'a> {
    type Item = (usize, usize);

    fn next(&mut self) -> Option<(usize, usize)> {...}
}

impl<'a> DoubleEndedIterator for MinusIter<'a> {
    fn next_back(&mut self) -> Option<Self::Item> {...}
}
```

## xi-editor-ph7/rust/core-lib/src/layers.rs

```rust
use std::collections::{BTreeMap, HashMap, HashSet};
use syntect::highlighting::StyleModifier;
use syntect::parsing::Scope;

use crate::trace::trace_block;
use xi_rope::spans::{Spans, SpansBuilder};
use xi_rope::{Interval, RopeDelta};

use crate::plugins::PluginPid;
use crate::styles::{Style, ThemeStyleMap};

/// A collection of layers containing scope information.
#[derive(Default)]
pub struct Layers {
    layers: BTreeMap<PluginPid, ScopeLayer>,
    deleted: HashSet<PluginPid>,
    merged: Spans<Style>,
}

/// A collection of scope spans from a single source.
#[derive(Default)]
pub struct ScopeLayer {
    stack_lookup: Vec<Vec<Scope>>,
    style_lookup: Vec<Style>,
    // TODO: this might be efficient (in memory at least) if we use
    // a prefix tree.
    /// style state of existing scope spans, so we can more efficiently
    /// compute styles of child spans.
    style_cache: HashMap<Vec<Scope>, StyleModifier>,
    /// Human readable scope names, for debugging
    scope_spans: Spans<u32>,
    style_spans: Spans<Style>,
}

impl Layers {
    pub fn get_merged(&self) -> &Spans<Style> {...}

    /// Adds the provided scopes to the layer's lookup table.
    pub fn add_scopes(
        &mut self,
        layer: PluginPid,
        scopes: Vec<Vec<String>>,
        style_map: &ThemeStyleMap,
    ) {...}

    /// Applies the delta to all layers, inserting empty intervals
    /// for any regions inserted in the delta.
    ///
    /// This is useful for clearing spans, and for updating spans
    /// as edits occur.
    pub fn update_all(&mut self, delta: &RopeDelta) {...}

    /// Updates the scope spans for a given layer.
    pub fn update_layer(&mut self, layer: PluginPid, iv: Interval, spans: Spans<u32>) {...}

    /// Removes a given layer. This will remove all styles derived from
    /// that layer's scopes.
    pub fn remove_layer(&mut self, layer: PluginPid) -> Option<ScopeLayer> {...}

    pub fn theme_changed(&mut self, style_map: &ThemeStyleMap) {...}

    /// Resolves styles from all layers for the given interval, updating
    /// the master style spans.
    fn resolve_styles(&mut self, iv: Interval) {...}

    /// Prints scopes and style information for the given `Interval`.
    pub fn debug_print_spans(&self, iv: Interval) {...}

    /// Returns an `Err` if this layer has been deleted; the caller should return.
    fn create_if_missing(&mut self, layer_id: PluginPid) -> Result<(), ()> {...}
}

impl ScopeLayer {
    pub fn new(len: usize) -> Self {...}

    fn theme_changed(&mut self, style_map: &ThemeStyleMap) {...}

    fn add_scopes(&mut self, scopes: Vec<Vec<String>>, style_map: &ThemeStyleMap) {...}

    fn styles_for_stacks(
        &mut self,
        stacks: &[Vec<Scope>],
        style_map: &ThemeStyleMap,
    ) -> Vec<Style> {...}

    fn update_scopes(&mut self, iv: Interval, spans: &Spans<u32>) {...}

    /// Applies `delta`, which is presumed to contain empty spans.
    /// This is only used when we receive an edit, to adjust current span
    /// positions.
    fn blank_scopes(&mut self, delta: &RopeDelta) {...}

    /// Updates `self.style_spans`, mapping scopes to styles and combining
    /// adjacent and equal spans.
    fn update_styles(&mut self, iv: Interval, spans: &Spans<u32>) {...}
}
```

## xi-editor-ph7/rust/core-lib/src/lib.rs

```rust
#![allow(
    clippy::boxed_local,
    clippy::cast_lossless,
    clippy::collapsible_if,
    clippy::let_and_return,
    clippy::map_entry,
    clippy::match_as_ref,
    clippy::match_bool,
    clippy::needless_pass_by_value,
    clippy::new_without_default,
    clippy::or_fun_call,
    clippy::ptr_arg,
    clippy::too_many_arguments,
    clippy::unreadable_literal,
    clippy::get_unwrap
)]

#[macro_use]
extern crate log;
extern crate regex;
extern crate serde;
#[macro_use]
extern crate serde_json;
#[macro_use]
extern crate serde_derive;
extern crate memchr;
#[cfg(feature = "notify")]
extern crate notify;
extern crate syntect;
extern crate time;
extern crate toml;

extern crate xi_rope;
extern crate xi_rpc;
#[cfg(feature = "trace")]
extern crate xi_trace;
extern crate xi_unicode;

#[cfg(feature = "ledger")]
mod ledger_includes {
    extern crate fuchsia_zircon;
    extern crate fuchsia_zircon_sys;
    extern crate mxruntime;
    #[macro_use]
    extern crate fidl;
    extern crate apps_ledger_services_public;
    extern crate sha2;
}
#[cfg(feature = "ledger")]
use ledger_includes::*;

pub mod annotations;
pub mod backspace;
pub mod client;
pub mod config;
pub mod core;
pub mod edit_ops;
pub mod edit_types;
pub mod editor;
pub mod event_context;
pub mod file;
pub mod find;
#[cfg(feature = "ledger")]
pub mod fuchsia;
pub mod index_set;
pub mod layers;
pub mod line_cache_shadow;
pub mod line_ending;
pub mod line_offset;
pub mod linewrap;
pub mod movement;
pub mod plugins;
pub mod recorder;
pub mod selection;
pub mod styles;
pub mod syntax;
pub mod tabs;
pub mod trace;
pub mod view;
#[cfg(feature = "notify")]
pub mod watcher;
pub mod whitespace;
pub mod width_cache;
pub mod word_boundaries;

pub mod rpc;

#[cfg(feature = "ledger")]
use apps_ledger_services_public::Ledger_Proxy;

pub use crate::config::{BufferItems as BufferConfig, Table as ConfigTable};
pub use crate::core::{WeakXiCore, XiCore};
pub use crate::editor::EditType;
pub use crate::plugins::manifest as plugin_manifest;
pub use crate::plugins::rpc as plugin_rpc;
pub use crate::plugins::PluginPid;
pub use crate::syntax::{LanguageDefinition, LanguageId};
pub use crate::tabs::test_helpers;
pub use crate::tabs::{BufferId, BufferIdentifier, ViewId};
```

## xi-editor-ph7/rust/core-lib/src/line_cache_shadow.rs

```rust
use std::cmp::{max, min};
use std::fmt;

const SCROLL_SLOP: usize = 2;
const PRESERVE_EXTENT: usize = 1000;

/// The line cache shadow tracks the state of the line cache in the front-end.
/// Any content marked as valid here is up-to-date in the current state of the
/// view. Also, if `dirty` is false, then the entire line cache is valid.
#[derive(Debug)]
pub struct LineCacheShadow {
    spans: Vec<Span>,
    dirty: bool,
}

type Validity = u8;

pub const INVALID: Validity = 0;
pub const TEXT_VALID: Validity = 1;
pub const STYLES_VALID: Validity = 2;
pub const CURSOR_VALID: Validity = 4;
pub const ALL_VALID: Validity = 7;

pub struct Span {
    /// Number of lines in this span. Units are visual lines in the
    /// current state of the view.
    pub n: usize,
    /// Starting line number. Units are visual lines in the front end's
    /// current cache state (i.e. the last one rendered). Note: this is
    /// irrelevant if validity is 0.
    pub start_line_num: usize,
    /// Validity of lines in this span, consisting of the above constants or'ed.
    pub validity: Validity,
}

/// Builder for `LineCacheShadow` object.
pub struct Builder {
    spans: Vec<Span>,
    dirty: bool,
}

#[derive(Clone, Copy, PartialEq)]
pub enum RenderTactic {
    /// Discard all content for this span. Used to keep storage reasonable.
    Discard,
    /// Preserve existing content.
    Preserve,
    /// Render content if it is invalid.
    Render,
}

pub struct RenderPlan {
    /// Each span is a number of lines and a tactic.
    pub spans: Vec<(usize, RenderTactic)>,
}

pub struct PlanIterator<'a> {
    lc_shadow: &'a LineCacheShadow,
    plan: &'a RenderPlan,
    shadow_ix: usize,
    shadow_line_num: usize,
    plan_ix: usize,
    plan_line_num: usize,
}

pub struct PlanSegment {
    /// Line number of start of segment, visual lines in current view state.
    pub our_line_num: usize,
    /// Line number of start of segment, visual lines in client's cache, if validity != 0.
    pub their_line_num: usize,
    /// Number of visual lines in this segment.
    pub n: usize,
    /// Validity of this segment in client's cache.
    pub validity: Validity,
    /// Tactic for rendering this segment.
    pub tactic: RenderTactic,
}

impl Builder {
    pub fn new() -> Builder {...}

    pub fn build(self) -> LineCacheShadow {...}

    pub fn add_span(&mut self, n: usize, start_line_num: usize, validity: Validity) {...}

    pub fn set_dirty(&mut self, dirty: bool) {...}
}

impl LineCacheShadow {
    pub fn edit(&mut self, start: usize, end: usize, replace: usize) {...}

    pub fn partial_invalidate(&mut self, start: usize, end: usize, invalid: Validity) {...}

    pub fn needs_render(&self, plan: &RenderPlan) -> bool {...}

    pub fn spans(&self) -> &[Span] {...}

    pub fn iter_with_plan<'a>(&'a self, plan: &'a RenderPlan) -> PlanIterator<'a> {...}
}

impl Default for LineCacheShadow {
    fn default() -> LineCacheShadow {...}
}

impl<'a> Iterator for PlanIterator<'a> {
    type Item = PlanSegment;

    fn next(&mut self) -> Option<PlanSegment> {...}
}

impl RenderPlan {
    /// This function implements the policy of what to discard, what to preserve, and
    /// what to render.
    pub fn create(total_height: usize, first_line: usize, height: usize) -> RenderPlan {...}

    /// Upgrade a range of lines to the "Render" tactic.
    pub fn request_lines(&mut self, start: usize, end: usize) {...}
}

impl fmt::Debug for Span {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {...}
}
```

## xi-editor-ph7/rust/core-lib/src/line_ending.rs

```rust
extern crate xi_rope;

use memchr::memchr2;
use xi_rope::Rope;

/// An enumeration of valid line endings
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum LineEnding {
    CrLf, // DOS style, \r\n
    Lf,   // *nix style, \n
}

/// A struct representing a mixed line ending error.
#[derive(Debug)]
pub struct MixedLineEndingError;

impl LineEnding {
    /// Breaks a rope down into chunks, and checks each chunk for line endings
    pub fn parse(rope: &Rope) -> Result<Option<Self>, MixedLineEndingError> {...}

    /// Checks a chunk for line endings, assuming \n or \r\n
    pub fn parse_chunk(chunk: &str) -> Result<Option<Self>, MixedLineEndingError> {...}
}
```

## xi-editor-ph7/rust/core-lib/src/line_offset.rs

```rust
#![allow(clippy::range_plus_one)]

use std::ops::Range;

use xi_rope::Rope;

use crate::linewrap::Lines;
use crate::selection::SelRegion;

/// A trait from which lines and columns in a document can be calculated
/// into offsets inside a rope an vice versa.
pub trait LineOffset {
    // use own breaks if present, or text if not (no line wrapping)

    /// Returns the byte offset corresponding to the given line.
    fn offset_of_line(&self, text: &Rope, line: usize) -> usize {...}

    /// Returns the visible line number containing the given offset.
    fn line_of_offset(&self, text: &Rope, offset: usize) -> usize {...}

    // How should we count "column"? Valid choices include:
    // * Unicode codepoints
    // * grapheme clusters
    // * Unicode width (so CJK counts as 2)
    // * Actual measurement in text layout
    // * Code units in some encoding
    //
    // Of course, all these are identical for ASCII. For now we use UTF-8 code units
    // for simplicity.

    fn offset_to_line_col(&self, text: &Rope, offset: usize) -> (usize, usize) {...}

    fn line_col_to_offset(&self, text: &Rope, line: usize, col: usize) -> usize {...}

    /// Get the line range of a selected region.
    fn get_line_range(&self, text: &Rope, region: &SelRegion) -> Range<usize> {...}
}

/// A struct from which the default definitions for `offset_of_line`
/// and `line_of_offset` can be accessed, and think in logical lines.
pub struct LogicalLines;

impl LineOffset for LogicalLines {}

impl LineOffset for xi_rope::breaks::Breaks {
    fn offset_of_line(&self, _text: &Rope, line: usize) -> usize {...}

    fn line_of_offset(&self, text: &Rope, offset: usize) -> usize {...}
}

impl LineOffset for Lines {
    fn offset_of_line(&self, text: &Rope, line: usize) -> usize {...}

    fn line_of_offset(&self, text: &Rope, offset: usize) -> usize {...}
}
```

## xi-editor-ph7/rust/core-lib/src/linewrap.rs

```rust
use std::cmp::Ordering;
use std::ops::Range;

use crate::trace::trace_block;
use xi_rope::breaks::{BreakBuilder, Breaks, BreaksInfo, BreaksLeaf, BreaksMetric};
use xi_rope::spans::Spans;
use xi_rope::{Cursor, Interval, LinesMetric, Rope, RopeDelta, RopeInfo};
use xi_unicode::LineBreakLeafIter;

use crate::client::Client;
use crate::styles::{Style, N_RESERVED_STYLES};
use crate::width_cache::{CodepointMono, Token, WidthCache, WidthMeasure};

/// The visual width of the buffer for the purpose of word wrapping.
#[derive(Clone, Copy, Debug, PartialEq, Default)]
pub(crate) enum WrapWidth {
    /// No wrapping in effect.
    #[default]
    None,

    /// Width in bytes (utf-8 code units).
    ///
    /// Only works well for ASCII, will probably not be maintained long-term.
    Bytes(usize),

    /// Width in px units, requiring measurement by the front-end.
    Width(f64),
}

impl WrapWidth {
    fn differs_in_kind(self, other: WrapWidth) -> bool {...}
}

/// A range to be rewrapped.
type Task = Interval;

/// Tracks state related to visual lines.
#[derive(Default)]
pub(crate) struct Lines {
    breaks: Breaks,
    wrap: WrapWidth,
    /// Aka the 'frontier'; ranges of lines that still need to be wrapped.
    work: Vec<Task>,
}

pub(crate) struct VisualLine {
    pub(crate) interval: Interval,
    /// The logical line number for this line. Only present when this is the
    /// first visual line in a logical line.
    pub(crate) line_num: Option<usize>,
}

impl VisualLine {
    fn new<I: Into<Interval>, L: Into<Option<usize>>>(iv: I, line: L) -> Self {...}
}

/// Describes what has changed after a batch of word wrapping; this is used
/// for minimal invalidation.
pub(crate) struct InvalLines {
    pub(crate) start_line: usize,
    pub(crate) inval_count: usize,
    pub(crate) new_count: usize,
}

/// Detailed information about changes to linebreaks, used to generate
/// invalidation information. The logic behind invalidation is different
/// depending on whether we're updating breaks after an edit, or continuing
/// a bulk rewrapping task. This is shared between the two cases, and contains
/// the information relevant to both of them.
struct WrapSummary {
    start_line: usize,
    /// Total number of invalidated lines; this is meaningless in the after_edit case.
    inval_count: usize,
    /// The total number of new (hard + soft) breaks in the wrapped region.
    new_count: usize,
    /// The number of new soft breaks.
    new_soft: usize,
}

impl Lines {
    pub(crate) fn set_wrap_width(&mut self, text: &Rope, wrap: WrapWidth) {...}

    fn add_task<T: Into<Interval>>(&mut self, iv: T) {...}

    pub(crate) fn is_converged(&self) -> bool {...}

    /// Returns `true` if this interval is part of an incomplete task.
    pub(crate) fn interval_needs_wrap(&self, iv: Interval) -> bool {...}

    pub(crate) fn visual_line_of_offset(&self, text: &Rope, offset: usize) -> usize {...}

    /// Returns the byte offset corresponding to the line `line`.
    pub(crate) fn offset_of_visual_line(&self, text: &Rope, line: usize) -> usize {...}

    /// Returns an iterator over [`VisualLine`]s, starting at (and including)
    /// `start_line`.
    pub(crate) fn iter_lines<'a>(
        &'a self,
        text: &'a Rope,
        start_line: usize,
    ) -> impl Iterator<Item = VisualLine> + 'a {...}

    /// Returns the next task, prioritizing the currently visible region.
    /// Does not modify the task list; this is done after the task runs.
    fn get_next_task(&self, visible_offset: usize) -> Option<Task> {...}

    fn update_tasks_after_wrap<T: Into<Interval>>(&mut self, wrapped_iv: T) {...}

    /// Adjust offsets for any tasks after an edit.
    fn patchup_tasks<T: Into<Interval>>(&mut self, iv: T, new_len: usize) {...}

    /// Do a chunk of wrap work, if any exists.
    pub(crate) fn rewrap_chunk(
        &mut self,
        text: &Rope,
        width_cache: &mut WidthCache,
        client: &Client,
        _spans: &Spans<Style>,
        visible_lines: Range<usize>,
    ) -> Option<InvalLines> {...}

    /// Updates breaks after an edit. Returns `InvalLines`, for minimal invalidation,
    /// when possible.
    pub(crate) fn after_edit(
        &mut self,
        text: &Rope,
        old_text: &Rope,
        delta: &RopeDelta,
        width_cache: &mut WidthCache,
        client: &Client,
        visible_lines: Range<usize>,
    ) -> Option<InvalLines> {...}

    fn do_wrap_task(
        &mut self,
        text: &Rope,
        width_cache: &mut WidthCache,
        client: &Client,
        visible_lines: Range<usize>,
        max_lines: Option<usize>,
    ) -> WrapSummary {...}

    pub fn logical_line_range(&self, text: &Rope, line: usize) -> (usize, usize) {...}

    }

/// A potential opportunity to insert a break. In this representation, the widths
/// have been requested (in a batch request) but are not necessarily known until
/// the request is issued.
struct PotentialBreak {
    /// The offset within the text of the end of the word.
    pos: usize,
    /// A token referencing the width of the word, to be resolved in the width cache.
    tok: Token,
    /// Whether the break is a hard break or a soft break.
    hard: bool,
}

/// State for a rewrap in progress
struct RewrapCtx<'a> {
    text: &'a Rope,
    lb_cursor: LineBreakCursor<'a>,
    lb_cursor_pos: usize,
    width_cache: &'a mut WidthCache,
    client: &'a dyn WidthMeasure,
    pot_breaks: Vec<PotentialBreak>,
    /// Index within `pot_breaks`
    pot_break_ix: usize,
    max_width: f64,
}

// This constant should be tuned so that the RPC takes about 1ms. Less than that,
// RPC overhead becomes significant. More than that, interactivity suffers.
const MAX_POT_BREAKS: usize = 10_000;

impl<'a> RewrapCtx<'a> {
    fn new(
        text: &'a Rope,
        //_style_spans: &Spans<Style>,  client: &'a T,
        client: &'a dyn WidthMeasure,
        max_width: f64,
        width_cache: &'a mut WidthCache,
        start: usize,
    ) -> RewrapCtx<'a> {...}

    fn refill_pot_breaks(&mut self) {...}

    /// Compute the next break, assuming `start` is a valid break.
    ///
    /// Invariant: `start` corresponds to the start of the word referenced by `pot_break_ix`.
    fn wrap_one_line(&mut self, start: usize) -> Option<usize> {...}
}

struct LineBreakCursor<'a> {
    inner: Cursor<'a, RopeInfo, String>,
    lb_iter: LineBreakLeafIter,
    last_byte: u8,
}

impl<'a> LineBreakCursor<'a> {
    fn new(text: &'a Rope, pos: usize) -> LineBreakCursor<'a> {...}

    // position and whether break is hard; up to caller to stop calling after EOT
    fn next(&mut self) -> (usize, bool) {...}
}

struct VisualLines<'a> {
    cursor: MergedBreaks<'a>,
    offset: usize,
    /// The current logical line number.
    logical_line: usize,
    len: usize,
    eof: bool,
}

impl<'a> Iterator for VisualLines<'a> {
    type Item = VisualLine;

    fn next(&mut self) -> Option<VisualLine> {...}
}

/// A cursor over both hard and soft breaks. Hard breaks are retrieved from
/// the rope; the soft breaks are stored independently; this interleaves them.
///
/// # Invariants:
///
/// `self.offset` is always a valid break in one of the cursors, unless
/// at 0 or EOF.
///
/// `self.offset == self.text.pos().min(self.soft.pos())`.
struct MergedBreaks<'a> {
    text: Cursor<'a, RopeInfo, String>,
    soft: Cursor<'a, BreaksInfo, BreaksLeaf>,
    offset: usize,
    /// Starting from zero, how many calls to `next` to get to `self.offset`?
    cur_line: usize,
    total_lines: usize,
    /// Total length, in base units
    len: usize,
}

impl<'a> Iterator for MergedBreaks<'a> {
    type Item = usize;

    fn next(&mut self) -> Option<usize> {...}
}

// arrived at this by just trying out a bunch of values ¯\_(ツ)_/¯
/// how far away a line can be before we switch to a binary search
const MAX_LINEAR_DIST: usize = 20;

impl<'a> MergedBreaks<'a> {
    fn new(text: &'a Rope, breaks: &'a Breaks) -> Self {...}

    /// Sets the `self.offset` to the first valid break immediately at or preceding `offset`,
    /// and restores invariants.
    fn set_offset(&mut self, offset: usize) {...}

    fn offset_of_line(&mut self, line: usize) -> usize {...}

    fn offset_of_line_linear(&mut self, line: usize) -> usize {...}

    fn offset_of_line_bsearch(&mut self, line: usize) -> usize {...}

    fn is_hard_break(&self) -> bool {...}

    fn at_eof(&self) -> bool {...}

    fn eof_without_newline(&mut self) -> bool {...}
}

fn merged_line_of_offset(text: &Rope, soft: &Breaks, offset: usize) -> usize {...}
```

## xi-editor-ph7/rust/core-lib/src/movement.rs

```rust
use std::cmp::max;

use crate::line_offset::LineOffset;
use crate::selection::{HorizPos, SelRegion, Selection};
use crate::word_boundaries::WordCursor;
use xi_rope::{Cursor, LinesMetric, Rope};

/// The specification of a movement.
#[derive(Debug, PartialEq, Clone, Copy)]
pub enum Movement {
    /// Move to the left by one grapheme cluster.
    Left,
    /// Move to the right by one grapheme cluster.
    Right,
    /// Move to the left by one word.
    LeftWord,
    /// Move to the right by one word.
    RightWord,
    /// Move to left end of visible line.
    LeftOfLine,
    /// Move to right end of visible line.
    RightOfLine,
    /// Move up one visible line.
    Up,
    /// Move down one visible line.
    Down,
    /// Move up one viewport height.
    UpPage,
    /// Move down one viewport height.
    DownPage,
    /// Move up to the next line that can preserve the cursor position.
    UpExactPosition,
    /// Move down to the next line that can preserve the cursor position.
    DownExactPosition,
    /// Move to the start of the text line.
    StartOfParagraph,
    /// Move to the end of the text line.
    EndOfParagraph,
    /// Move to the end of the text line, or next line if already at end.
    EndOfParagraphKill,
    /// Move to the start of the document.
    StartOfDocument,
    /// Move to the end of the document
    EndOfDocument,
}

/// Compute movement based on vertical motion by the given number of lines.
///
/// Note: in non-exceptional cases, this function preserves the `horiz`
/// field of the selection region.
fn vertical_motion(
    r: SelRegion,
    lo: &dyn LineOffset,
    text: &Rope,
    line_delta: isize,
    modify: bool,
) -> (usize, Option<HorizPos>) {...}

/// Compute movement based on vertical motion by the given number of lines skipping
/// any line that is shorter than the current cursor position.
fn vertical_motion_exact_pos(
    r: SelRegion,
    lo: &dyn LineOffset,
    text: &Rope,
    move_up: bool,
    modify: bool,
) -> (usize, Option<HorizPos>) {...}

/// Based on the current selection position this will return the cursor position, the current line, and the
/// total number of lines of the file.
fn selection_position(
    r: SelRegion,
    lo: &dyn LineOffset,
    text: &Rope,
    move_up: bool,
    modify: bool,
) -> (HorizPos, usize) {...}

/// When paging through a file, the number of lines from the previous page
/// that will also be visible in the next.
const SCROLL_OVERLAP: isize = 2;

/// Computes the actual desired amount of scrolling (generally slightly
/// less than the height of the viewport, to allow overlap).
fn scroll_height(height: usize) -> isize {...}

/// Compute the result of movement on one selection region.
///
/// # Arguments
///
/// * `height` - viewport height
pub fn region_movement(
    m: Movement,
    r: SelRegion,
    lo: &dyn LineOffset,
    height: usize,
    text: &Rope,
    modify: bool,
) -> SelRegion {...}

/// Compute a new selection by applying a movement to an existing selection.
///
/// In a multi-region selection, this function applies the movement to each
/// region in the selection, and returns the union of the results.
///
/// If `modify` is `true`, the selections are modified, otherwise the results
/// of individual region movements become carets.
///
/// # Arguments
///
/// * `height` - viewport height
pub fn selection_movement(
    m: Movement,
    s: &Selection,
    lo: &dyn LineOffset,
    height: usize,
    text: &Rope,
    modify: bool,
) -> Selection {...}
```

## xi-editor-ph7/rust/core-lib/src/plugins/catalog.rs

```rust
use std::collections::HashMap;
use std::error::Error as StdError;
use std::fmt;
use std::fs;
use std::io::{self, Read};
use std::path::{Path, PathBuf};
use std::sync::Arc;

use super::{PluginDescription, PluginName};
use crate::config::table_from_toml_str;
use crate::syntax::Languages;

/// A catalog of all available plugins.
#[derive(Debug, Clone, Default)]
pub struct PluginCatalog {
    items: HashMap<PluginName, Arc<PluginDescription>>,
    locations: HashMap<PathBuf, Arc<PluginDescription>>,
}

/// Errors that can occur while trying to load a plugin.
#[derive(Debug)]
pub enum PluginLoadError {
    Io(io::Error),
    /// Malformed manifest
    Parse(toml::de::Error),
}

#[allow(dead_code)]
impl<'a> PluginCatalog {
    /// Loads any plugins discovered in these paths, replacing any existing
    /// plugins.
    pub fn reload_from_paths(&mut self, paths: &[PathBuf]) {...}

    /// Loads plugins from paths and adds them to existing plugins.
    pub fn load_from_paths(&mut self, paths: &[PathBuf]) {...}

    pub fn make_languages_map(&self) -> Languages {...}

    /// Returns an iterator over all plugins in the catalog, in arbitrary order.
    pub fn iter(&'a self) -> impl Iterator<Item = Arc<PluginDescription>> + 'a {...}

    /// Returns an iterator over all plugin names in the catalog,
    /// in arbitrary order.
    pub fn iter_names(&'a self) -> impl Iterator<Item = &'a PluginName> {...}

    /// Returns the plugin located at the provided file path.
    pub fn get_from_path(&self, path: &PathBuf) -> Option<Arc<PluginDescription>> {...}

    /// Returns a reference to the named plugin if it exists in the catalog.
    pub fn get_named(&self, plugin_name: &str) -> Option<Arc<PluginDescription>> {...}

    /// Removes the named plugin.
    pub fn remove_named(&mut self, plugin_name: &str) {...}
}

fn find_all_manifests(paths: &[PathBuf]) -> Vec<PathBuf> {...}

fn load_manifest(path: &Path) -> Result<PluginDescription, PluginLoadError> {...}

impl From<io::Error> for PluginLoadError {
    fn from(err: io::Error) -> PluginLoadError {...}
}

impl From<toml::de::Error> for PluginLoadError {
    fn from(err: toml::de::Error) -> PluginLoadError {...}
}

impl fmt::Display for PluginLoadError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {...}
}

impl StdError for PluginLoadError {
    fn source(&self) -> Option<&(dyn StdError + 'static)> {...}
}
```

## xi-editor-ph7/rust/core-lib/src/plugins/manifest.rs

```rust
use std::path::PathBuf;

use serde::{Deserialize, Deserializer, Serialize};
use serde_json::{self, Value};

use crate::syntax::{LanguageDefinition, LanguageId};

/// Describes attributes and capabilities of a plugin.
///
/// Note: - these will eventually be loaded from manifest files.
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "snake_case")]
pub struct PluginDescription {
    pub name: String,
    pub version: String,
    #[serde(default)]
    pub scope: PluginScope,
    // more metadata ...
    /// path to plugin executable
    #[serde(deserialize_with = "platform_exec_path")]
    pub exec_path: PathBuf,
    /// Events that cause this plugin to run
    #[serde(default)]
    pub activations: Vec<PluginActivation>,
    #[serde(default)]
    pub commands: Vec<Command>,
    #[serde(default)]
    pub languages: Vec<LanguageDefinition>,
}

fn platform_exec_path<'de, D: Deserializer<'de>>(deserializer: D) -> Result<PathBuf, D::Error> {...}

/// `PluginActivation`s represent events that trigger running a plugin.
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "snake_case")]
pub enum PluginActivation {
    /// Always run this plugin, when available.
    Autorun,
    /// Run this plugin if the provided SyntaxDefinition is active.
    #[allow(dead_code)]
    OnSyntax(LanguageId),
    /// Run this plugin in response to a given command.
    #[allow(dead_code)]
    OnCommand,
}

/// Describes the scope of events a plugin receives.
#[derive(Debug, Clone, Deserialize, Serialize)]
#[serde(rename_all = "snake_case")]
#[derive(Default)]
pub enum PluginScope {
    /// The plugin receives events from multiple buffers.
    Global,
    /// The plugin receives events for a single buffer.
    #[default]
    BufferLocal,
    /// The plugin is launched in response to a command, and receives no
    /// further updates.
    SingleInvocation,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
/// Represents a custom command provided by a plugin.
pub struct Command {
    /// Human readable title, for display in (for example) a menu.
    pub title: String,
    /// A short description of the command.
    pub description: String,
    /// Template of the command RPC as it should be sent to the plugin.
    pub rpc_cmd: PlaceholderRpc,
    /// A list of `CommandArgument`s, which the client should use to build the RPC.
    pub args: Vec<CommandArgument>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
/// A user provided argument to a plugin command.
pub struct CommandArgument {
    /// A human readable name for this argument, for use as placeholder
    /// text or equivelant.
    pub title: String,
    /// A short (single sentence) description of this argument's use.
    pub description: String,
    pub key: String,
    pub arg_type: ArgumentType,
    #[serde(skip_serializing_if = "Option::is_none")]
    /// If `arg_type` is `Choice`, `options` must contain a list of options.
    pub options: Option<Vec<ArgumentOption>>,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub enum ArgumentType {
    Number,
    Int,
    PosInt,
    Bool,
    String,
    Choice,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
/// Represents an option for a user-selectable argument.
pub struct ArgumentOption {
    pub title: String,
    pub value: Value,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
#[serde(rename_all = "snake_case")]
/// A placeholder type which can represent a generic RPC.
///
/// This is the type used for custom plugin commands, which may have arbitrary
/// method names and parameters.
pub struct PlaceholderRpc {
    pub method: String,
    pub params: Value,
    pub rpc_type: RpcType,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
#[serde(rename_all = "snake_case")]
pub enum RpcType {
    Notification,
    Request,
}

impl Command {
    pub fn new<S, V>(title: S, description: S, rpc_cmd: PlaceholderRpc, args: V) -> Self
    where
        S: AsRef<str>,
        V: Into<Option<Vec<CommandArgument>>>,
    {...}
}

impl CommandArgument {
    pub fn new<S: AsRef<str>>(
        title: S,
        description: S,
        key: S,
        arg_type: ArgumentType,
        options: Option<Vec<ArgumentOption>>,
    ) -> Self {...}
}

impl ArgumentOption {
    pub fn new<S: AsRef<str>, V: Serialize>(title: S, value: V) -> Self {...}
}

impl PlaceholderRpc {
    pub fn new<S, V>(method: S, params: V, request: bool) -> Self
    where
        S: AsRef<str>,
        V: Into<Option<Value>>,
    {...}

    pub fn is_request(&self) -> bool {...}

    /// Returns a reference to the placeholder's params.
    pub fn params_ref(&self) -> &Value {...}

    /// Returns a mutable reference to the placeholder's params.
    pub fn params_ref_mut(&mut self) -> &mut Value {...}

    /// Returns a reference to the placeholder's method.
    pub fn method_ref(&self) -> &str {...}
}

impl PluginDescription {
    /// Returns `true` if this plugin is globally scoped, else `false`.
    pub fn is_global(&self) -> bool {...}
}
```

## xi-editor-ph7/rust/core-lib/src/plugins/mod.rs

```rust
mod catalog;
pub mod manifest;
pub mod rpc;

use std::fmt;
use std::io::BufReader;
use std::path::Path;
use std::process::{Child, Command as ProcCommand, Stdio};
use std::sync::Arc;
use std::thread;

use serde_json::Value;

use xi_rpc::{self, RpcLoop, RpcPeer};

use crate::config::Table;
use crate::syntax::LanguageId;
use crate::tabs::ViewId;
use crate::WeakXiCore;

use self::rpc::{PluginBufferInfo, PluginUpdate};

pub(crate) use self::catalog::PluginCatalog;
pub use self::manifest::{Command, PlaceholderRpc, PluginDescription};

pub type PluginName = String;

/// A process-unique identifier for a running plugin.
///
/// Note: two instances of the same executable will have different identifiers.
/// Note: this identifier is distinct from the OS's process id.
#[derive(
    Serialize, Deserialize, Default, Debug, Clone, Copy, Hash, PartialEq, Eq, PartialOrd, Ord,
)]
pub struct PluginPid(pub(crate) usize);

pub type PluginId = PluginPid;

impl fmt::Display for PluginPid {
    fn fmt(&self, f: &mut fmt::Formatter) -> Result<(), fmt::Error> {...}
}

pub struct Plugin {
    peer: RpcPeer,
    pub(crate) id: PluginId,
    pub(crate) name: String,
    #[allow(dead_code)]
    process: Child,
}

impl Plugin {
    //TODO: initialize should be sent automatically during launch,
    //and should only send the plugin_id. We can just use the existing 'new_buffer'
    // RPC for adding views
    pub fn initialize(&self, info: Vec<PluginBufferInfo>) {...}

    pub fn shutdown(&self) {...}

    // TODO: rethink naming, does this need to be a vec?
    pub fn new_buffer(&self, info: &PluginBufferInfo) {...}

    pub fn close_view(&self, view_id: ViewId) {...}

    pub fn did_save(&self, view_id: ViewId, path: &Path) {...}

    pub fn update<F>(&self, update: &PluginUpdate, callback: F)
    where
        F: FnOnce(Result<Value, xi_rpc::Error>) + Send + 'static,
    {...}

    pub fn toggle_tracing(&self, enabled: bool) {...}

    pub fn collect_trace(&self) -> Result<Value, xi_rpc::Error> {...}

    pub fn config_changed(&self, view_id: ViewId, changes: &Table) {...}

    pub fn language_changed(&self, view_id: ViewId, new_lang: &LanguageId) {...}

    pub fn get_hover(&self, view_id: ViewId, request_id: usize, position: usize) {...}

    pub fn dispatch_command(&self, view_id: ViewId, method: &str, params: &Value) {...}
}

pub(crate) fn start_plugin_process(
    plugin_desc: Arc<PluginDescription>,
    id: PluginId,
    core: WeakXiCore,
) {...}
```

## xi-editor-ph7/rust/core-lib/src/plugins/rpc.rs

```rust
use std::borrow::Borrow;
use std::path::PathBuf;

use serde::de::{self, Deserialize, Deserializer};
use serde::ser::{self, Serialize, Serializer};
use serde_json::{self, Value};

use super::PluginPid;
use crate::annotations::AnnotationType;
use crate::config::Table;
use crate::syntax::LanguageId;
use crate::tabs::{BufferIdentifier, ViewId};
use xi_rope::{LinesMetric, Rope, RopeDelta};
use xi_rpc::RemoteError;

//TODO: At the moment (May 08, 2017) this is all very much in flux.
// At some point, it will be stabalized and then perhaps will live in another crate,
// shared with the plugin lib.

// ====================================================================
// core -> plugin RPC method types + responses
// ====================================================================

/// Buffer information sent on plugin init.
#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct PluginBufferInfo {
    /// The buffer's unique identifier.
    pub buffer_id: BufferIdentifier,
    /// The buffer's current views.
    pub views: Vec<ViewId>,
    pub rev: u64,
    pub buf_size: usize,
    pub nb_lines: usize,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub path: Option<String>,
    pub syntax: LanguageId,
    pub config: Table,
}

//TODO: very likely this should be merged with PluginDescription
//TODO: also this does not belong here.
/// Describes an available plugin to the client.
#[derive(Serialize, Deserialize, Debug)]
pub struct ClientPluginInfo {
    pub name: String,
    pub running: bool,
}

/// A simple update, sent to a plugin.
#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct PluginUpdate {
    pub view_id: ViewId,
    /// The delta representing changes to the document.
    ///
    /// Note: Is `Some` in the general case; only if the delta involves
    /// inserting more than some maximum number of bytes, will this be `None`,
    /// indicating the plugin should flush cache and fetch manually.
    pub delta: Option<RopeDelta>,
    /// The size of the document after applying this delta.
    pub new_len: usize,
    /// The total number of lines in the document after applying this delta.
    pub new_line_count: usize,
    pub rev: u64,
    /// The undo_group associated with this update. The plugin may pass
    /// this value back to core when making an edit, to associate the
    /// plugin's edit with this undo group. Core uses undo_group
    //  to undo actions occurred due to plugins after a user action
    // in a single step.
    pub undo_group: Option<usize>,
    pub edit_type: String,
    pub author: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct EmptyStruct {}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "snake_case")]
#[serde(tag = "method", content = "params")]
/// RPC requests sent from the host
pub enum HostRequest {
    Update(PluginUpdate),
    CollectTrace(EmptyStruct),
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "snake_case")]
#[serde(tag = "method", content = "params")]
/// RPC Notifications sent from the host
pub enum HostNotification {
    Ping(EmptyStruct),
    Initialize { plugin_id: PluginPid, buffer_info: Vec<PluginBufferInfo> },
    DidSave { view_id: ViewId, path: PathBuf },
    ConfigChanged { view_id: ViewId, changes: Table },
    NewBuffer { buffer_info: Vec<PluginBufferInfo> },
    DidClose { view_id: ViewId },
    GetHover { view_id: ViewId, request_id: usize, position: usize },
    Shutdown(EmptyStruct),
    TracingConfig { enabled: bool },
    LanguageChanged { view_id: ViewId, new_lang: LanguageId },
    CustomCommand { view_id: ViewId, method: String, params: Value },
}

// ====================================================================
// plugin -> core RPC method types
// ====================================================================

/// A simple edit, received from a plugin.
#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct PluginEdit {
    pub rev: u64,
    pub delta: RopeDelta,
    /// the edit priority determines the resolution strategy when merging
    /// concurrent edits. The highest priority edit will be applied last.
    pub priority: u64,
    /// whether the inserted text prefers to be to the right of the cursor.
    pub after_cursor: bool,
    /// the originator of this edit: some identifier (plugin name, 'core', etc)
    /// undo_group associated with this edit
    pub undo_group: Option<usize>,
    pub author: String,
}

#[derive(Serialize, Deserialize, Debug, Clone, Copy)]
pub struct ScopeSpan {
    pub start: usize,
    pub end: usize,
    pub scope_id: u32,
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct DataSpan {
    pub start: usize,
    pub end: usize,
    pub data: Value,
}

/// The object returned by the `get_data` RPC.
#[derive(Debug, Serialize, Deserialize)]
pub struct GetDataResponse {
    pub chunk: String,
    pub offset: usize,
    pub first_line: usize,
    pub first_line_offset: usize,
}

/// The unit of measure when requesting data.
#[derive(Serialize, Deserialize, Debug, Clone, Copy)]
#[serde(rename_all = "snake_case")]
pub enum TextUnit {
    /// The requested offset is in bytes. The returned chunk will be valid
    /// UTF8, and is guaranteed to include the byte specified the offset.
    Utf8,
    /// The requested offset is a line number. The returned chunk will begin
    /// at the offset of the requested line.
    Line,
}

#[derive(Serialize, Deserialize, Debug, Clone)]
#[serde(rename_all = "snake_case")]
#[serde(tag = "method", content = "params")]
/// RPC requests sent from plugins.
pub enum PluginRequest {
    GetData { start: usize, unit: TextUnit, max_size: usize, rev: u64 },
    LineCount,
    GetSelections,
}

#[derive(Serialize, Deserialize, Debug, Clone)]
#[serde(rename_all = "snake_case")]
#[serde(tag = "method", content = "params")]
/// RPC commands sent from plugins.
pub enum PluginNotification {
    AddScopes {
        scopes: Vec<Vec<String>>,
    },
    UpdateSpans {
        start: usize,
        len: usize,
        spans: Vec<ScopeSpan>,
        rev: u64,
    },
    Edit {
        edit: PluginEdit,
    },
    Alert {
        msg: String,
    },
    AddStatusItem {
        key: String,
        value: String,
        alignment: String,
    },
    UpdateStatusItem {
        key: String,
        value: String,
    },
    RemoveStatusItem {
        key: String,
    },
    ShowHover {
        request_id: usize,
        result: Result<Hover, RemoteError>,
    },
    UpdateAnnotations {
        start: usize,
        len: usize,
        spans: Vec<DataSpan>,
        annotation_type: AnnotationType,
        rev: u64,
    },
}

/// Range expressed in terms of PluginPosition. Meant to be sent from
/// plugin to core.
#[derive(Serialize, Deserialize, Debug, Clone)]
#[serde(rename_all = "snake_case")]
pub struct Range {
    pub start: usize,
    pub end: usize,
}

/// Hover Item sent from Plugin to Core
#[derive(Serialize, Deserialize, Debug, Clone)]
#[serde(rename_all = "snake_case")]
pub struct Hover {
    pub content: String,
    pub range: Option<Range>,
}

/// Common wrapper for plugin-originating RPCs.
pub struct PluginCommand<T> {
    pub view_id: ViewId,
    pub plugin_id: PluginPid,
    pub cmd: T,
}

impl<T: Serialize> Serialize for PluginCommand<T> {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {...}
}

impl<'de, T: Deserialize<'de>> Deserialize<'de> for PluginCommand<T> {
    fn deserialize<D>(deserializer: D) -> Result<Self, D::Error>
    where
        D: Deserializer<'de>,
    {...}
}

impl PluginBufferInfo {
    pub fn new(
        buffer_id: BufferIdentifier,
        views: &[ViewId],
        rev: u64,
        buf_size: usize,
        nb_lines: usize,
        path: Option<PathBuf>,
        syntax: LanguageId,
        config: Table,
    ) -> Self {...}
}

impl PluginUpdate {
    pub fn new<D>(
        view_id: ViewId,
        rev: u64,
        delta: D,
        new_len: usize,
        new_line_count: usize,
        undo_group: Option<usize>,
        edit_type: String,
        author: String,
    ) -> Self
    where
        D: Into<Option<RopeDelta>>,
    {...}
}

// maybe this should be in xi_rope? has a strong resemblance to the various
// concrete `Metric` types.
impl TextUnit {
    /// Converts an offset in some unit to a concrete byte offset. Returns
    /// `None` if the input offset is out of bounds in its unit space.
    pub fn resolve_offset<T: Borrow<Rope>>(self, text: T, offset: usize) -> Option<usize> {...}
}
```

## xi-editor-ph7/rust/core-lib/src/recorder.rs

```rust
use std::collections::HashMap;

use crate::trace::trace_block;

use crate::edit_types::{BufferEvent, EventDomain};

/// A container that manages and holds all recordings for the current editing session
pub(crate) struct Recorder {
    active_recording: Option<String>,
    recording_buffer: Vec<EventDomain>,
    recordings: HashMap<String, Recording>,
}

impl Recorder {
    pub(crate) fn new() -> Recorder {...}

    pub(crate) fn is_recording(&self) -> bool {...}

    /// Starts or stops the specified recording.
    ///
    ///
    /// There are three outcome behaviors:
    /// - If the current recording name is specified, the active recording is saved
    /// - If no recording name is specified, the currently active recording is saved
    /// - If a recording name other than the active recording is specified,
    /// the current recording will be thrown out and will be switched to the new name
    ///
    /// In addition to the above:
    /// - If the recording was saved, there is no active recording
    /// - If the recording was switched, there will be a new active recording
    pub(crate) fn toggle_recording(&mut self, recording_name: Option<String>) {...}

    /// Saves an event into the currently active recording.
    ///
    /// Every sequential `BufferEvent::Insert` event will be merged together to cut down the number of
    /// `Editor::commit_delta` calls we need to make when playing back.
    pub(crate) fn record(&mut self, current_event: EventDomain) {...}

    /// Iterates over a specified recording's buffer and runs the specified action
    /// on each event.
    pub(crate) fn play<F>(&self, recording_name: &str, action: F)
    where
        F: FnMut(&EventDomain),
    {...}

    /// Completely removes the specified recording from the Recorder
    pub(crate) fn clear(&mut self, recording_name: &str) {...}

    /// Cleans the recording buffer by filtering out any undo or redo events and then saving it
    /// with the specified name.
    ///
    /// A recording should not store any undos or redos--
    /// call this once a recording is 'finalized.'
    fn save_recording_buffer(&mut self, recording_name: String) {...}
}

struct Recording {
    events: Vec<EventDomain>,
}

impl Recording {
    fn new(events: Vec<EventDomain>) -> Recording {...}

    /// Iterates over the recording buffer and runs the specified action
    /// on each event.
    fn play<F>(&self, action: F)
    where
        F: FnMut(&EventDomain),
    {...}
}

// Tests for filtering undo / redo from the recording buffer
// A = Event
// B = Event
// U = Undo
// R = Redo
```

## xi-editor-ph7/rust/core-lib/src/rpc.rs

```rust
use std::path::PathBuf;

use serde::de::{self, Deserialize, Deserializer};
use serde::ser::{self, Serialize, Serializer};
use serde_json::{self, Value};

use crate::config::{ConfigDomainExternal, Table};
use crate::plugins::PlaceholderRpc;
use crate::syntax::LanguageId;
use crate::tabs::ViewId;
use crate::view::Size;

// =============================================================================
//  Command types
// =============================================================================

#[derive(Serialize, Deserialize, Debug, PartialEq)]
#[doc(hidden)]
pub struct EmptyStruct {}

/// The notifications which make up the base of the protocol.
///
/// # Note
///
/// For serialization, all identifiers are converted to "snake_case".
///
/// # Examples
///
/// The `close_view` command:
///
/// ```
/// # extern crate xi_core_lib as xi_core;
/// extern crate serde_json;
/// use crate::xi_core::rpc::CoreNotification;
///
/// let json = r#"{
///     "method": "close_view",
///     "params": { "view_id": "view-id-1" }
///     }"#;
///
/// let cmd: CoreNotification = serde_json::from_str(&json).unwrap();
/// match cmd {
///     CoreNotification::CloseView { .. } => (), // expected
///     other => panic!("Unexpected variant"),
/// }
/// ```
///
/// The `client_started` command:
///
/// ```
/// # extern crate xi_core_lib as xi_core;
/// extern crate serde_json;
/// use crate::xi_core::rpc::CoreNotification;
///
/// let json = r#"{
///     "method": "client_started",
///     "params": {}
///     }"#;
///
/// let cmd: CoreNotification = serde_json::from_str(&json).unwrap();
/// match cmd {
///     CoreNotification::ClientStarted { .. }  => (), // expected
///     other => panic!("Unexpected variant"),
/// }
/// ```
#[derive(Serialize, Deserialize, Debug, PartialEq)]
#[serde(rename_all = "snake_case")]
#[serde(tag = "method", content = "params")]
pub enum CoreNotification {
    /// The 'edit' namespace, for view-specific editor actions.
    ///
    /// The params object has internal `method` and `params` members,
    /// which are parsed into the appropriate `EditNotification`.
    ///
    /// # Note:
    ///
    /// All edit commands (notifications and requests) include in their
    /// inner params object a `view_id` field. On the xi-core side, we
    /// pull out this value during parsing, and use it for routing.
    ///
    /// For more on the edit commands, see [`EditNotification`] and
    /// [`EditRequest`].
    ///
    /// [`EditNotification`]: enum.EditNotification.html
    /// [`EditRequest`]: enum.EditRequest.html
    ///
    /// # Examples
    ///
    /// ```
    /// # extern crate xi_core_lib as xi_core;
    /// #[macro_use]
    /// extern crate serde_json;
    /// use crate::xi_core::rpc::*;
    /// # fn main() {
    /// let edit = EditCommand {
    ///     view_id: 1.into(),
    ///     cmd: EditNotification::Insert { chars: "hello!".into() },
    /// };
    /// let rpc = CoreNotification::Edit(edit);
    /// let expected = json!({
    ///     "method": "edit",
    ///     "params": {
    ///         "method": "insert",
    ///         "view_id": "view-id-1",
    ///         "params": {
    ///             "chars": "hello!",
    ///         }
    ///     }
    /// });
    /// assert_eq!(serde_json::to_value(&rpc).unwrap(), expected);
    /// # }
    /// ```
    Edit(EditCommand<EditNotification>),
    /// The 'plugin' namespace, for interacting with plugins.
    ///
    /// As with edit commands, the params object has is a nested RPC,
    /// with the name of the command included as the `command` field.
    ///
    /// (this should be changed to more accurately reflect the behaviour
    /// of the edit commands).
    ///
    /// For the available commands, see [`PluginNotification`].
    ///
    /// [`PluginNotification`]: enum.PluginNotification.html
    ///
    /// # Examples
    ///
    /// ```
    /// # extern crate xi_core_lib as xi_core;
    /// #[macro_use]
    /// extern crate serde_json;
    /// use crate::xi_core::rpc::*;
    /// # fn main() {
    /// let rpc = CoreNotification::Plugin(
    ///     PluginNotification::Start {
    ///         view_id: 1.into(),
    ///         plugin_name: "syntect".into(),
    ///     });
    ///
    /// let expected = json!({
    ///     "method": "plugin",
    ///     "params": {
    ///         "command": "start",
    ///         "view_id": "view-id-1",
    ///         "plugin_name": "syntect",
    ///     }
    /// });
    /// assert_eq!(serde_json::to_value(&rpc).unwrap(), expected);
    /// # }
    /// ```
    Plugin(PluginNotification),
    /// Tells `xi-core` to close the specified view.
    CloseView { view_id: ViewId },
    /// Tells `xi-core` to save the contents of the specified view's
    /// buffer to the specified path.
    Save { view_id: ViewId, file_path: String },
    /// Tells `xi-core` to set the theme.
    SetTheme { theme_name: String },
    /// Notifies `xi-core` that the client has started.
    ClientStarted {
        #[serde(default)]
        config_dir: Option<PathBuf>,
        /// Path to additional plugins, included by the client.
        #[serde(default)]
        client_extras_dir: Option<PathBuf>,
    },
    /// Updates the user's config for the given domain. Where keys in
    /// `changes` are `null`, those keys are cleared in the user config
    /// for that domain; otherwise the config is updated with the new
    /// value.
    ///
    /// Note: If the client is using file-based config, the only valid
    /// domain argument is `ConfigDomain::UserOverride(_)`, which
    /// represents non-persistent view-specific settings, such as when
    /// a user manually changes whitespace settings for a given view.
    ModifyUserConfig { domain: ConfigDomainExternal, changes: Table },
    /// Control whether the tracing infrastructure is enabled.
    /// This propagates to all peers that should respond by toggling its own
    /// infrastructure on/off.
    TracingConfig { enabled: bool },
    /// Save trace data to the given path.  The core will first send
    /// CoreRequest::CollectTrace to all peers to collect the samples.
    SaveTrace { destination: PathBuf, frontend_samples: Value },
    /// Tells `xi-core` to set the language id for the view.
    SetLanguage { view_id: ViewId, language_id: LanguageId },
}

/// The requests which make up the base of the protocol.
///
/// All requests expect a response.
///
/// # Examples
///
/// The `new_view` command:
///
/// ```
/// # extern crate xi_core_lib as xi_core;
/// extern crate serde_json;
/// use crate::xi_core::rpc::CoreRequest;
///
/// let json = r#"{
///     "method": "new_view",
///     "params": { "file_path": "~/my_very_fun_file.rs" }
///     }"#;
///
/// let cmd: CoreRequest = serde_json::from_str(&json).unwrap();
/// match cmd {
///     CoreRequest::NewView { .. } => (), // expected
///     other => panic!("Unexpected variant {:?}", other),
/// }
/// ```
#[derive(Serialize, Deserialize, Debug, PartialEq)]
#[serde(rename_all = "snake_case")]
#[serde(tag = "method", content = "params")]
pub enum CoreRequest {
    /// The 'edit' namespace, for view-specific requests.
    Edit(EditCommand<EditRequest>),
    /// Tells `xi-core` to create a new view. If the `file_path`
    /// argument is present, `xi-core` should attempt to open the file
    /// at that location.
    ///
    /// Returns the view identifier that should be used to interact
    /// with the newly created view.
    NewView { file_path: Option<String> },
    /// Returns the current collated config object for the given view.
    GetConfig { view_id: ViewId },
    /// Returns the contents of the buffer for a given `ViewId`.
    /// In the future this might also be used to return structured data (such
    /// as for printing).
    DebugGetContents { view_id: ViewId },
}

/// A helper type, which extracts the `view_id` field from edit
/// requests and notifications.
///
/// Edit requests and notifications have 'method', 'params', and
/// 'view_id' param members. We use this wrapper, which has custom
/// `Deserialize` and `Serialize` implementations, to pull out the
/// `view_id` field.
///
/// # Examples
///
/// ```
/// # extern crate xi_core_lib as xi_core;
/// extern crate serde_json;
/// use crate::xi_core::rpc::*;
///
/// let json = r#"{
///     "view_id": "view-id-1",
///     "method": "scroll",
///     "params": [0, 6]
///     }"#;
///
/// let cmd: EditCommand<EditNotification> = serde_json::from_str(&json).unwrap();
/// match cmd.cmd {
///     EditNotification::Scroll( .. ) => (), // expected
///     other => panic!("Unexpected variant {:?}", other),
/// }
/// ```
#[derive(Debug, Clone, PartialEq)]
pub struct EditCommand<T> {
    pub view_id: ViewId,
    pub cmd: T,
}

/// The smallest unit of text that a gesture can select
#[derive(Serialize, Deserialize, PartialEq, Eq, Debug, Copy, Clone)]
#[serde(rename_all = "snake_case")]
pub enum SelectionGranularity {
    /// Selects any point or character range
    Point,
    /// Selects one word at a time
    Word,
    /// Selects one line at a time
    Line,
}

/// An enum representing touch and mouse gestures applied to the text.
#[derive(Serialize, Deserialize, PartialEq, Eq, Debug, Copy, Clone)]
#[serde(rename_all = "snake_case")]
pub enum GestureType {
    Select { granularity: SelectionGranularity, multi: bool },
    SelectExtend { granularity: SelectionGranularity },
    Drag,

    // Deprecated
    PointSelect,
    ToggleSel,
    RangeSelect,
    LineSelect,
    WordSelect,
    MultiLineSelect,
    MultiWordSelect,
}

/// An inclusive range.
///
/// # Note:
///
/// Several core protocol commands use a params array to pass arguments
/// which are named, internally. this type use custom Serialize /
/// Deserialize impls to accommodate this.
#[derive(PartialEq, Eq, Debug, Clone)]
pub struct LineRange {
    pub first: i64,
    pub last: i64,
}

/// A mouse event. See the note for [`LineRange`].
///
/// [`LineRange`]: enum.LineRange.html
#[derive(PartialEq, Eq, Debug, Clone)]
pub struct MouseAction {
    pub line: u64,
    pub column: u64,
    pub flags: u64,
    pub click_count: Option<u64>,
}

#[derive(Serialize, Deserialize, PartialEq, Eq, Debug, Clone)]
pub struct Position {
    pub line: usize,
    pub column: usize,
}

/// Represents how the current selection is modified (used by find
/// operations).
#[derive(Serialize, Deserialize, PartialEq, Eq, Debug, Clone)]
#[serde(rename_all = "snake_case")]
#[derive(Default)]
pub enum SelectionModifier {
    None,
    #[default]
    Set,
    Add,
    AddRemovingCurrent,
}

#[derive(Serialize, Deserialize, PartialEq, Eq, Debug, Clone)]
#[serde(rename_all = "snake_case")]
pub struct FindQuery {
    pub id: Option<usize>,
    pub chars: String,
    pub case_sensitive: bool,
    #[serde(default)]
    pub regex: bool,
    #[serde(default)]
    pub whole_words: bool,
}

/// The edit-related notifications.
///
/// Alongside the [`EditRequest`] members, these commands constitute
/// the API for interacting with a particular window and document.
#[derive(Serialize, Deserialize, Debug, PartialEq)]
#[serde(rename_all = "snake_case")]
#[serde(tag = "method", content = "params")]
pub enum EditNotification {
    Insert {
        chars: String,
    },
    Paste {
        chars: String,
    },
    DeleteForward,
    DeleteBackward,
    DeleteWordForward,
    DeleteWordBackward,
    DeleteToEndOfParagraph,
    DeleteToBeginningOfLine,
    InsertNewline,
    InsertTab,
    MoveUp,
    MoveUpAndModifySelection,
    MoveDown,
    MoveDownAndModifySelection,
    MoveLeft,
    // synoynm for `MoveLeft`
    MoveBackward,
    MoveLeftAndModifySelection,
    MoveRight,
    // synoynm for `MoveRight`
    MoveForward,
    MoveRightAndModifySelection,
    MoveWordLeft,
    MoveWordLeftAndModifySelection,
    MoveWordRight,
    MoveWordRightAndModifySelection,
    MoveToBeginningOfParagraph,
    MoveToBeginningOfParagraphAndModifySelection,
    MoveToEndOfParagraph,
    MoveToEndOfParagraphAndModifySelection,
    MoveToLeftEndOfLine,
    MoveToLeftEndOfLineAndModifySelection,
    MoveToRightEndOfLine,
    MoveToRightEndOfLineAndModifySelection,
    MoveToBeginningOfDocument,
    MoveToBeginningOfDocumentAndModifySelection,
    MoveToEndOfDocument,
    MoveToEndOfDocumentAndModifySelection,
    ScrollPageUp,
    PageUpAndModifySelection,
    ScrollPageDown,
    PageDownAndModifySelection,
    SelectAll,
    AddSelectionAbove,
    AddSelectionBelow,
    Scroll(LineRange),
    Resize(Size),
    GotoLine {
        line: u64,
    },
    RequestLines(LineRange),
    Yank,
    Transpose,
    Click(MouseAction),
    Drag(MouseAction),
    Gesture {
        line: u64,
        col: u64,
        ty: GestureType,
    },
    Undo,
    Redo,
    Find {
        chars: String,
        case_sensitive: bool,
        #[serde(default)]
        regex: bool,
        #[serde(default)]
        whole_words: bool,
    },
    MultiFind {
        queries: Vec<FindQuery>,
    },
    FindNext {
        #[serde(default)]
        wrap_around: bool,
        #[serde(default)]
        allow_same: bool,
        #[serde(default)]
        modify_selection: SelectionModifier,
    },
    FindPrevious {
        #[serde(default)]
        wrap_around: bool,
        #[serde(default)]
        allow_same: bool,
        #[serde(default)]
        modify_selection: SelectionModifier,
    },
    FindAll,
    DebugRewrap,
    DebugWrapWidth,
    /// Prints the style spans present in the active selection.
    DebugPrintSpans,
    DebugToggleComment,
    Uppercase,
    Lowercase,
    Capitalize,
    Reindent,
    Indent,
    Outdent,
    /// Indicates whether find highlights should be rendered
    HighlightFind {
        visible: bool,
    },
    SelectionForFind {
        #[serde(default)]
        case_sensitive: bool,
    },
    Replace {
        chars: String,
        #[serde(default)]
        preserve_case: bool,
    },
    ReplaceNext,
    ReplaceAll,
    SelectionForReplace,
    RequestHover {
        request_id: usize,
        position: Option<Position>,
    },
    SelectionIntoLines,
    DuplicateLine,
    IncreaseNumber,
    DecreaseNumber,
    ToggleRecording {
        recording_name: Option<String>,
    },
    PlayRecording {
        recording_name: String,
    },
    ClearRecording {
        recording_name: String,
    },
    CollapseSelections,
}

/// The edit related requests.
#[derive(Serialize, Deserialize, Debug, PartialEq)]
#[serde(rename_all = "snake_case")]
#[serde(tag = "method", content = "params")]
pub enum EditRequest {
    /// Cuts the active selection, returning their contents,
    /// or `Null` if the selection was empty.
    Cut,
    /// Copies the active selection, returning their contents or
    /// or `Null` if the selection was empty.
    Copy,
}

/// The plugin related notifications.
#[derive(Serialize, Deserialize, Debug, PartialEq)]
#[serde(tag = "command")]
#[serde(rename_all = "snake_case")]
pub enum PluginNotification {
    Start { view_id: ViewId, plugin_name: String },
    Stop { view_id: ViewId, plugin_name: String },
    PluginRpc { view_id: ViewId, receiver: String, rpc: PlaceholderRpc },
}

// Serialize / Deserialize

impl<T: Serialize> Serialize for EditCommand<T> {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {...}
}

impl<'de, T: Deserialize<'de>> Deserialize<'de> for EditCommand<T> {
    fn deserialize<D>(deserializer: D) -> Result<Self, D::Error>
    where
        D: Deserializer<'de>,
    {...}
}

impl Serialize for MouseAction {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {...}
}

impl<'de> Deserialize<'de> for MouseAction {
    fn deserialize<D>(deserializer: D) -> Result<Self, D::Error>
    where
        D: Deserializer<'de>,
    {...}
}

impl Serialize for LineRange {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {...}
}

impl<'de> Deserialize<'de> for LineRange {
    fn deserialize<D>(deserializer: D) -> Result<Self, D::Error>
    where
        D: Deserializer<'de>,
    {...}
}
```

## xi-editor-ph7/rust/core-lib/src/selection.rs

```rust
use std::cmp::{max, min};
use std::fmt;
use std::ops::Deref;

use crate::annotations::{AnnotationRange, AnnotationSlice, AnnotationType, ToAnnotation};
use crate::index_set::remove_n_at;
use crate::line_offset::LineOffset;
use crate::view::View;
use xi_rope::{Interval, Rope, RopeDelta, Transformer};

/// A type representing horizontal measurements. This is currently in units
/// that are not very well defined except that ASCII characters count as
/// 1 each. It will change.
pub type HorizPos = usize;

/// Indicates if an edit should try to drift inside or outside nearby selections. If the selection
/// is zero width, that is, it is a caret, this value will be ignored, the equivalent of the
/// `Default` value.
#[derive(Copy, Clone)]
pub enum InsertDrift {
    /// Indicates this edit should happen within any (non-caret) selections if possible.
    Inside,
    /// Indicates this edit should happen outside any selections if possible.
    Outside,
    /// Indicates to do whatever the `after` bool says to do
    Default,
}

/// A set of zero or more selection regions, representing a selection state.
#[derive(Default, Debug, Clone)]
pub struct Selection {
    // An invariant: regions[i].max() <= regions[i+1].min()
    // and < if either is_caret()
    regions: Vec<SelRegion>,
}

impl Selection {
    /// Creates a new empty selection.
    pub fn new() -> Selection {...}

    /// Creates a selection with a single region.
    pub fn new_simple(region: SelRegion) -> Selection {...}

    /// Clear the selection.
    pub fn clear(&mut self) {...}

    /// Collapse all selections into a single caret.
    pub fn collapse(&mut self) {...}

    // The smallest index so that offset > region.max() for all preceding
    // regions.
    pub fn search(&self, offset: usize) -> usize {...}

    /// Add a region to the selection. This method implements merging logic.
    ///
    /// Two non-caret regions merge if their interiors intersect; merely
    /// touching at the edges does not cause a merge. A caret merges with
    /// a non-caret if it is in the interior or on either edge. Two carets
    /// merge if they are the same offset.
    ///
    /// Performance note: should be O(1) if the new region strictly comes
    /// after all the others in the selection, otherwise O(n).
    pub fn add_region(&mut self, region: SelRegion) {...}

    /// Gets a slice of regions that intersect the given range. Regions that
    /// merely touch the range at the edges are also included, so it is the
    /// caller's responsibility to further trim them, in particular to only
    /// display one caret in the upstream/downstream cases.
    ///
    /// Performance note: O(log n).
    pub fn regions_in_range(&self, start: usize, end: usize) -> &[SelRegion] {...}

    /// Deletes all the regions that intersect or (if delete_adjacent = true) touch the given range.
    pub fn delete_range(&mut self, start: usize, end: usize, delete_adjacent: bool) {...}

    /// Add a region to the selection. This method does not merge regions and does not allow
    /// ambiguous regions (regions that overlap).
    ///
    /// On ambiguous regions, the region with the lower start position wins. That is, in such a
    /// case, the new region is either not added at all, because there is an ambiguous region with
    /// a lower start position, or existing regions that intersect with the new region but do
    /// not start before the new region, are deleted.
    #[allow(clippy::suspicious_operation_groupings)]
    pub fn add_range_distinct(&mut self, region: SelRegion) -> (usize, usize) {...}

    /// Computes a new selection based on applying a delta to the old selection.
    ///
    /// When new text is inserted at a caret, the new caret can be either before
    /// or after the inserted text, depending on the `after` parameter.
    ///
    /// Whether or not the preceding selections are restored depends on the keep_selections
    /// value (only set to true on transpose).
    pub fn apply_delta(&self, delta: &RopeDelta, after: bool, drift: InsertDrift) -> Selection {...}
}

/// Implementing the `ToAnnotation` trait allows to convert selections to annotations.
impl ToAnnotation for Selection {
    fn get_annotations(&self, interval: Interval, view: &View, text: &Rope) -> AnnotationSlice {...}
}

/// Implementing the Deref trait allows callers to easily test `is_empty`, iterate
/// through all ranges, etc.
impl Deref for Selection {
    type Target = [SelRegion];

    fn deref(&self) -> &[SelRegion] {...}
}

/// The "affinity" of a cursor which is sitting exactly on a line break.
///
/// We say "cursor" here rather than "caret" because (depending on presentation)
/// the front-end may draw a cursor even when the region is not a caret.
#[derive(Clone, Copy, PartialEq, Eq, Debug, Default)]
pub enum Affinity {
    /// The cursor should be displayed downstream of the line break. For
    /// example, if the buffer is "abcd", and the cursor is on a line break
    /// after "ab", it should be displayed on the second line before "cd".
    #[default]
    Downstream,
    /// The cursor should be displayed upstream of the line break. For
    /// example, if the buffer is "abcd", and the cursor is on a line break
    /// after "ab", it should be displayed on the previous line after "ab".
    Upstream,
}

/// A type representing a single contiguous region of a selection. We use the
/// term "caret" (sometimes also "cursor", more loosely) to refer to a selection
/// region with an empty interior. A "non-caret region" is one with a non-empty
/// interior (i.e. `start != end`).
#[derive(Clone, Copy, PartialEq, Eq, Debug)]
pub struct SelRegion {
    /// The inactive edge of a selection, as a byte offset. When
    /// equal to end, the selection range acts as a caret.
    pub start: usize,

    /// The active edge of a selection, as a byte offset.
    pub end: usize,

    /// A saved horizontal position (used primarily for line up/down movement).
    pub horiz: Option<HorizPos>,

    /// The affinity of the cursor.
    pub affinity: Affinity,
}

impl SelRegion {
    /// Returns a new region.
    pub fn new(start: usize, end: usize) -> Self {...}

    /// Returns a new caret region (`start == end`).
    pub fn caret(pos: usize) -> Self {...}

    /// Returns a region with the given horizontal position.
    pub fn with_horiz(self, horiz: Option<HorizPos>) -> Self {...}

    /// Returns a region with the given affinity.
    pub fn with_affinity(self, affinity: Affinity) -> Self {...}

    /// Gets the earliest offset within the region, ie the minimum of both edges.
    pub fn min(self) -> usize {...}

    /// Gets the latest offset within the region, ie the maximum of both edges.
    pub fn max(self) -> usize {...}

    /// Determines whether the region is a caret (ie has an empty interior).
    pub fn is_caret(self) -> bool {...}

    /// Determines whether the region's affinity is upstream.
    pub fn is_upstream(self) -> bool {...}

    // Indicate whether this region should merge with the next.
    // Assumption: regions are sorted (self.min() <= other.min())
    #[allow(clippy::suspicious_operation_groupings)] // clippy doesn't like comparing min() to max()
    fn should_merge(self, other: SelRegion) -> bool {...}

    // Merge self with an overlapping region.
    // Retains direction of self.
    fn merge_with(self, other: SelRegion) -> SelRegion {...}
}

impl<'a> From<&'a SelRegion> for Interval {
    fn from(src: &'a SelRegion) -> Interval {...}
}

impl From<Interval> for SelRegion {
    fn from(src: Interval) -> SelRegion {...}
}

impl From<SelRegion> for Selection {
    fn from(region: SelRegion) -> Self {...}
}

impl fmt::Display for Selection {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {...}
}

impl fmt::Display for SelRegion {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {...}
}
```

## xi-editor-ph7/rust/core-lib/src/styles.rs

```rust
use std::collections::{BTreeMap, HashMap, HashSet};
use std::ffi::OsStr;
use std::fmt;
use std::fs;
use std::path::{Path, PathBuf};

use serde_json::{self, Value};
use syntect::dumps::{dump_to_file, from_dump_file};
use syntect::highlighting::StyleModifier as SynStyleModifier;
use syntect::highlighting::{Color, Highlighter, Theme, ThemeSet};
use syntect::LoadingError;

pub use syntect::highlighting::ThemeSettings;

pub const N_RESERVED_STYLES: usize = 8;
const SYNTAX_PRIORITY_DEFAULT: u16 = 200;
const SYNTAX_PRIORITY_LOWEST: u16 = 0;
pub const DEFAULT_THEME: &str = "InspiredGitHub";

#[derive(Clone, PartialEq, Eq, Default, Hash, Serialize, Deserialize)]
/// A mergeable style. All values except priority are optional.
///
/// Note: A `None` value represents the absense of preference; in the case of
/// boolean options, `Some(false)` means that this style will override a lower
/// priority value in the same field.
pub struct Style {
    /// The priority of this style, in the range (0, 1000). Used to resolve
    /// conflicting fields when merging styles. The higher priority wins.
    #[serde(skip_serializing)]
    pub priority: u16,
    /// The foreground text color, in ARGB.
    pub fg_color: Option<u32>,
    /// The background text color, in ARGB.
    #[serde(skip_serializing_if = "Option::is_none")]
    pub bg_color: Option<u32>,
    /// The font-weight, in the range 100-900, interpreted like the CSS
    /// font-weight property.
    #[serde(skip_serializing_if = "Option::is_none")]
    pub weight: Option<u16>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub underline: Option<bool>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub italic: Option<bool>,
}

impl Style {
    /// Creates a new `Style` by converting from a `Syntect::StyleModifier`.
    pub fn from_syntect_style_mod(style: &SynStyleModifier) -> Self {...}

    pub fn new<O32, O16, OB>(
        priority: u16,
        fg_color: O32,
        bg_color: O32,
        weight: O16,
        underline: OB,
        italic: OB,
    ) -> Self
    where
        O32: Into<Option<u32>>,
        O16: Into<Option<u16>>,
        OB: Into<Option<bool>>,
    {...}

    /// Returns the default style for the given `Theme`.
    pub fn default_for_theme(theme: &Theme) -> Self {...}

    /// Creates a new style by combining attributes of `self` and `other`.
    /// If both styles define an attribute, the highest priority wins; `other`
    /// wins in the case of a tie.
    ///
    /// Note: when merging multiple styles, apply them in increasing priority.
    pub fn merge(&self, other: &Style) -> Style {...}

    /// Encode this `Style`, setting the `id` property.
    ///
    /// Note: this should only be used when sending the `def_style` RPC.
    pub fn to_json(&self, id: usize) -> Value {...}

    fn rgba_from_syntect_color(color: Color) -> u32 {...}
}

/// A map from styles to client identifiers for a given `Theme`.
pub struct ThemeStyleMap {
    themes: ThemeSet,
    theme_name: String,
    theme: Theme,

    // Keeps a list of default themes.
    default_themes: Vec<String>,
    default_style: Style,
    map: HashMap<Style, usize>,

    // Maintains the map of found themes and their paths.
    path_map: BTreeMap<String, PathBuf>,

    // It's not obvious we actually have to store the style, we seem to only need it
    // as the key in the map.
    styles: Vec<Style>,
    themes_dir: Option<PathBuf>,
    cache_dir: Option<PathBuf>,
    caching_enabled: bool,
}

impl ThemeStyleMap {
    pub fn new(themes_dir: Option<PathBuf>) -> ThemeStyleMap {...}

    pub fn get_default_style(&self) -> &Style {...}

    pub fn get_highlighter(&self) -> Highlighter<'_> {...}

    pub fn get_theme_name(&self) -> &str {...}

    pub fn get_theme_settings(&self) -> &ThemeSettings {...}

    pub fn get_theme_names(&self) -> Vec<String> {...}

    pub fn contains_theme(&self, k: &str) -> bool {...}

    pub fn set_theme(&mut self, theme_name: &str) -> Result<(), &'static str> {...}

    pub fn merge_with_default(&self, style: &Style) -> Style {...}

    pub fn lookup(&self, style: &Style) -> Option<usize> {...}

    pub fn add(&mut self, style: &Style) -> usize {...}

    /// Delete key and the corresponding dump file from the themes map.
    pub(crate) fn remove_theme(&mut self, path: &Path) -> Option<String> {...}

    /// Cache all themes names and their paths inside the given directory.
    pub(crate) fn load_theme_dir(&mut self) {...}

    /// A wrapper around `from_dump_file`
    /// to validate the state of dump file.
    /// Invalidates if mod time of dump is less
    /// than the original one.
    fn try_load_from_dump(&self, theme_p: &Path) -> Option<(String, Theme)> {...}

    /// Loads a theme's name and its respective path into the theme path map.
    pub(crate) fn load_theme_info_from_path(
        &mut self,
        theme_p: &Path,
    ) -> Result<String, LoadingError> {...}

    /// Loads theme using syntect's `get_theme` fn to our `theme` path map.
    /// Stores binary dump in a file with `tmdump` extension, only if
    /// caching is enabled.
    fn load_theme(&mut self, theme_name: &str) -> Result<(), LoadingError> {...}

    fn insert_to_map(&mut self, k: String, v: Theme) {...}

    /// Returns dump's path corresponding to the given theme name.
    fn get_dump_path(&self, theme_name: &str) -> Option<PathBuf> {...}

    /// Compare the stored file paths in `self.state`
    /// to the present ones.
    pub(crate) fn sync_dir(&mut self, dir: Option<&Path>) {...}
    /// Creates the cache dir returns true
    /// if it is successfully initialized or
    /// already exists.
    fn init_cache_dir(&mut self) -> bool {...}
}

/// Used to remove files with extension other than `tmTheme`.
fn validate_theme_file(path: &Path) -> Result<bool, LoadingError> {...}

impl fmt::Debug for Style {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {...}
}
```

## xi-editor-ph7/rust/core-lib/src/syntax.rs

```rust
use std::borrow::Borrow;
use std::collections::{BTreeMap, HashMap};
use std::path::Path;
use std::sync::Arc;

use crate::config::Table;

/// The canonical identifier for a particular `LanguageDefinition`.
#[derive(Debug, Default, Clone, Serialize, Deserialize, PartialEq, Eq, Hash, PartialOrd, Ord)]
#[allow(clippy::rc_buffer)] // suppress clippy;  TODO consider addressing
                            // the warning by changing String to str
pub struct LanguageId(Arc<String>);

/// Describes a `LanguageDefinition`. Although these are provided by plugins,
/// they are a fundamental concept in core, used to determine things like
/// plugin activations and active user config tables.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct LanguageDefinition {
    pub name: LanguageId,
    pub extensions: Vec<String>,
    pub first_line_match: Option<String>,
    pub scope: String,
    #[serde(skip)]
    pub default_config: Option<Table>,
}

/// A repository of all loaded `LanguageDefinition`s.
#[derive(Debug, Default)]
pub struct Languages {
    // NOTE: BTreeMap is used for sorting the languages by name alphabetically
    named: BTreeMap<LanguageId, Arc<LanguageDefinition>>,
    extensions: HashMap<String, Arc<LanguageDefinition>>,
}

impl Languages {
    pub fn new(language_defs: &[LanguageDefinition]) -> Self {...}

    pub fn language_for_path(&self, path: &Path) -> Option<Arc<LanguageDefinition>> {...}

    pub fn language_for_name<S>(&self, name: S) -> Option<Arc<LanguageDefinition>>
    where
        S: AsRef<str>,
    {...}

    /// Returns a Vec of any `LanguageDefinition`s which exist
    /// in `self` but not `other`.
    pub fn difference(&self, other: &Languages) -> Vec<Arc<LanguageDefinition>> {...}

    pub fn iter(&self) -> impl Iterator<Item = &Arc<LanguageDefinition>> {...}
}

impl AsRef<str> for LanguageId {
    fn as_ref(&self) -> &str {...}
}

// let's us use &str to query a HashMap with `LanguageId` keys
impl Borrow<str> for LanguageId {
    fn borrow(&self) -> &str {...}
}

impl<'a> From<&'a str> for LanguageId {
    fn from(src: &'a str) -> LanguageId {...}
}

// for testing
```

## xi-editor-ph7/rust/core-lib/src/tabs.rs

```rust
use std::cell::{Cell, RefCell};
use std::collections::{BTreeMap, HashSet};
use std::fmt;
use std::fs::File;
use std::io;
use std::mem;
use std::path::{Path, PathBuf};

use serde::de::{self, Deserialize, Deserializer, Unexpected};
use serde::ser::{Serialize, Serializer};
use serde_json::Value;

use crate::trace::trace_block;
use xi_rope::Rope;
use xi_rpc::{self, ReadError, RemoteError, RpcCtx, RpcPeer};

use crate::client::Client;
use crate::config::{self, ConfigDomain, ConfigDomainExternal, ConfigManager, Table};
use crate::editor::Editor;
use crate::event_context::EventContext;
use crate::file::FileManager;
use crate::line_ending::LineEnding;
use crate::plugin_rpc::{PluginNotification, PluginRequest};
use crate::plugins::rpc::ClientPluginInfo;
use crate::plugins::{start_plugin_process, Plugin, PluginCatalog, PluginPid};
use crate::recorder::Recorder;
use crate::rpc::{
    CoreNotification, CoreRequest, EditNotification, EditRequest,
    PluginNotification as CorePluginNotification,
};
use crate::styles::{ThemeStyleMap, DEFAULT_THEME};
use crate::syntax::LanguageId;
use crate::view::View;
use crate::whitespace::Indentation;
use crate::width_cache::WidthCache;
use crate::WeakXiCore;

#[cfg(feature = "notify")]
use crate::watcher::{FileWatcher, WatchToken};
#[cfg(feature = "notify")]
use notify::Event;
#[cfg(feature = "notify")]
use std::ffi::OsStr;

/// ViewIds are the primary means of routing messages between
/// xi-core and a client view.
#[derive(Debug, Clone, Copy, PartialEq, Eq, PartialOrd, Ord, Hash)]
pub struct ViewId(pub(crate) usize);

/// BufferIds uniquely identify open buffers.
#[derive(Debug, Clone, Copy, PartialEq, Eq, PartialOrd, Ord, Serialize, Deserialize, Hash)]
pub struct BufferId(pub(crate) usize);

pub type PluginId = crate::plugins::PluginPid;

// old-style names; will be deprecated
pub type BufferIdentifier = BufferId;

/// Totally arbitrary; we reserve this space for `ViewId`s
pub(crate) const RENDER_VIEW_IDLE_MASK: usize = 1 << 25;
pub(crate) const REWRAP_VIEW_IDLE_MASK: usize = 1 << 26;
pub(crate) const FIND_VIEW_IDLE_MASK: usize = 1 << 27;

const NEW_VIEW_IDLE_TOKEN: usize = 1001;

/// xi_rpc idle Token for watcher related idle scheduling.
pub(crate) const WATCH_IDLE_TOKEN: usize = 1002;

#[cfg(feature = "notify")]
const CONFIG_EVENT_TOKEN: WatchToken = WatchToken(1);

/// Token for file-change events in open files
#[cfg(feature = "notify")]
pub const OPEN_FILE_EVENT_TOKEN: WatchToken = WatchToken(2);

#[cfg(feature = "notify")]
const THEME_FILE_EVENT_TOKEN: WatchToken = WatchToken(3);

#[cfg(feature = "notify")]
const PLUGIN_EVENT_TOKEN: WatchToken = WatchToken(4);

#[allow(dead_code)]
pub struct CoreState {
    editors: BTreeMap<BufferId, RefCell<Editor>>,
    views: BTreeMap<ViewId, RefCell<View>>,
    file_manager: FileManager,
    /// A local pasteboard.
    kill_ring: RefCell<Rope>,
    /// Theme and style state.
    style_map: RefCell<ThemeStyleMap>,
    width_cache: RefCell<WidthCache>,
    /// User and platform specific settings
    config_manager: ConfigManager,
    /// Recorded editor actions
    recorder: RefCell<Recorder>,
    /// A weak reference to the main state container, stashed so that
    /// it can be passed to plugins.
    self_ref: Option<WeakXiCore>,
    /// Views which need to have setup finished.
    pending_views: Vec<(ViewId, Table)>,
    peer: Client,
    id_counter: Counter,
    plugins: PluginCatalog,
    // for the time being we auto-start all plugins we find on launch.
    running_plugins: Vec<Plugin>,
}

/// Initial setup and bookkeeping
impl CoreState {
    pub(crate) fn new(
        peer: &RpcPeer,
        config_dir: Option<PathBuf>,
        extras_dir: Option<PathBuf>,
    ) -> Self {...}

    fn next_view_id(&self) -> ViewId {...}

    fn next_buffer_id(&self) -> BufferId {...}

    fn next_plugin_id(&self) -> PluginId {...}

    pub(crate) fn finish_setup(&mut self, self_ref: WeakXiCore) {...}

    /// Attempt to load a config file.
    fn load_file_based_config(&mut self, path: &Path) {...}

    /// Sets (overwriting) the config for a given domain.
    fn set_config(&mut self, domain: ConfigDomain, table: Table) {...}

    /// Notify editors/views/plugins of config changes.
    fn handle_config_changes(&self, changes: Vec<(BufferId, Table)>) {...}
}

/// Handling client events
impl CoreState {
    /// Creates an `EventContext` for the provided `ViewId`. This context
    /// holds references to the `Editor` and `View` backing this `ViewId`,
    /// as well as to sibling views, plugins, and other state necessary
    /// for handling most events.
    pub(crate) fn make_context(&self, view_id: ViewId) -> Option<EventContext<'_>> {...}

    /// Produces an iterator over all event contexts, with each view appearing
    /// exactly once.
    fn iter_groups<'a>(&'a self) -> Iter<'a, Box<dyn Iterator<Item = &'a ViewId> + 'a>> {...}

    pub(crate) fn client_notification(&mut self, cmd: CoreNotification) {...}

    pub(crate) fn client_request(&mut self, cmd: CoreRequest) -> Result<Value, RemoteError> {...}

    fn do_edit(&mut self, view_id: ViewId, cmd: EditNotification) {...}

    fn do_edit_sync(&mut self, view_id: ViewId, cmd: EditRequest) -> Result<Value, RemoteError> {...}

    fn do_new_view(&mut self, path: Option<PathBuf>) -> Result<Value, RemoteError> {...}

    fn do_save<P>(&mut self, view_id: ViewId, path: P)
    where
        P: AsRef<Path>,
    {...}

    fn do_close_view(&mut self, view_id: ViewId) {...}

    fn do_set_theme(&self, theme_name: &str) {...}

    fn notify_client_and_update_views(&self) {...}

    /// Updates the config for a given domain.
    fn do_modify_user_config(&mut self, domain: ConfigDomainExternal, changes: Table) {...}

    fn do_get_config(&self, view_id: ViewId) -> Result<Table, RemoteError> {...}

    fn do_get_contents(&self, view_id: ViewId) -> Result<Rope, RemoteError> {...}

    fn do_set_language(&mut self, view_id: ViewId, language_id: LanguageId) {...}

    fn do_start_plugin(&mut self, _view_id: ViewId, plugin: &str) {...}

    fn do_stop_plugin(&mut self, _view_id: ViewId, plugin: &str) {...}

    fn do_plugin_rpc(&self, view_id: ViewId, receiver: &str, method: &str, params: &Value) {...}

    fn after_stop_plugin(&mut self, plugin: &Plugin) {...}
}

/// Idle, tracing, and file event handling
impl CoreState {
    pub(crate) fn handle_idle(&mut self, token: usize) {...}

    fn finalize_new_views(&mut self) {...}

    // Detects whitespace settings from the file and merges them with the config
    fn detect_whitespace(&mut self, id: ViewId, config: &Table) -> Option<Table> {...}

    fn handle_render_timer(&mut self, token: usize) {...}

    /// Callback for doing word wrap on a view
    fn handle_rewrap_callback(&mut self, token: usize) {...}

    /// Callback for doing incremental find in a view
    fn handle_find_callback(&mut self, token: usize) {...}

    #[cfg(feature = "notify")]
    fn handle_fs_events(&mut self) {...}

    #[cfg(not(feature = "notify"))]
    fn handle_fs_events(&mut self) {...}

    /// Handles a file system event related to a currently open file
    #[cfg(feature = "notify")]
    fn handle_open_file_fs_event(&mut self, event: Event) {...}

    /// Handles a config related file system event.
    #[cfg(feature = "notify")]
    fn handle_config_fs_event(&mut self, event: Event) {...}

    fn remove_config_at_path(&mut self, path: &Path) {...}

    /// Handles changes in plugin files.
    #[cfg(feature = "notify")]
    fn handle_plugin_fs_event(&mut self, event: Event) {...}

    /// Handles changes in theme files.
    #[cfg(feature = "notify")]
    fn handle_themes_fs_event(&mut self, event: Event) {...}

    /// Load a single theme file. Updates if already present.
    fn load_theme_file(&mut self, path: &Path) {...}

    fn remove_theme(&mut self, path: &Path) {...}

    fn toggle_tracing(&self, enabled: bool) {...}

    #[cfg(feature = "trace")]
    fn save_trace<P>(&self, path: P, frontend_samples: Value)
    where
        P: AsRef<Path>,
    {...}

    #[cfg(not(feature = "trace"))]
    fn save_trace<P>(&self, _path: P, _frontend_samples: Value)
    where
        P: AsRef<Path>,
    {...}
}

/// plugin event handling
impl CoreState {
    /// Called from a plugin's thread after trying to start the plugin.
    pub(crate) fn plugin_connect(&mut self, plugin: Result<Plugin, io::Error>) {...}

    pub(crate) fn plugin_exit(&mut self, id: PluginId, error: Result<(), ReadError>) {...}

    /// Handles the response to a sync update sent to a plugin.
    pub(crate) fn plugin_update(
        &mut self,
        _plugin_id: PluginId,
        view_id: ViewId,
        response: Result<Value, xi_rpc::Error>,
    ) {...}

    pub(crate) fn plugin_notification(
        &mut self,
        _ctx: &RpcCtx,
        view_id: ViewId,
        plugin_id: PluginId,
        cmd: PluginNotification,
    ) {...}

    pub(crate) fn plugin_request(
        &mut self,
        _ctx: &RpcCtx,
        view_id: ViewId,
        plugin_id: PluginId,
        cmd: PluginRequest,
    ) -> Result<Value, RemoteError> {...}
}

/// test helpers
impl CoreState {
    pub fn _test_open_editors(&self) -> Vec<BufferId> {...}

    pub fn _test_open_views(&self) -> Vec<ViewId> {...}
}

pub mod test_helpers {
    use super::{BufferId, ViewId};

    pub fn new_view_id(id: usize) -> ViewId {...}

    pub fn new_buffer_id(id: usize) -> BufferId {...}
}

/// A multi-view aware iterator over `EventContext`s. A view which appears
/// as a sibling will not appear again as a main view.
pub struct Iter<'a, I> {
    views: I,
    seen: HashSet<ViewId>,
    inner: &'a CoreState,
}

impl<'a, I> Iterator for Iter<'a, I>
where
    I: Iterator<Item = &'a ViewId>,
{
    type Item = EventContext<'a>;

    fn next(&mut self) -> Option<Self::Item> {...}
}

#[derive(Debug, Default)]
pub(crate) struct Counter(Cell<usize>);

impl Counter {
    pub(crate) fn next(&self) -> usize {...}
}

// these two only exist so that we can use ViewIds as idle tokens
impl From<usize> for ViewId {
    fn from(src: usize) -> ViewId {...}
}

impl From<ViewId> for usize {
    fn from(src: ViewId) -> usize {...}
}

impl fmt::Display for ViewId {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {...}
}

impl Serialize for ViewId {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {...}
}

impl<'de> Deserialize<'de> for ViewId {
    fn deserialize<D>(deserializer: D) -> Result<Self, D::Error>
    where
        D: Deserializer<'de>,
    {...}
}

impl fmt::Display for BufferId {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {...}
}

impl BufferId {
    pub fn new(val: usize) -> Self {...}
}
```

## xi-editor-ph7/rust/core-lib/src/trace.rs

```rust
#[cfg(feature = "trace")]
pub use xi_trace::{
    chrome_trace_dump, disable_tracing, enable_tracing, is_enabled, samples_cloned_unsorted,
    trace_block, trace_payload, Sample, SampleGuard,
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

    pub fn trace_block<'a, S, C>(_name: S, _categories: C) -> SampleGuard<'a> {...}

    pub fn trace_payload<S, C, P>(_name: S, _categories: C, _payload: P) {...}

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
    }

    pub use chrome_trace_dump;
    pub use SampleGuard;
}

#[cfg(not(feature = "trace"))]
pub use shim::{
    chrome_trace_dump, disable_tracing, enable_tracing, is_enabled, samples_cloned_unsorted,
    trace_block, trace_payload, Sample, SampleGuard,
};
```

## xi-editor-ph7/rust/core-lib/src/view.rs

```rust
use std::cell::RefCell;
use std::cmp::{max, min};
use std::iter;
use std::ops::Range;

use serde_json::Value;

use crate::annotations::{AnnotationStore, Annotations, ToAnnotation};
use crate::client::{Client, Update, UpdateOp};
use crate::edit_types::ViewEvent;
use crate::find::{Find, FindStatus};
use crate::line_cache_shadow::{self, LineCacheShadow, RenderPlan, RenderTactic};
use crate::line_offset::LineOffset;
use crate::linewrap::{InvalLines, Lines, VisualLine, WrapWidth};
use crate::movement::{region_movement, selection_movement, Movement};
use crate::plugins::PluginId;
use crate::rpc::{FindQuery, GestureType, MouseAction, SelectionGranularity, SelectionModifier};
use crate::selection::{Affinity, InsertDrift, SelRegion, Selection};
use crate::styles::{Style, ThemeStyleMap};
use crate::tabs::{BufferId, Counter, ViewId};
use crate::trace::trace_block;
use crate::width_cache::WidthCache;
use crate::word_boundaries::WordCursor;
use xi_rope::spans::Spans;
use xi_rope::{Cursor, Interval, LinesMetric, Rope, RopeDelta};

type StyleMap = RefCell<ThemeStyleMap>;

/// A flag used to indicate when legacy actions should modify selections
const FLAG_SELECT: u64 = 2;

/// Size of batches as number of bytes used during incremental find.
const FIND_BATCH_SIZE: usize = 500000;

/// A view to a buffer. It is the buffer plus additional information
/// like line breaks and selection state.
pub struct View {
    view_id: ViewId,
    buffer_id: BufferId,

    /// Tracks whether this view has been scheduled to render.
    /// We attempt to reduce duplicate renders by setting a small timeout
    /// after an edit is applied, to allow batching with any plugin updates.
    pending_render: bool,
    size: Size,
    /// The selection state for this view. Invariant: non-empty.
    selection: Selection,

    drag_state: Option<DragState>,

    /// vertical scroll position
    first_line: usize,
    /// height of visible portion
    height: usize,
    lines: Lines,

    /// Front end's line cache state for this view. See the `LineCacheShadow`
    /// description for the invariant.
    lc_shadow: LineCacheShadow,

    /// New offset to be scrolled into position after an edit.
    scroll_to: Option<usize>,

    /// The state for finding text for this view.
    /// Each instance represents a separate search query.
    find: Vec<Find>,

    /// Tracks the IDs for additional search queries in find.
    find_id_counter: Counter,

    /// Tracks whether there has been changes in find results or find parameters.
    /// This is used to determined whether FindStatus should be sent to the frontend.
    find_changed: FindStatusChange,

    /// Tracks the progress of incremental find.
    find_progress: FindProgress,

    /// Tracks whether find highlights should be rendered.
    /// Highlights are only rendered when search dialog is open.
    highlight_find: bool,

    /// The state for replacing matches for this view.
    replace: Option<Replace>,

    /// Tracks whether the replacement string or replace parameters changed.
    replace_changed: bool,

    /// Annotations provided by plugins.
    annotations: AnnotationStore,
}

/// Indicates what changed in the find state.
#[derive(PartialEq, Debug)]
enum FindStatusChange {
    /// None of the find parameters or number of matches changed.
    None,

    /// Find parameters and number of matches changed.
    All,

    /// Only number of matches changed
    Matches,
}

/// Indicates what changed in the find state.
#[derive(PartialEq, Debug, Clone)]
enum FindProgress {
    /// Incremental find is done/not running.
    Ready,

    /// The find process just started.
    Started,

    /// Incremental find is in progress. Keeps tracked of already searched range.
    InProgress(Range<usize>),
}

/// Contains replacement string and replace options.
#[derive(Debug, Default, PartialEq, Serialize, Deserialize, Clone)]
pub struct Replace {
    /// Replacement string.
    pub chars: String,
    pub preserve_case: bool,
}

/// A size, in pixel units (not display pixels).
#[derive(Debug, Default, PartialEq, Serialize, Deserialize, Clone)]
pub struct Size {
    pub width: f64,
    pub height: f64,
}

/// State required to resolve a drag gesture into a selection.
struct DragState {
    /// All the selection regions other than the one being dragged.
    base_sel: Selection,

    /// Start of the region selected when drag was started (region is
    /// assumed to be forward).
    min: usize,

    /// End of the region selected when drag was started.
    max: usize,

    granularity: SelectionGranularity,
}

impl View {
    pub fn new(view_id: ViewId, buffer_id: BufferId) -> View {...}

    pub(crate) fn get_buffer_id(&self) -> BufferId {...}

    pub(crate) fn get_view_id(&self) -> ViewId {...}

    pub(crate) fn get_lines(&self) -> &Lines {...}

    pub(crate) fn get_replace(&self) -> Option<Replace> {...}

    pub(crate) fn set_has_pending_render(&mut self, pending: bool) {...}

    pub(crate) fn has_pending_render(&self) -> bool {...}

    pub(crate) fn update_wrap_settings(&mut self, text: &Rope, wrap_cols: usize, word_wrap: bool) {...}

    pub(crate) fn needs_more_wrap(&self) -> bool {...}

    pub(crate) fn needs_wrap_in_visible_region(&self, text: &Rope) -> bool {...}

    pub(crate) fn find_in_progress(&self) -> bool {...}

    pub(crate) fn do_edit(&mut self, text: &Rope, cmd: ViewEvent) {...}

    fn do_gesture(&mut self, text: &Rope, line: u64, col: u64, ty: GestureType) {...}

    fn goto_line(&mut self, text: &Rope, line: u64) {...}

    pub fn set_size(&mut self, size: Size) {...}

    pub fn set_scroll(&mut self, first: i64, last: i64) {...}

    pub fn scroll_height(&self) -> usize {...}

    fn scroll_to_cursor(&mut self, text: &Rope) {...}

    /// Removes any selection present at the given offset.
    /// Returns true if a selection was removed, false otherwise.
    pub fn deselect_at_offset(&mut self, text: &Rope, offset: usize) -> bool {...}

    /// Move the selection by the given movement. Return value is the offset of
    /// a point that should be scrolled into view.
    ///
    /// If `modify` is `true`, the selections are modified, otherwise the results
    /// of individual region movements become carets.
    pub fn do_move(&mut self, text: &Rope, movement: Movement, modify: bool) {...}

    /// Set the selection to a new value.
    pub fn set_selection<S: Into<Selection>>(&mut self, text: &Rope, sel: S) {...}

    /// Sets the selection to a new value, without invalidating.
    fn set_selection_for_edit(&mut self, text: &Rope, sel: Selection) {...}

    /// Sets the selection to a new value, invalidating the line cache as needed.
    /// This function does not perform any scrolling.
    fn set_selection_raw(&mut self, text: &Rope, sel: Selection) {...}

    /// Invalidate the current selection. Note that we could be even more
    /// fine-grained in the case of multiple cursors, but we also want this
    /// method to be fast even when the selection is large.
    fn invalidate_selection(&mut self, text: &Rope) {...}

    fn add_selection_by_movement(&mut self, text: &Rope, movement: Movement) {...}

    // TODO: insert from keyboard or input method shouldn't break undo group,
    /// Invalidates the styles of the given range (start and end are offsets within
    /// the text).
    pub fn invalidate_styles(&mut self, text: &Rope, start: usize, end: usize) {...}

    pub fn update_annotations(
        &mut self,
        plugin: PluginId,
        interval: Interval,
        annotations: Annotations,
    ) {...}

    /// Select entire buffer.
    ///
    /// Note: unlike movement based selection, this does not scroll.
    pub fn select_all(&mut self, text: &Rope) {...}

    /// Finds the unit of text containing the given offset.
    fn unit(&self, text: &Rope, offset: usize, granularity: SelectionGranularity) -> Interval {...}

    /// Selects text with a certain granularity and supports multi_selection
    fn select(
        &mut self,
        text: &Rope,
        offset: usize,
        granularity: SelectionGranularity,
        multi: bool,
    ) {...}

    /// Extends an existing selection (eg. when the user performs SHIFT + click).
    pub fn extend_selection(
        &mut self,
        text: &Rope,
        offset: usize,
        granularity: SelectionGranularity,
    ) {...}

    /// Splits current selections into lines.
    fn do_split_selection_into_lines(&mut self, text: &Rope) {...}

    /// Does a drag gesture, setting the selection from a combination of the drag
    /// state and new offset.
    fn do_drag(&mut self, text: &Rope, offset: usize, affinity: Affinity) {...}

    /// Creates a `SelRegion` for range select or drag operations.
    pub fn range_region(
        &self,
        text: &Rope,
        start: (usize, usize),
        offset: usize,
        granularity: SelectionGranularity,
    ) -> SelRegion {...}

    /// Returns the regions of the current selection.
    pub fn sel_regions(&self) -> &[SelRegion] {...}

    /// Collapse all selections in this view into a single caret
    pub fn collapse_selections(&mut self, text: &Rope) {...}

    /// Determines whether the offset is in any selection (counting carets and
    /// selection edges).
    pub fn is_point_in_selection(&self, offset: usize) -> bool {...}

    // Encode a single line with its styles and cursors in JSON.
    // If "text" is not specified, don't add "text" to the output.
    // If "style_spans" are not specified, don't add "styles" to the output.
    fn encode_line(
        &self,
        client: &Client,
        styles: &StyleMap,
        line: VisualLine,
        text: Option<&Rope>,
        style_spans: Option<&Spans<Style>>,
        last_pos: usize,
    ) -> Value {...}

    pub fn encode_styles(
        &self,
        client: &Client,
        styles: &StyleMap,
        start: usize,
        end: usize,
        sel: &[(usize, usize)],
        hls: &Vec<Vec<(usize, usize)>>,
        style_spans: &Spans<Style>,
    ) -> Vec<isize> {...}

    fn get_or_def_style_id(&self, client: &Client, style_map: &StyleMap, style: &Style) -> usize {...}

    fn send_update_for_plan(
        &mut self,
        text: &Rope,
        client: &Client,
        styles: &StyleMap,
        style_spans: &Spans<Style>,
        plan: &RenderPlan,
        pristine: bool,
    ) {...}

    /// Determines the current number of find results and search parameters to send them to
    /// the frontend.
    pub fn find_status(&self, text: &Rope, matches_only: bool) -> Vec<FindStatus> {...}

    /// Update front-end with any changes to view since the last time sent.
    /// The `pristine` argument indicates whether or not the buffer has
    /// unsaved changes.
    pub fn render_if_dirty(
        &mut self,
        text: &Rope,
        client: &Client,
        styles: &StyleMap,
        style_spans: &Spans<Style>,
        pristine: bool,
    ) {...}

    // Send the requested lines even if they're outside the current scroll region.
    pub fn request_lines(
        &mut self,
        text: &Rope,
        client: &Client,
        styles: &StyleMap,
        style_spans: &Spans<Style>,
        first_line: usize,
        last_line: usize,
        pristine: bool,
    ) {...}

    /// Invalidates front-end's entire line cache, forcing a full render at the next
    /// update cycle. This should be a last resort, updates should generally cause
    /// finer grain invalidation.
    pub fn set_dirty(&mut self, text: &Rope) {...}

    /// Returns the byte range of the currently visible lines.
    fn interval_of_visible_region(&self, text: &Rope) -> Interval {...}

    /// Generate line breaks, based on current settings. Currently batch-mode,
    /// and currently in a debugging state.
    pub(crate) fn rewrap(
        &mut self,
        text: &Rope,
        width_cache: &mut WidthCache,
        client: &Client,
        spans: &Spans<Style>,
    ) {...}

    /// Updates the view after the text has been modified by the given `delta`.
    /// This method is responsible for updating the cursors, and also for
    /// recomputing line wraps.
    pub fn after_edit(
        &mut self,
        text: &Rope,
        last_text: &Rope,
        delta: &RopeDelta,
        client: &Client,
        width_cache: &mut WidthCache,
        drift: InsertDrift,
    ) {...}

    fn do_selection_for_find(&mut self, text: &Rope, case_sensitive: bool) {...}

    fn add_find(&mut self) {...}

    fn set_find(&mut self, text: &Rope, queries: Vec<FindQuery>) {...}

    pub fn do_find(&mut self, text: &Rope) {...}

    /// Selects the next find match.
    pub fn do_find_next(
        &mut self,
        text: &Rope,
        reverse: bool,
        wrap: bool,
        allow_same: bool,
        modify_selection: &SelectionModifier,
    ) {...}

    /// Selects all find matches.
    pub fn do_find_all(&mut self, text: &Rope) {...}

    /// Select the next occurrence relative to the last cursor. `reverse` determines whether the
    /// next occurrence before (`true`) or after (`false`) the last cursor is selected. `wrapped`
    /// indicates a search for the next occurrence past the end of the file.
    pub fn select_next_occurrence(
        &mut self,
        text: &Rope,
        reverse: bool,
        wrapped: bool,
        _allow_same: bool,
        modify_selection: &SelectionModifier,
    ) {...}

    fn do_set_replace(&mut self, chars: String, preserve_case: bool) {...}

    fn do_selection_for_replace(&mut self, text: &Rope) {...}

    pub fn get_caret_offset(&self) -> Option<usize> {...}
}

impl View {
    /// Exposed for benchmarking
    #[doc(hidden)]
    pub fn debug_force_rewrap_cols(&mut self, text: &Rope, cols: usize) {...}
}

impl LineOffset for View {
    fn offset_of_line(&self, text: &Rope, line: usize) -> usize {...}

    fn line_of_offset(&self, text: &Rope, offset: usize) -> usize {...}
}

// utility function to clamp a value within the given range
fn clamp(x: usize, min: usize, max: usize) -> usize {...}
```

## xi-editor-ph7/rust/core-lib/src/watcher.rs

```rust
use crossbeam_channel::unbounded;
use notify::{event::*, watcher, RecommendedWatcher, RecursiveMode, Watcher};
use std::collections::VecDeque;
use std::fmt;
use std::mem;
use std::path::{Path, PathBuf};
use std::sync::{Arc, Mutex};
use std::thread;
use std::time::Duration;

use xi_rpc::RpcPeer;

/// Delay for aggregating related file system events.
pub const DEBOUNCE_WAIT_MILLIS: u64 = 50;

/// Wrapper around a `notify::Watcher`. It runs the inner watcher
/// in a separate thread, and communicates with it via a [crossbeam channel].
/// [crossbeam channel]: https://docs.rs/crossbeam-channel
pub struct FileWatcher {
    inner: RecommendedWatcher,
    state: Arc<Mutex<WatcherState>>,
}

#[derive(Debug, Default)]
struct WatcherState {
    events: EventQueue,
    watchees: Vec<Watchee>,
}

/// Tracks a registered 'that-which-is-watched'.
#[doc(hidden)]
struct Watchee {
    path: PathBuf,
    recursive: bool,
    token: WatchToken,
    filter: Option<Box<PathFilter>>,
}

/// Token provided to `FileWatcher`, to associate events with
/// interested parties.
///
/// Note: `WatchToken`s are assumed to correspond with an
/// 'area of interest'; that is, they are used to route delivery
/// of events.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct WatchToken(pub usize);

/// A trait for types which can be notified of new events.
/// New events are accessible through the `FileWatcher` instance.
pub trait Notify: Send {
    fn notify(&self);
}

pub type EventQueue = VecDeque<(WatchToken, Event)>;

pub type PathFilter = dyn Fn(&Path) -> bool + Send + 'static;

impl FileWatcher {
    pub fn new<T: Notify + 'static>(peer: T) -> Self {...}

    /// Begin watching `path`. As `Event`s (documented in the
    /// [notify](https://docs.rs/notify) crate) arrive, they are stored
    /// with the associated `token` and a task is added to the runloop's
    /// idle queue.
    ///
    /// Delivery of events then requires that the runloop's handler
    /// correctly forward the `handle_idle` call to the interested party.
    pub fn watch(&mut self, path: &Path, recursive: bool, token: WatchToken) {...}

    /// Like `watch`, but taking a predicate function that filters delivery
    /// of events based on their path.
    pub fn watch_filtered<F>(&mut self, path: &Path, recursive: bool, token: WatchToken, filter: F)
    where
        F: Fn(&Path) -> bool + Send + 'static,
    {...}

    fn watch_impl(
        &mut self,
        path: &Path,
        recursive: bool,
        token: WatchToken,
        filter: Option<Box<PathFilter>>,
    ) {...}

    /// Removes the provided token/path pair from the watch list.
    /// Does not stop watching this path, if it is associated with
    /// other tokens.
    pub fn unwatch(&mut self, path: &Path, token: WatchToken) {...}

    /// Takes ownership of this `Watcher`'s current event queue.
    pub fn take_events(&mut self) -> VecDeque<(WatchToken, Event)> {...}
}

impl Watchee {
    fn wants_event(&self, event: &Event) -> bool {...}

    fn applies_to_path(&self, path: &Path) -> bool {...}
}

impl Notify for RpcPeer {
    fn notify(&self) {...}
}

impl fmt::Debug for Watchee {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {...}
}

fn mode_from_bool(is_recursive: bool) -> RecursiveMode {...}
```

## xi-editor-ph7/rust/core-lib/src/whitespace.rs

```rust
extern crate xi_rope;

use std::collections::BTreeMap;
use xi_rope::Rope;

/// An enumeration of legal indentation types.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum Indentation {
    Tabs,
    Spaces(usize),
}

/// A struct representing the mixed indentation error.
#[derive(Debug)]
pub struct MixedIndentError;

impl Indentation {
    /// Parses a rope for indentation settings.
    pub fn parse(rope: &Rope) -> Result<Option<Self>, MixedIndentError> {...}

    /// Detects the indentation on a specific line.
    /// Parses whitespace until first occurrence of something else
    pub fn parse_line(line: &str) -> Result<Option<Self>, MixedIndentError> {...}
}

/// Uses a heuristic to calculate the greatest common denominator of most used indentation depths.
///
/// As BTreeMaps are ordered by value, using take on the iterator ensures the indentation levels
/// most frequently used in the file are extracted.
fn extract_count(spaces: BTreeMap<usize, usize>) -> usize {...}

/// Simple implementation to calculate greatest common divisor, based on Euclid's algorithm
fn gcd(a: usize, b: usize) -> usize {...}
```

## xi-editor-ph7/rust/core-lib/src/width_cache.rs

```rust
use std::borrow::Cow;
use std::collections::{BTreeMap, HashMap};

use crate::client::Client;

/// A token which can be used to retrieve an actual width value when the
/// batch request is submitted.
///
/// Internally, it is implemented as an index into the `widths` array.
pub type Token = usize;

/// A measured width, in px units.
type Width = f64;

type StyleId = usize;

pub struct WidthCache {
    /// maps cache key to index within widths
    m: HashMap<WidthCacheKey<'static>, Token>,
    widths: Vec<Width>,
}

#[derive(Eq, PartialEq, Hash)]
struct WidthCacheKey<'a> {
    id: StyleId,
    s: Cow<'a, str>,
}

/// A batched request, so that a number of strings can be measured in a
/// a single RPC.
pub struct WidthBatchReq<'a> {
    cache: &'a mut WidthCache,
    pending_tok: Token,
    req: Vec<WidthReq>,
    req_toks: Vec<Vec<Token>>,
    // maps style id to index into req/req_toks
    req_ids: BTreeMap<StyleId, Token>,
}

/// A request for measuring the widths of strings all of the same style
/// (a request from core to front-end).
#[derive(Serialize, Deserialize)]
pub struct WidthReq {
    pub id: StyleId,
    pub strings: Vec<String>,
}

/// The response for a batch of [`WidthReq`]s.
pub type WidthResponse = Vec<Vec<Width>>;

/// A trait for types that provide width measurement. In the general case this
/// will be provided by the frontend, but alternative implementations might
/// be provided for faster measurement of 'fixed-width' fonts, or for testing.
pub trait WidthMeasure {
    fn measure_width(&self, request: &[WidthReq]) -> Result<WidthResponse, xi_rpc::Error>;
}

impl WidthMeasure for Client {
    fn measure_width(&self, request: &[WidthReq]) -> Result<WidthResponse, xi_rpc::Error> {...}
}

/// A measure in which each codepoint has width of 1.
pub struct CodepointMono;

impl WidthMeasure for CodepointMono {
    /// In which each codepoint has width == 1.
    fn measure_width(&self, request: &[WidthReq]) -> Result<WidthResponse, xi_rpc::Error> {...}
}

impl WidthCache {
    pub fn new() -> WidthCache {...}

    /// Returns the number of items currently in the cache.
    pub(crate) fn len(&self) -> usize {...}

    /// Resolve a previously obtained token into a width value.
    pub fn resolve(&self, tok: Token) -> Width {...}

    /// Create a new batch of requests.
    pub fn batch_req(self: &mut WidthCache) -> WidthBatchReq<'_> {...}
}

impl<'a> WidthBatchReq<'a> {
    /// Request measurement of one string/style pair within the batch.
    pub fn request(&mut self, id: StyleId, s: &str) -> Token {...}

    /// Resolves pending measurements to concrete widths using the provided [`WidthMeasure`].
    /// On success, the tokens given by `request` will resolve in the cache.
    pub fn resolve_pending<T: WidthMeasure + ?Sized>(
        &mut self,
        handler: &T,
    ) -> Result<(), xi_rpc::Error> {...}
}
```

## xi-editor-ph7/rust/core-lib/src/word_boundaries.rs

```rust
use xi_rope::{Cursor, Rope, RopeInfo};

pub struct WordCursor<'a> {
    inner: Cursor<'a, RopeInfo, String>,
}

impl<'a> WordCursor<'a> {
    pub fn new(text: &'a Rope, pos: usize) -> WordCursor<'a> {...}

    /// Get previous boundary, and set the cursor at the boundary found.
    pub fn prev_boundary(&mut self) -> Option<usize> {...}

    /// Get next boundary, and set the cursor at the boundary found.
    pub fn next_boundary(&mut self) -> Option<usize> {...}

    /// Return the selection for the word containing the current cursor. The
    /// cursor is moved to the end of that selection.
    pub fn select_word(&mut self) -> (usize, usize) {...}
}

#[derive(PartialEq, Eq)]
enum WordBoundary {
    Interior,
    Start, // a boundary indicating the end of a word
    End,   // a boundary indicating the start of a word
    Both,
}

impl WordBoundary {
    fn is_start(&self) -> bool {...}

    fn is_end(&self) -> bool {...}

    fn is_boundary(&self) -> bool {...}
}

fn classify_boundary(prev: WordProperty, next: WordProperty) -> WordBoundary {...}

fn classify_boundary_initial(prev: WordProperty, next: WordProperty) -> WordBoundary {...}

#[derive(Copy, Clone)]
enum WordProperty {
    Lf,
    Space,
    Punctuation,
    Other, // includes letters and all of non-ascii unicode
}

fn get_word_property(codepoint: char) -> WordProperty {...}
```

## xi-editor-ph7/rust/core-lib/tests/rpc.rs

```rust
#[macro_use]
extern crate serde_json;

extern crate xi_core_lib;
extern crate xi_rpc;

use std::io;

use xi_core_lib::test_helpers;
use xi_core_lib::XiCore;
use xi_rpc::test_utils::{make_reader, test_channel};
use xi_rpc::{ReadError, RpcLoop};

const MOVEMENT_RPCS: &str = r#"{"method":"edit","params":{"view_id":"view-id-1","method":"move_up","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_down","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_up_and_modify_selection","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_down_and_modify_selection","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_left","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_backward","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_right","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_forward","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_left_and_modify_selection","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_right_and_modify_selection","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_word_left","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_word_right","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_word_left_and_modify_selection","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_word_right_and_modify_selection","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_to_beginning_of_paragraph","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_to_end_of_paragraph","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_to_left_end_of_line","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_to_left_end_of_line_and_modify_selection","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_to_right_end_of_line","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_to_right_end_of_line_and_modify_selection","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_to_beginning_of_document","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_to_beginning_of_document_and_modify_selection","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_to_end_of_document","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"move_to_end_of_document_and_modify_selection","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"scroll_page_up","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"scroll_page_down","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"page_up_and_modify_selection","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"page_down_and_modify_selection","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"select_all","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"add_selection_above","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"add_selection_below","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"collapse_selections","params":[]}}"#;

const TEXT_EDIT_RPCS: &str = r#"{"method":"edit","params":{"view_id":"view-id-1","method":"insert","params":{"chars":"a"}}}
{"method":"edit","params":{"view_id":"view-id-1","method":"delete_backward","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"delete_forward","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"delete_word_forward","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"delete_word_backward","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"delete_to_end_of_paragraph","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"insert_newline","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"insert_tab","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"yank","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"undo","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"redo","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"transpose","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"uppercase","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"lowercase","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"indent","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"outdent","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"duplicate_line","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"replace_next","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"replace_all","params":[]}}
{"id":2,"method":"edit","params":{"view_id":"view-id-1","method":"cut","params":[]}}"#;

const OTHER_EDIT_RPCS: &str = r#"{"method":"edit","params":{"view_id":"view-id-1","method":"scroll","params":[0,1]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"goto_line","params":{"line":1}}}
{"method":"edit","params":{"view_id":"view-id-1","method":"request_lines","params":[0,1]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"drag","params":[17,15,0]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"gesture","params":{"line": 1, "col": 2, "ty": "toggle_sel"}}}
{"method":"edit","params":{"view_id":"view-id-1","method":"gesture","params":{"line": 1, "col": 2, "ty": "point_select"}}}
{"method":"edit","params":{"view_id":"view-id-1","method":"gesture","params":{"line": 1, "col": 2, "ty": "range_select"}}}
{"method":"edit","params":{"view_id":"view-id-1","method":"gesture","params":{"line": 1, "col": 2, "ty": "line_select"}}}
{"method":"edit","params":{"view_id":"view-id-1","method":"gesture","params":{"line": 1, "col": 2, "ty": "word_select"}}}
{"method":"edit","params":{"view_id":"view-id-1","method":"gesture","params":{"line": 1, "col": 2, "ty": "multi_line_select"}}}
{"method":"edit","params":{"view_id":"view-id-1","method":"gesture","params":{"line": 1, "col": 2, "ty": "multi_word_select"}}}
{"method":"edit","params":{"view_id":"view-id-1","method":"find","params":{"case_sensitive":false,"chars":"m"}}}
{"method":"edit","params":{"view_id":"view-id-1","method":"multi_find","params":{"queries": [{"case_sensitive":false,"chars":"m"}]}}}
{"method":"edit","params":{"view_id":"view-id-1","method":"find_next","params":{"wrap_around":true}}}
{"method":"edit","params":{"view_id":"view-id-1","method":"find_previous","params":{"wrap_around":true}}}
{"method":"edit","params":{"view_id":"view-id-1","method":"find_all","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"highlight_find","params":{"visible":true}}}
{"method":"edit","params":{"view_id":"view-id-1","method":"selection_for_find","params":{"case_sensitive":true}}}
{"method":"edit","params":{"view_id":"view-id-1","method":"replace","params":{"chars":"a"}}}
{"method":"edit","params":{"view_id":"view-id-1","method":"selection_for_replace","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"debug_rewrap","params":[]}}
{"method":"edit","params":{"view_id":"view-id-1","method":"debug_print_spans","params":[]}}
{"id":3,"method":"edit","params":{"view_id":"view-id-1","method":"copy","params":[]}}"#;
```

