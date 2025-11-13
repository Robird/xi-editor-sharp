## xi-editor-ph7/rust/rpc/examples/try_chan.rs

```rust
extern crate xi_rpc;

use std::sync::mpsc;
use std::thread;

/*
use xi_rpc::chan::Chan;

pub fn test_chan() {
    let n_iter = 1000000;
    let chan1 = Chan::new();
    let chan1s = chan1.clone();
    let chan2 = Chan::new();
    let chan2s = chan2.clone();
    let thread1 = thread::spawn(move|| {
        for _ in 0..n_iter {
            chan2s.try_send(chan1.recv());
        }
    });
    let thread2 = thread::spawn(move|| {
        for _ in 0..n_iter {
            chan1s.try_send(42);
            let _ = chan2.recv();
        }
    });
    let _ = thread1.join();
    let _ = thread2.join();
}
*/

pub fn test_mpsc() {...}

pub fn main() {...}
```

## xi-editor-ph7/rust/rpc/src/error.rs

```rust
use std::fmt;
use std::io;

use serde::de::{Deserialize, Deserializer};
use serde::ser::{Serialize, Serializer};
use serde_json::{Error as JsonError, Value};

/// The possible error outcomes when attempting to send a message.
#[derive(Debug)]
pub enum Error {
    /// An IO error occurred on the underlying communication channel.
    Io(io::Error),
    /// The peer returned an error.
    RemoteError(RemoteError),
    /// The peer closed the connection.
    PeerDisconnect,
    /// The peer sent a response containing the id, but was malformed.
    InvalidResponse,
}

/// The possible error outcomes when attempting to read a message.
#[derive(Debug)]
pub enum ReadError {
    /// An error occurred in the underlying stream
    Io(io::Error),
    /// The message was not valid JSON.
    Json(JsonError),
    /// The message was not a JSON object.
    NotObject,
    /// The the method and params were not recognized by the handler.
    UnknownRequest(JsonError),
    /// The peer closed the connection.
    Disconnect,
}

/// Errors that can be received from the other side of the RPC channel.
///
/// This type is intended to go over the wire. And by convention
/// should `Serialize` as a JSON object with "code", "message",
/// and optionally "data" fields.
///
/// The xi RPC protocol defines one error: `RemoteError::InvalidRequest`,
/// represented by error code `-32600`; however codes in the range
/// `-32700 ... -32000` (inclusive) are reserved for compatability with
/// the JSON-RPC spec.
///
/// # Examples
///
/// An invalid request:
///
/// ```
/// # extern crate xi_rpc;
/// # extern crate serde_json;
/// use xi_rpc::RemoteError;
/// use serde_json::Value;
///
/// let json = r#"{
///     "code": -32600,
///     "message": "Invalid request",
///     "data": "Additional details"
///     }"#;
///
/// let err = serde_json::from_str::<RemoteError>(&json).unwrap();
/// assert_eq!(err,
///            RemoteError::InvalidRequest(
///                Some(Value::String("Additional details".into()))));
/// ```
///
/// A custom error:
///
/// ```
/// # extern crate xi_rpc;
/// # extern crate serde_json;
/// use xi_rpc::RemoteError;
/// use serde_json::Value;
///
/// let json = r#"{
///     "code": 404,
///     "message": "Not Found"
///     }"#;
///
/// let err = serde_json::from_str::<RemoteError>(&json).unwrap();
/// assert_eq!(err, RemoteError::custom(404, "Not Found", None));
/// ```
#[derive(Debug, Clone, PartialEq)]
pub enum RemoteError {
    /// The JSON was valid, but was not a correctly formed request.
    ///
    /// This Error is used internally, and should not be returned by
    /// clients.
    InvalidRequest(Option<Value>),
    /// A custom error, defined by the client.
    Custom { code: i64, message: String, data: Option<Value> },
    /// An error that cannot be represented by an error object.
    ///
    /// This error is intended to accommodate clients that return arbitrary
    /// error values. It should not be used for new errors.
    Unknown(Value),
}

impl RemoteError {
    /// Creates a new custom error.
    pub fn custom<S, V>(code: i64, message: S, data: V) -> Self
    where
        S: AsRef<str>,
        V: Into<Option<Value>>,
    {...}
}

impl ReadError {
    /// Returns `true` iff this is the `ReadError::Disconnect` variant.
    pub fn is_disconnect(&self) -> bool {...}
}

impl fmt::Display for ReadError {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {...}
}

impl From<JsonError> for ReadError {
    fn from(err: JsonError) -> ReadError {...}
}

impl From<io::Error> for ReadError {
    fn from(err: io::Error) -> ReadError {...}
}

impl From<JsonError> for RemoteError {
    fn from(err: JsonError) -> RemoteError {...}
}

impl From<RemoteError> for Error {
    fn from(err: RemoteError) -> Error {...}
}

#[derive(Deserialize, Serialize)]
struct ErrorHelper {
    code: i64,
    message: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    data: Option<Value>,
}

impl<'de> Deserialize<'de> for RemoteError {
    fn deserialize<D>(deserializer: D) -> Result<Self, D::Error>
    where
        D: Deserializer<'de>,
    {...}
}

impl Serialize for RemoteError {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {...}
}
```

## xi-editor-ph7/rust/rpc/src/lib.rs

```rust
#![allow(clippy::boxed_local, clippy::or_fun_call)]

#[macro_use]
extern crate serde_json;
#[macro_use]
extern crate serde_derive;
extern crate crossbeam_utils;
extern crate serde;
#[cfg(feature = "trace")]
extern crate xi_trace;

#[macro_use]
extern crate log;

mod error;
mod parse;
pub mod trace;

pub mod test_utils;

use std::cmp;
use std::collections::{BTreeMap, BinaryHeap, VecDeque};
use std::io::{self, BufRead, Write};
use std::sync::atomic::{AtomicBool, AtomicUsize, Ordering};
use std::sync::mpsc;
use std::sync::{Arc, Condvar, Mutex};
use std::thread;
use std::time::{Duration, Instant};

use serde::de::DeserializeOwned;
use serde_json::Value;

use crate::trace::{trace, trace_block, trace_block_payload, trace_payload};

pub use crate::error::{Error, ReadError, RemoteError};
use crate::parse::{Call, MessageReader, Response, RpcObject};

/// The maximum duration we will block on a reader before checking for an task.
const MAX_IDLE_WAIT: Duration = Duration::from_millis(5);

/// An interface to access the other side of the RPC channel. The main purpose
/// is to send RPC requests and notifications to the peer.
///
/// A single shared `RawPeer` exists for each `RpcLoop`; a reference can
/// be taken with `RpcLoop::get_peer()`.
///
/// In general, `RawPeer` shouldn't be used directly, but behind a pointer as
/// the `Peer` trait object.
pub struct RawPeer<W: Write + 'static>(Arc<RpcState<W>>);

/// The `Peer` trait represents the interface for the other side of the RPC
/// channel. It is intended to be used behind a pointer, a trait object.
pub trait Peer: Send + 'static {
    /// Used to implement `clone` in an object-safe way.
    /// For an explanation on this approach, see
    /// [this thread](https://users.rust-lang.org/t/solved-is-it-possible-to-clone-a-boxed-trait-object/1714/6).
    fn box_clone(&self) -> Box<dyn Peer>;
    /// Sends a notification (asynchronous RPC) to the peer.
    fn send_rpc_notification(&self, method: &str, params: &Value);
    /// Sends a request asynchronously, and the supplied callback will
    /// be called when the response arrives.
    ///
    /// `Callback` is an alias for `FnOnce(Result<Value, Error>)`; it must
    /// be boxed because trait objects cannot use generic paramaters.
    fn send_rpc_request_async(&self, method: &str, params: &Value, f: Box<dyn Callback>);
    /// Sends a request (synchronous RPC) to the peer, and waits for the result.
    fn send_rpc_request(&self, method: &str, params: &Value) -> Result<Value, Error>;
    /// Determines whether an incoming request (or notification) is
    /// pending. This is intended to reduce latency for bulk operations
    /// done in the background.
    fn request_is_pending(&self) -> bool;
    /// Adds a token to the idle queue. When the runloop is idle and the
    /// queue is not empty, the handler's `idle` fn will be called
    /// with the earliest added token.
    fn schedule_idle(&self, token: usize);
    /// Like `schedule_idle`, with the guarantee that the handler's `idle`
    /// fn will not be called _before_ the provided `Instant`.
    ///
    /// # Note
    ///
    /// This is not intended as a high-fidelity timer. Regular RPC messages
    /// will always take priority over an idle task.
    fn schedule_timer(&self, after: Instant, token: usize);
}

/// The `Peer` trait object.
pub type RpcPeer = Box<dyn Peer>;

pub struct RpcCtx {
    peer: RpcPeer,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
/// An RPC command.
///
/// This type is used as a placeholder in various places, and can be
/// used by clients as a catchall type for implementing `MethodHandler`.
pub struct RpcCall {
    pub method: String,
    pub params: Value,
}

/// A trait for types which can handle RPCs.
///
/// Types which implement `MethodHandler` are also responsible for implementing
/// `Parser`; `Parser` is provided when Self::Notification and Self::Request
/// can be used with serde::DeserializeOwned.
pub trait Handler {
    type Notification: DeserializeOwned;
    type Request: DeserializeOwned;
    fn handle_notification(&mut self, ctx: &RpcCtx, rpc: Self::Notification);
    fn handle_request(&mut self, ctx: &RpcCtx, rpc: Self::Request) -> Result<Value, RemoteError>;
    #[allow(unused_variables)]
    fn idle(&mut self, ctx: &RpcCtx, token: usize) {...}
}

pub trait Callback: Send {
    fn call(self: Box<Self>, result: Result<Value, Error>);
}

impl<F: Send + FnOnce(Result<Value, Error>)> Callback for F {
    fn call(self: Box<F>, result: Result<Value, Error>) {...}
}

/// A helper type which shuts down the runloop if a panic occurs while
/// handling an RPC.
struct PanicGuard<'a, W: Write + 'static>(&'a RawPeer<W>);

impl<'a, W: Write + 'static> Drop for PanicGuard<'a, W> {
    fn drop(&mut self) {...}
}

// IdleProc trait removed: The RPC idle queue uses tokens (usize), not boxed
// closures, so a dedicated trait was unused and only raised warning noise.

enum ResponseHandler {
    Chan(mpsc::Sender<Result<Value, Error>>),
    Callback(Box<dyn Callback>),
}

impl ResponseHandler {
    fn invoke(self, result: Result<Value, Error>) {...}
}

#[derive(Debug, PartialEq, Eq)]
struct Timer {
    fire_after: Instant,
    token: usize,
}

struct RpcState<W: Write> {
    rx_queue: Mutex<VecDeque<Result<RpcObject, ReadError>>>,
    rx_cvar: Condvar,
    writer: Mutex<W>,
    id: AtomicUsize,
    pending: Mutex<BTreeMap<usize, ResponseHandler>>,
    idle_queue: Mutex<VecDeque<usize>>,
    timers: Mutex<BinaryHeap<Timer>>,
    needs_exit: AtomicBool,
    is_blocked: AtomicBool,
}

/// A structure holding the state of a main loop for handling RPC's.
pub struct RpcLoop<W: Write + 'static> {
    reader: MessageReader,
    peer: RawPeer<W>,
}

impl<W: Write + Send> RpcLoop<W> {
    /// Creates a new `RpcLoop` with the given output stream (which is used for
    /// sending requests and notifications, as well as responses).
    pub fn new(writer: W) -> Self {...}

    /// Gets a reference to the peer.
    pub fn get_raw_peer(&self) -> RawPeer<W> {...}

    /// Starts the event loop, reading lines from the reader until EOF,
    /// or an error occurs.
    ///
    /// Returns `Ok()` in the EOF case, otherwise returns the
    /// underlying `ReadError`.
    ///
    /// # Note:
    /// The reader is supplied via a closure, as basically a workaround
    /// so that the reader doesn't have to be `Send`. Internally, the
    /// main loop starts a separate thread for I/O, and at startup that
    /// thread calls the given closure.
    ///
    /// Calls to the handler happen on the caller's thread.
    ///
    /// Calls to the handler are guaranteed to preserve the order as
    /// they appear on on the channel. At the moment, there is no way
    /// for there to be more than one incoming request to be outstanding.
    pub fn mainloop<R, RF, H>(&mut self, rf: RF, handler: &mut H) -> Result<(), ReadError>
    where
        R: BufRead,
        RF: Send + FnOnce() -> R,
        H: Handler,
    {...}
}

/// Returns the next read result, checking for idle work when no
/// result is available.
fn next_read<W, H>(peer: &RawPeer<W>, handler: &mut H, ctx: &RpcCtx) -> Result<RpcObject, ReadError>
where
    W: Write + Send,
    H: Handler,
{...}

fn do_idle<H: Handler>(handler: &mut H, ctx: &RpcCtx, token: usize) {...}

impl RpcCtx {
    pub fn get_peer(&self) -> &RpcPeer {...}

    /// Schedule the idle handler to be run when there are no requests pending.
    pub fn schedule_idle(&self, token: usize) {...}
}

impl<W: Write + Send + 'static> Peer for RawPeer<W> {
    fn box_clone(&self) -> Box<dyn Peer> {...}

    fn send_rpc_notification(&self, method: &str, params: &Value) {...}

    fn send_rpc_request_async(&self, method: &str, params: &Value, f: Box<dyn Callback>) {...}

    fn send_rpc_request(&self, method: &str, params: &Value) -> Result<Value, Error> {...}

    fn request_is_pending(&self) -> bool {...}

    fn schedule_idle(&self, token: usize) {...}

    fn schedule_timer(&self, after: Instant, token: usize) {...}
}

impl<W: Write> RawPeer<W> {
    fn send(&self, v: &Value) -> Result<(), io::Error> {...}

    fn respond(&self, result: Response, id: u64) {...}

    fn send_rpc_request_common(&self, method: &str, params: &Value, rh: ResponseHandler) {...}

    fn handle_response(&self, id: u64, resp: Result<Value, Error>) {...}

    /// Get a message from the receive queue if available.
    fn try_get_rx(&self) -> Option<Result<RpcObject, ReadError>> {...}

    /// Get a message from the receive queue, waiting for at most `Duration`
    /// and returning `None` if no message is available.
    fn get_rx_timeout(&self, dur: Duration) -> Option<Result<RpcObject, ReadError>> {...}

    /// Adds a message to the receive queue. The message should only
    /// be `None` if the read thread is exiting.
    fn put_rx(&self, json: Result<RpcObject, ReadError>) {...}

    fn try_get_idle(&self) -> Option<usize> {...}

    /// Checks status of the most imminent timer. If that timer has expired,
    /// returns `Some(Ok(_))`, with the corresponding token.
    /// If a timer exists but has not expired, returns `Some(Err(_))`,
    /// with the error value being the `Duration` until the timer is ready.
    /// Returns `None` if no timers are registered.
    fn check_timers(&self) -> Option<Result<usize, Duration>> {...}

    /// send disconnect error to pending requests.
    fn disconnect(&self) {...}

    /// Returns `true` if an error has occured in the main thread.
    fn needs_exit(&self) -> bool {...}

    fn reset_needs_exit(&self) {...}
}

impl Clone for Box<dyn Peer> {
    fn clone(&self) -> Box<dyn Peer> {...}
}

impl<W: Write> Clone for RawPeer<W> {
    fn clone(&self) -> Self {...}
}

//NOTE: for our timers to work with Rust's BinaryHeap we want to reverse
//the default comparison; smaller `Instant`'s are considered 'greater'.
impl Ord for Timer {
    fn cmp(&self, other: &Timer) -> cmp::Ordering {...}
}

impl PartialOrd for Timer {
    fn partial_cmp(&self, other: &Timer) -> Option<cmp::Ordering> {...}
}
```

## xi-editor-ph7/rust/rpc/src/parse.rs

```rust
use std::io::BufRead;

use serde::de::DeserializeOwned;
use serde_json::{Error as JsonError, Value};

use crate::error::{ReadError, RemoteError};
use crate::trace::trace_block;

/// A unique identifier attached to request RPCs.
type RequestId = u64;

/// An RPC response, received from the peer.
pub type Response = Result<Value, RemoteError>;

/// Reads and parses RPC messages from a stream, maintaining an
/// internal buffer.
#[derive(Debug, Default)]
pub struct MessageReader(String);

/// An internal type used during initial JSON parsing.
///
/// Wraps an arbitrary JSON object, which may be any valid or invalid
/// RPC message. This allows initial parsing and response handling to
/// occur on the read thread. If the message looks like a request, it
/// is passed to the main thread for handling.
#[derive(Debug, Clone)]
pub struct RpcObject(pub Value);

#[derive(Debug, Clone, PartialEq)]
/// An RPC call, which may be either a notification or a request.
pub enum Call<N, R> {
    /// An id and an RPC Request
    Request(RequestId, R),
    /// An RPC Notification
    Notification(N),
    /// A malformed request: the request contained an id, but could
    /// not be parsed. The client will receive an error.
    InvalidRequest(RequestId, RemoteError),
}

impl MessageReader {
    /// Attempts to read the next line from the stream and parse it as
    /// an RPC object.
    ///
    /// # Errors
    ///
    /// This function will return an error if there is an underlying
    /// I/O error, if the stream is closed, or if the message is not
    /// a valid JSON object.
    pub fn next<R: BufRead>(&mut self, reader: &mut R) -> Result<RpcObject, ReadError> {...}

    /// Attempts to parse a &str as an RPC Object.
    ///
    /// This should not be called directly unless you are writing tests.
    #[doc(hidden)]
    pub fn parse(&self, s: &str) -> Result<RpcObject, ReadError> {...}
}

impl RpcObject {
    /// Returns the 'id' of the underlying object, if present.
    pub fn get_id(&self) -> Option<RequestId> {...}

    /// Returns the 'method' field of the underlying object, if present.
    pub fn get_method(&self) -> Option<&str> {...}

    /// Returns `true` if this object looks like an RPC response;
    /// that is, if it has an 'id' field and does _not_ have a 'method'
    /// field.
    pub fn is_response(&self) -> bool {...}

    /// Attempts to convert the underlying `Value` into an RPC response
    /// object, and returns the result.
    ///
    /// The caller is expected to verify that the object is a response
    /// before calling this method.
    ///
    /// # Errors
    ///
    /// If the `Value` is not a well formed response object, this will
    /// return a `String` containing an error message. The caller should
    /// print this message and exit.
    pub fn into_response(mut self) -> Result<Response, String> {...}

    /// Attempts to convert the underlying `Value` into either an RPC
    /// notification or request.
    ///
    /// # Errors
    ///
    /// Returns a `serde_json::Error` if the `Value` cannot be converted
    /// to one of the expected types.
    pub fn into_rpc<N, R>(self) -> Result<Call<N, R>, JsonError>
    where
        N: DeserializeOwned,
        R: DeserializeOwned,
    {...}
}

impl From<Value> for RpcObject {
    fn from(v: Value) -> RpcObject {...}
}
```

## xi-editor-ph7/rust/rpc/src/test_utils.rs

```rust
use std::io::{self, Cursor, Write};
use std::sync::mpsc::{channel, Receiver, Sender};
use std::time::{Duration, Instant};

use serde_json::{self, Value};

use super::{Callback, Error, MessageReader, Peer, ReadError, Response, RpcObject};

/// Wraps an instance of `mpsc::Sender`, implementing `Write`.
///
/// This lets the tx side of an mpsc::channel serve as the destination
/// stream for an RPC loop.
pub struct DummyWriter(Sender<String>);

/// Wraps an instance of `mpsc::Receiver`, providing convenience methods
/// for parsing received messages.
pub struct DummyReader(MessageReader, Receiver<String>);

/// An Peer that doesn't do anything.
#[derive(Debug, Clone)]
pub struct DummyPeer;

/// Returns a `(DummyWriter, DummyReader)` pair.
pub fn test_channel() -> (DummyWriter, DummyReader) {...}

/// Given a string type, returns a `Cursor<Vec<u8>>`, which implements
/// `BufRead`.
pub fn make_reader<S: AsRef<str>>(s: S) -> Cursor<Vec<u8>> {...}

impl DummyReader {
    /// Attempts to read a message, returning `None` if the wait exceeds
    /// `timeout`.
    ///
    /// This method makes no assumptions about the contents of the
    /// message, and does no error handling.
    pub fn next_timeout(&mut self, timeout: Duration) -> Option<Result<RpcObject, ReadError>> {...}

    /// Reads and parses a response object.
    ///
    /// # Panics
    ///
    /// Panics if a non-response message is received, or if no message
    /// is received after a reasonable time.
    pub fn expect_response(&mut self) -> Response {...}

    pub fn expect_object(&mut self) -> RpcObject {...}

    pub fn expect_rpc(&mut self, method: &str) -> RpcObject {...}

    pub fn expect_nothing(&mut self) {...}
}

impl Write for DummyWriter {
    fn write(&mut self, buf: &[u8]) -> io::Result<usize> {...}

    fn flush(&mut self) -> io::Result<()> {...}
}

impl Peer for DummyPeer {
    fn box_clone(&self) -> Box<dyn Peer> {...}
    fn send_rpc_notification(&self, _method: &str, _params: &Value) {...}
    fn send_rpc_request_async(&self, _method: &str, _params: &Value, f: Box<dyn Callback>) {...}
    fn send_rpc_request(&self, _method: &str, _params: &Value) -> Result<Value, Error> {...}
    fn request_is_pending(&self) -> bool {...}
    fn schedule_idle(&self, _token: usize) {...}
    fn schedule_timer(&self, _time: Instant, _token: usize) {...}
}
```

## xi-editor-ph7/rust/rpc/src/trace.rs

```rust
#[cfg(feature = "trace")]
pub use xi_trace::{trace, trace_block, trace_block_payload, trace_payload, SampleGuard};

#[cfg(not(feature = "trace"))]
mod shim {
    use std::marker::PhantomData;

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
}

#[cfg(not(feature = "trace"))]
pub use shim::{trace, trace_block, trace_block_payload, trace_payload, SampleGuard};
```

## xi-editor-ph7/rust/rpc/tests/integration.rs

```rust
#[macro_use]
extern crate serde_json;
extern crate xi_rpc;

use std::io;
use std::time::Duration;

use serde_json::Value;
use xi_rpc::test_utils::{make_reader, test_channel};
use xi_rpc::{Handler, ReadError, RemoteError, RpcCall, RpcCtx, RpcLoop};

/// Handler that responds to requests with whatever params they sent.
pub struct EchoHandler;

#[allow(unused)]
impl Handler for EchoHandler {
    type Notification = RpcCall;
    type Request = RpcCall;
    fn handle_notification(&mut self, ctx: &RpcCtx, rpc: Self::Notification) {...}
    fn handle_request(&mut self, ctx: &RpcCtx, rpc: Self::Request) -> Result<Value, RemoteError> {...}
}
```

