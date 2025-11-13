
// E:\repos\Atelia-org\xi-editor-sharp\src\xi.Core\bin\Debug\net9.0\xi.Core.dll
// xi.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// Global type: <Module>
// Architecture: AnyCPU (64-bit preferred)
// Runtime: v4.0.30319
// This assembly was compiled using the /deterministic option.
// Hash algorithm: SHA1
// Debug info: Loaded from portable PDB: E:\repos\Atelia-org\xi-editor-sharp\src\xi.Core\bin\Debug\net9.0\xi.Core.pdb
using Xi.Core.Rope.Tree;
[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.EnableEditAndContinue)]
[assembly: InternalsVisibleTo("xi.Core.Tests")]
[assembly: TargetFramework(".NETCoreApp,Version=v9.0", FrameworkDisplayName = ".NET 9.0")]
[assembly: AssemblyCompany("xi.Core")]
[assembly: AssemblyConfiguration("Debug")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0+cce7f6fc3dcd38d45e780cab6f9d06463a7f315a")]
[assembly: AssemblyProduct("xi.Core")]
[assembly: AssemblyTitle("xi.Core")]
[assembly: AssemblyVersion("1.0.0.0")]
[module: RefSafetyRules(11)]
namespace Xi.Core {
	public interface ITextBuffer {
		int Length { get; }
		void Append(string? text);
		void Append(ReadOnlySpan<char> text);
		void Clear();
		void Replace(int start, int length, string? text);
		string Snapshot();
		string GetSlice(int start, int length);
	}
	public sealed class TextBuffer : ITextBuffer {
		private readonly StringBuilder _builder = new StringBuilder();
		public int Length => _builder.Length;
		public void Append(string? text) {
			/* Append provided text to the buffer (delegates to Replace).
			   Compact summary: Append at end, no-op if null. */
		}
		public void Append(ReadOnlySpan<char> text) {
			/* Append ReadOnlySpan<char> contents to buffer if non-empty. */
		}
		public void Clear() {
			/* Clear the underlying StringBuilder contents. */
		}
		public void Replace(int start, int length, string? text) {
			/* Replace a range in the buffer with the provided text.
			   Validates range and updates the underlying builder. */
		}
		public string Snapshot() {
			/* Return a string snapshot of the buffer contents. */
		}
		public string GetSlice(int start, int length) {
			/* Return a substring of the buffer; validates bounds and throws when invalid. */
		}
		public override string ToString() {
			/* Return the buffer snapshot as string. */
		}
		private static void ValidateRange(int start, int length, int totalLength) {
			/* Validate start and length are within [0, totalLength]; throw on violation. */
		}
	}
}
namespace Xi.Core.Rope {
	public interface IMetric : ITreeMetric<string, RopeInfo> {
	}
	public readonly struct Interval : IEquatable<Interval> {
		public int Start { get; }
		public int End { get; }
		public int Length => End - Start;
		public bool IsEmpty => Start == End;
		public static Interval Empty => new Interval(0, 0);
		public Interval(int start, int end) {
			/* Initialize interval with start and end; validate non-negative and start <= end. */
		}
		public bool Contains(int position) {
			/* Return true when position lies in [Start, End). */
		}
		public Interval Intersect(Interval other) {
			/* Return intersection of this interval with other, or Empty when disjoint. */
		}
		public Interval Union(Interval other) {
			/* Return union of two intervals, handling empties specially. */
		}
		public Interval Translate(int delta) {
			/* Translate interval by delta with checked arithmetic; return new interval. */
		}
		public static Interval EmptyAt(int position) {
			/* Create and return an empty interval at the given position. */
		}
		public bool Equals(Interval other) {
			/* Compare start and end for equality. */
		}
		public override bool Equals(object? obj) {
			/* Boxed equality comparison: true when obj is an Interval and equal. */
		}
		public override int GetHashCode() {
			/* Compute hash code from Start and End. */
		}
		public override string ToString() {
			/* Format interval as string "[Start, End)". */
		}
		public static bool operator ==(Interval left, Interval right) {
			/* Equality operator: compare as Equals. */
		}
		public static bool operator !=(Interval left, Interval right) {
			/* Inequality operator: opposite of Equals. */
		}
	}
	public sealed class BaseMetric : IMetric, ITreeMetric<string, RopeInfo> {
		public static BaseMetric Instance { get; } = new BaseMetric();
		public bool CanFragment => false;
		private BaseMetric() {
			/* Private ctor for singleton BaseMetric. */
		}
		public int Measure(RopeInfo info, int nodeLength) {
			/* Measure returns nodeLength in base units for BaseMetric. */
		}
		public int ToBaseUnits(string leaf, int measuredUnits) {
			/* Convert measured units to base units (identity) while validating UTF-16 boundary. */
		}
		public int FromBaseUnits(string leaf, int baseUnits) {
			/* Convert base units to measured units (identity) while validating UTF-16 boundary. */
		}
		public bool IsBoundary(string leaf, int offset) {
			/* Determine if offset is a UTF-16 boundary using helper. */
		}
		public int? GetPreviousBoundary(string leaf, int offset) {
			/* Return previous UTF-16 boundary index if any. */
		}
		public int? GetNextBoundary(string leaf, int offset) {
			/* Return next UTF-16 boundary index if any. */
		}
	}
	public sealed class LinesMetric : IMetric, ITreeMetric<string, RopeInfo> {
		public static LinesMetric Instance { get; } = new LinesMetric();
		public bool CanFragment => true;
		private LinesMetric() {
			/* Private ctor for LinesMetric singleton. */
		}
		public int Measure(RopeInfo info, int nodeLength) {
			/* Measure returns the line count from RopeInfo. */
		}
		public int ToBaseUnits(string leaf, int measuredUnits) {
			/* Convert measured line count to base character units by scanning for newlines and validating count. */
		}
		public int FromBaseUnits(string leaf, int baseUnits) {
			/* Compute number of lines present in the first baseUnits characters. */
		}
		public bool IsBoundary(string leaf, int offset) {
			/* A boundary is at offset when previous character is newline (except boundaries beyond range). */
		}
		public int? GetPreviousBoundary(string leaf, int offset) {
			/* Search backward for previous newline and return its subsequent offset, or null. */
		}
		public int? GetNextBoundary(string leaf, int offset) {
			/* Search forward for next newline and return its subsequent offset, or null. */
		}
	}
	public sealed class Utf16Metric : IMetric, ITreeMetric<string, RopeInfo> {
		public static Utf16Metric Instance { get; } = new Utf16Metric();
		public bool CanFragment => false;
		private Utf16Metric() {
			/* Private ctor for singleton Utf16Metric. */
		}
		public int Measure(RopeInfo info, int nodeLength) {
			/* Measure returns the UTF-16 code unit length from RopeInfo. */
		}
		public int ToBaseUnits(string leaf, int measuredUnits) {
			/* Convert measured UTF-16 units to base units by enumerating runes, validating boundaries. */
		}
		public int FromBaseUnits(string leaf, int baseUnits) {
			/* Convert base unit count to measured value; simple identity with range validation. */
		}
		public bool IsBoundary(string leaf, int offset) {
			/* Determine whether offset sits on a valid UTF-16 boundary using helper. */
		}
		public int? GetPreviousBoundary(string leaf, int offset) {
			/* Return previous UTF-16 boundary index via helper. */
		}
		public int? GetNextBoundary(string leaf, int offset) {
			/* Return next UTF-16 boundary index via helper. */
		}
	}
	internal static class Utf16BoundaryHelper {
		public static bool IsBoundary(ReadOnlySpan<char> leaf, int offset) {
			/* Determine whether offset is a valid UTF-16 boundary: true for ends, or not splitting a surrogate pair. */
		}
		public static int? GetPreviousBoundary(ReadOnlySpan<char> leaf, int offset) {
			/* Return the index of the previous UTF-16 boundary, adjusting for surrogate pairs. */
		}
		public static int? GetNextBoundary(ReadOnlySpan<char> leaf, int offset) {
			/* Return the next UTF-16 boundary index, adjusting for surrogate pairs, or null when at end. */
		}
	}
	public sealed class Rope : ITextBuffer {
		private Node _root = Node.Empty;
		public int Length => _root.Length;
		internal Node DebugRoot => _root;
		public void Append(string? text) {
			/* Append provided text to the rope by delegating to Replace at end. */
		}
		public void Append(ReadOnlySpan<char> text) {
			/* Append span contents to rope if non-empty (converts to string). */
		}
		public void Clear() {
			/* Reset root to empty node. */
		}
		public void Replace(int start, int length, string? text) {
			/* Replace range in rope by validating and delegating to root.Replace. */
		}
		public string Snapshot() {
			/* Return concatenated string snapshot by delegating to root.ToString(). */
		}
		public string GetSlice(int start, int length) {
			/* Validate and return a slice by delegating to node slicing. */
		}
		private static void ValidateRange(int start, int length, int totalLength) {
			/* Validate that start/length fit inside totalLength; throw on invalid values. */
		}
	}
	public readonly struct RopeInfo : ITreeNodeInfo<RopeInfo, string>, IDefaultMetricProvider<RopeInfo, string, BaseMetric> {
		public int LineCount { get; }
		public int Utf16Length { get; }
		public static RopeInfo Identity => new RopeInfo(0, 0);
		static BaseMetric IDefaultMetricProvider<RopeInfo, string, BaseMetric>.DefaultMetric => BaseMetric.Instance;
		private RopeInfo(int lineCount, int utf16Length) {
			/* Initialize RopeInfo with line count and UTF-16 length. */
		}
		public static RopeInfo FromLeaf(ReadOnlySpan<char> span) {
			/* Analyze span and return RopeInfo representing line and utf16 counts. */
		}
		public static RopeInfo FromLeaf(string leaf) {
			/* Validate leaf is non-null and forward to span-based analyzer. */
		}
		public RopeInfo Accumulate(RopeInfo other) {
			/* Accumulate counts from other RopeInfo and return a combined value. */
		}
		public Interval IntervalForPrefix(int prefixLength) {
			/* Return interval representing first prefixLength base units (clamped to Utf16Length). */
		}
		private static (int lines, int utf16) AnalyzeSpan(ReadOnlySpan<char> span) {
			/* Compute line count and total UTF-16 sequence length from span by scanning chars and runes. */
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int AddLength(int baseLength) {
			/* Add this info's UTF-16 length to baseLength with checked arithmetic. */
		}
		static RopeInfo ITreeNodeInfo<RopeInfo, string>.FromLeaf(string leaf) {
			/* Explicit interface impl delegation to FromLeaf. */
		}
	}
}
namespace Xi.Core.Rope.Tree {
	internal static class LeafSplitter {
		internal const int NewlinePreferenceWindow = 64;
		internal static IEnumerable<string> Split(string leaf) {
            /* Split a string leaf into segments that satisfy capacity constraints (newline-preferring). */
		}
	}
	public sealed class Node {
		private sealed class NodeBody {
			public int Height { get; }
			public int Length { get; }
			public RopeInfo Info { get; }
			public string? Leaf { get; }
			public Node[]? Children { get; }
			public NodeBody(int height, int length, RopeInfo info, string? leaf, Node[]? children) {
				/* Construct NodeBody with height, aggregate length, info and payload. */
			}
			public NodeBody Clone(Node[]? overrideChildren = null) {
				/* Clone underlying body optionally replacing children; used by shared node make-unique paths. */
			}
		}
		private sealed class SharedNode {
			private readonly NodeBody _body;
			public static SharedNode Empty { get; } = new SharedNode(new NodeBody(0, 0, RopeInfo.Identity, string.Empty, null));
			public NodeBody Body => _body;
			public int Height => _body.Height;
			public int Length => _body.Length;
			public RopeInfo Info => _body.Info;
			public string? Leaf => _body.Leaf;
			public Node[]? Children => _body.Children;
			public SharedNode(NodeBody body) {
				/* Wrap an existing NodeBody in an immutable SharedNode container. */
			}
			public SharedNode EnsureUnique() {
				/* Return a new SharedNode wrapping a shallow-cloned NodeBody (to ensure unique ownership). */
			}
			public SharedNode CloneWithChildren(IReadOnlyList<Node> newChildren) {
				/* Create a new SharedNode built from provided children while validating heights and aggregating info. */
			}
			public SharedNode ReplaceChildRange(int index, int removeCount, IReadOnlyList<Node> replacements) {
				/* Replace a child range with replacements; validate inputs and aggregate new info/length. */
			}
			public static SharedNode FromInternal(int height, IReadOnlyList<Node> children) {
				/* Construct SharedNode from internal children by materializing and aggregating info. */
			}
			private static (int Length, RopeInfo Info, Node[] Array) MaterializeChildren(int parentHeight, IReadOnlyList<Node> children) {
				/* Validate and materialize provided children for an internal node; aggregate length and info. */
			}
			private static (int Length, RopeInfo Info) AggregateChildren(int parentHeight, Node[] children) {
				/* Aggregate child lengths and info, validating heights for an internal node. */
			}
		}
		private readonly SharedNode _shared;
		public static int MinLeafSize => StringLeafOperations.MinLeafSize;
		public static int MaxLeafSize => StringLeafOperations.MaxLeafSize;
		private NodeBody Body => _shared.Body;
		public static Node Empty { get; } = new Node(SharedNode.Empty);
		public int Height => Body.Height;
		public int Length => Body.Length;
		public RopeInfo Info => Body.Info;
		public bool IsLeaf => Body.Height == 0;
		public bool IsEmpty => Length == 0;
		public int ChildCount {
			get {
				/* Return number of child nodes for internal node (0 for leaves). */
			}
		}
		public IReadOnlyList<Node> Children => Body.Children ?? Array.Empty<Node>();
		public ReadOnlySpan<char> LeafSpan => (Body.Leaf == null) ? ReadOnlySpan<char>.Empty : Body.Leaf.AsSpan();
		private Node(NodeBody body)
			: this(new SharedNode(body)) {
			/* Create a new Node by wrapping NodeBody in a SharedNode. */
		}
		private Node(SharedNode shared) {
			/* Construct Node from shared wrapper. */
		}
		private static Node FromShared(SharedNode shared) {
			/* Create Node from existing SharedNode. */
		}
		public static Node FromLeaf(string? text) {
			/* Create a leaf node from a string; compute RopeInfo and return Empty for null/empty. */
		}
		public static Node Concat(Node left, Node right) {
			/* Concatenate two nodes, handling empty cases and balancing heights with helpers. */
		}
		public IEnumerable<Node> TraverseLeaves() {
			/* Iterate leaf nodes in the tree; depth-first traversal exposing leaves. */
		}
		public Node Slice(int start, int length) {
			/* Return a node representing the slice [start, start+length) by splitting as needed. */
		}
		public override string ToString() {
			/* Concatenate child strings or return leaf payload. */
		}
		public Node Insert(int start, string text) {
			/* Insert text at position start: try in-leaf fast path, otherwise split and rebuild with TreeBuilder. */
		}
		public Node Delete(int start, int length) {
			/* Delete the range [start, start+length); attempt fast single-segment path or rebuild from splits. */
		}
		public Node Replace(int start, int length, string? text) {
			/* Replace a range with text by delegating to Insert/Delete fast paths or performing split-based rebuild. */
		}
		public Node EnsureWritableLeaf() {
			/* Return a writable clone of a leaf: clone payload if needed to ensure uniqueness. */
		}
		public IReadOnlyList<Node> SplitLeafByBounds() {
			/* Split this leaf into smaller leaf nodes according to capacity boundaries. */
		}
		private bool TryInsertInSingleLeaf(int start, string text, out Node result, out IReadOnlyList<Node>? splitNodes) {
			/* Fast-path insertion when operation affects only a single leaf.
			   Returns result or replacement segments if splitting was required. */
		}
		private bool TryDeleteInSingleSegment(int start, int length, out Node result) {
			/* Fast-path deletion when the range lies entirely within a single leaf; may merge/rebalance neighbors. */
		}
		private bool TryReplaceInSingleSegment(int start, int length, string text, out Node result, out IReadOnlyList<Node>? splitNodes) {
			/* Fast-path replace when impact is confined to a single leaf; may split, merge, or rebalance neighboring leaves. */
		}
		public Node CloneWithChildren(IReadOnlyList<Node> newChildren) {
			/* Clone current node but replace its children with the provided list; validate node type. */
		}
		public IReadOnlyList<string> CollectInvariantIssues(bool enforceLeafMinimum = false) {
			/* Collect a list of invariant issues by traversing and validating the tree; optional leaf-min enforcement. */
		}
		public void ValidateInvariants(bool enforceLeafMinimum = false) {
			/* Validate invariants and throw if issues found (delegates to CollectInvariantIssues). */
		}
		public Node NormalizeLeafMinimum() {
			/* Iterate to repair leaf underflow issues by resolving paths and rebalancing until stable. */
		}
		private static bool TryParseInvariantPath(string issue, out int[] indices) {
			/* Parse a path expression from an invariant description string and return indices for node traversal. */
		}
		private bool TryResolveLeafUnderflow(ReadOnlySpan<int> path, out Node updated) {
			/* Attempt to resolve a leaf underflow found at path: merge or rebalance and rebuild upward segments. */
		}
		private Node ReplaceChildWithSegments(Node[] children, int index, IReadOnlyList<Node> segments) {
			/* Replace a single child with a set of segment nodes and rebuild the internal SharedNode. */
		}
		public Node WithChildReplaced(int index, Node newChild) {
			/* Replace child at index with a new child node, returning updated internal node. */
		}
		public (Node Left, Node Right) SplitAt(int index) {
			/* Split node at index into a left and right node; recurse into children for internal splits. */
		}
		private static Node ConcatLeftShorter(Node left, Node right) {
			/* Handle concatenation when left.Height < right.Height by pushing into right's leftmost subtree and rebalancing. */
		}
		private static Node ConcatRightShorter(Node left, Node right) {
			/* Handle concatenation when right.Height < left.Height by merging into left's rightmost subtree and rebalancing. */
		}
		private Node[] RequireChildren() {
			/* Return non-null children array for internal nodes; throw if leaf. */
		}
		private static string GetLeafText(Node node) {
			/* Return the leaf text of a leaf node, or empty string if null; throw when called on internal node. */
		}
		private bool TryMergeLeafWithSibling(Node[] children, int index, Node replacement, out Node result) {
			/* Attempt to merge a small replacement leaf with a neighboring sibling when size constraints permit. */
		}
		private bool TryRebalanceLeafWithSibling(Node[] children, int index, Node replacement, out Node result) {
			/* Attempt to rebalance a pair of adjacent leaf nodes to maintain min/max constraints, using helper TryRebalancePair. */
		}
		private bool TryRebalancePair(Node[] children, int firstIndex, int secondIndex, Node first, Node second, out Node result) {
			/* Rebalance an adjacent leaf pair by computing a balanced split if combined size exceeds max; return new internal structure. */
		}
		private static void ValidateNode(Node node, bool isRoot, bool enforceLeafMinimum, List<string> issues, string path) {
			/* Validate a node's invariants: leaf length, leaf sizes, and internal-child aggregates; record issues in the list. */
		}
		private static string FormatLeafPreview(Node node) {
			/* Return a short escaped preview of a leaf's text (first 32 chars visible). */
		}
		private static string EscapePreview(string text) {
			/* Escape control characters in preview strings for display in diagnostic messages. */
		}
		private static string SummarizeChildren(Node[] children) {
			/* Return a compact summary string with lengths of up to first 6 children and total count. */
		}
		private Node BuildMergedNode(Node[] children, int firstIndex, int secondIndex, Node mergedLeaf) {
			/* Build a node with two adjacent children replaced by a merged leaf; handle edge cases like full-child replacement. */
		}
		private static Node CreateInternal(int height, IReadOnlyList<Node> children) {
			/* Create an internal node from the given children; return Empty when no children. */
		}
		private static Node BuildFromSegments(List<Node> segments) {
			/* Build a balanced node tree from provided segments using TreeBuilder, handling empty and single segment cases. */
		}
	}
	public sealed class Node<TInfo, TLeaf, TLeafOps> where TInfo : struct, ITreeNodeInfo<TInfo, TLeaf> where TLeafOps : ILeafOperations<TLeaf> {
		private sealed record NodeBody(int Height, int Length, TInfo Info, TLeaf Leaf, Node<TInfo, TLeaf, TLeafOps>[]? Children);
		private static readonly Node<TInfo, TLeaf, TLeafOps> s_empty = new Node<TInfo, TLeaf, TLeafOps>(new NodeBody(0, 0, TInfo.Identity, TLeafOps.Empty, null));
		private readonly NodeBody _body;
		public static Node<TInfo, TLeaf, TLeafOps> Empty => s_empty;
		public int Height => _body.Height;
		public int Length => _body.Length;
		public TInfo Info => _body.Info;
		public bool IsLeaf => _body.Children == null;
		public bool IsEmpty => Length == 0;
		public TLeaf Leaf {
			get {
				/* Return the leaf payload; throws when called on an internal node. */
			}
		}
		public IReadOnlyList<Node<TInfo, TLeaf, TLeafOps>> Children => _body.Children ?? Array.Empty<Node<TInfo, TLeaf, TLeafOps>>();
		private Node(NodeBody body) {
			/* Construct a generic node from NodeBody. */
		}
		public static Node<TInfo, TLeaf, TLeafOps> FromLeaf(TLeaf leaf) {
			/* Create generic leaf node from TLeaf by computing length and node info via TLeafOps/TInfo. */
		}
		public static Node<TInfo, TLeaf, TLeafOps> CreateInternal(IReadOnlyList<Node<TInfo, TLeaf, TLeafOps>> children) {
			/* Create an internal generic node by validating heights, aggregating length/info, and building NodeBody. */
		}
		public IEnumerable<Node<TInfo, TLeaf, TLeafOps>> TraverseLeaves() {
			/* Generic traversal of leaf nodes; equivalent semantics to non-generic TraverseLeaves. */
		}
	}
	public sealed class NodeCursor {
		public Node Root { get; }
		public int Position { get; private set; }
		public int TotalLength => Root.Length;
		public NodeCursor(Node root, int position) {
			/* NodeCursor skeleton: store root and initial position; implementation omitted. */
		}
		public (string Leaf, int Offset)? GetLeaf() {
			/* Return the leaf string and offset for current cursor position when available. */
		}
		public void SetPosition(int position) {
			/* Update cursor position within root bounds. */
		}
		public bool IsBoundary(IMetric metric) {
			/* Determine if the current position is a metric boundary (e.g., glyph or line). */
		}
		public int? MoveToPrevious(IMetric metric) {
			/* Move cursor to the previous boundary for given metric and return new position. */
		}
		public int? MoveToNext(IMetric metric) {
			/* Move cursor to next boundary for metric and return new position. */
		}
		public int? AtOrNext(IMetric metric) {
			/* Return the position at or after the current cursor that aligns to the metric boundary. */
		}
		public int? AtOrPrevious(IMetric metric) {
			/* Return the position at or before the current cursor that aligns to the metric boundary. */
		}
	}
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	internal readonly struct StringLeafOperations : ILeafOperations<string> {
		public static int MinLeafSize => 511;
		public static int MaxLeafSize => 1024;
		public static string Empty => string.Empty;
		public static int GetLength(string leaf) {
			/* Return the length of the leaf string (zero for null). */
		}
		public static bool IsValidChild(string leaf) {
			/* Return whether the leaf is long enough to be a valid child (>= MinLeafSize). */
		}
		public static string Clone(string leaf) {
			/* Clone a leaf string into a new allocation preserving contents; returns Empty when length is 0. */
		}
		public static string Insert(string leaf, int index, string text) {
			/* Insert text into leaf at index and return a newly created string; validate index and no-op when insertion is empty. */
		}
		public static string RemoveRange(string leaf, int index, int length) {
			/* Remove a range from a leaf and return a new string with content before and after removed range; validate bounds. */
		}
		public static string ReplaceRange(string leaf, int index, int length, string replacement) {
			/* Replace range in a leaf with a replacement string, combining Insert/Remove fast paths; returns new string. */
		}
		public static string Merge(string left, string right) {
			/* Merge two adjacent leaf strings into a single new string; empty optimized to Empty. */
		}
		public static bool TryComputeBalancedSplit(string left, string right, out string newLeft, out string newRight) {
			/* Try to split a combined left/right into two balanced leaves within Min/Max constraints, favoring newline boundaries and UTF-16 safety. */
		}
		public static IEnumerable<string> SplitByCapacity(string leaf) {
			/* Split a leaf into capacity-bound segments preferring newline boundaries and avoiding surrogate split. */
		}
		private static int PreferNewlineBoundary(string left, string right, int candidate, int minSplit, int maxSplit) {
			/* Prefer a split offset that lands on a newline within a reasonable window when possible. */
		}
		private static bool TryEnsureSurrogateBoundary(string left, string right, ref int splitIndex, int minSplit, int maxSplit) {
			/* Adjust candidate split index to ensure it doesn't split a surrogate pair; attempt +-1 adjustment if possible. */
		}
		private static bool IsSafeBoundary(string left, string right, int index) {
			/* Return true when index does not split a surrogate pair across left+right combined boundaries. */
		}
		private static string CreateCombinedSegment(string left, string right, int start, int length) {
			/* Create a contiguous segment from the logical concatenation of left and right, starting at start for length chars. */
		}
		private static char GetCombinedChar(string left, string right, int index) {
			/* Retrieve the char at index from the conceptual concatenation of left+right. */
		}
	}
	public sealed class TreeBuilder {
		private readonly List<Node> _pending = new List<Node>();
		public void PushString(string? text) {
			/* Push a string into the builder by splitting into leaf-sized segments and appending. */
		}
		public void PushSpan(ReadOnlySpan<char> span) {
			/* Push a span by converting to string and pushing via PushString when non-empty. */
		}
		public void PushNode(Node node) {
			/* Append a pre-built node segment into pending list if it's not empty. */
		}
		public Node Build() {
			/* Build and return a balanced node tree by concatenating pending nodes. */
		}
		public void Reset() {
			/* Clear the internal pending list. */
		}
		private void AppendNode(Node node) {
			/* Append node while collapsing adjacent nodes of equal height by concatenating them. */
		}
		private static IEnumerable<string> SplitIntoLeaves(string text) {
			/* Split string into leaf-sized segments via the LeafSplitter (newline-preferring). */
		}
	}
	public interface ILeafOperations<TLeaf> {
		static abstract int MinLeafSize { get; }
		static abstract int MaxLeafSize { get; }
		static abstract TLeaf Empty { get; }
		static abstract int GetLength(TLeaf leaf);
		static abstract bool IsValidChild(TLeaf leaf);
		static abstract TLeaf Clone(TLeaf leaf);
		static abstract TLeaf Insert(TLeaf leaf, int index, TLeaf insertion);
		static abstract TLeaf RemoveRange(TLeaf leaf, int index, int length);
		static abstract TLeaf ReplaceRange(TLeaf leaf, int index, int length, TLeaf replacement);
		static abstract TLeaf Merge(TLeaf left, TLeaf right);
		static abstract bool TryComputeBalancedSplit(TLeaf left, TLeaf right, [MaybeNullWhen(false)] out TLeaf newLeft, [MaybeNullWhen(false)] out TLeaf newRight);
		static abstract IEnumerable<TLeaf> SplitByCapacity(TLeaf leaf);
	}
	public interface ITreeNodeInfo<TSelf, TLeaf> where TSelf : struct, ITreeNodeInfo<TSelf, TLeaf> {
		static abstract TSelf Identity { get; }
		static abstract TSelf FromLeaf(TLeaf leaf);
		TSelf Accumulate(TSelf other);
		Interval IntervalForPrefix(int prefixLength);
	}
	public interface IDefaultMetricProvider<TSelf, TLeaf, TMetric> where TSelf : struct, ITreeNodeInfo<TSelf, TLeaf> where TMetric : class, ITreeMetric<TLeaf, TSelf> {
		static abstract TMetric DefaultMetric { get; }
	}
	public interface ITreeMetric<TLeaf, TInfo> where TInfo : struct, ITreeNodeInfo<TInfo, TLeaf> {
		bool CanFragment { get; }
		int Measure(TInfo info, int nodeLength);
		int ToBaseUnits(TLeaf leaf, int measuredUnits);
		int FromBaseUnits(TLeaf leaf, int baseUnits);
		bool IsBoundary(TLeaf leaf, int offset);
		int? GetPreviousBoundary(TLeaf leaf, int offset);
		int? GetNextBoundary(TLeaf leaf, int offset);
	}
}
namespace xi.Core {
	public class Class1 {
	}
}
