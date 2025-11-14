
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
[assembly: AssemblyInformationalVersion("1.0.0+8e70694a288359017e8225e799769d324e901489")]
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
			// Appends text to the end of the buffer in the original implementation.
		}
		public void Append(ReadOnlySpan<char> text) {
			// Appended spans directly to the StringBuilder when not empty.
		}
		public void Clear() {
			// Cleared all accumulated text within the backing builder.
		}
		public void Replace(int start, int length, string? text) {
			// Replaced a span within the buffer, removing and optionally inserting new text.
		}
		public string Snapshot() {
			// Returned the full textual contents captured in the builder.
		}
		public string GetSlice(int start, int length) {
			// Produced a substring slice after validating bounds against the builder length.
		}
		public override string ToString() {
			// Delegated to Snapshot() for textual representation.
		}
		private static void ValidateRange(int start, int length, int totalLength) {
			// Guarded against invalid range parameters before mutating the buffer state.
		}
	}
}
namespace Xi.Core.Rope {
	internal static class BreaksMetricHelper {
		public static int GetNthBreakOffset(ReadOnlySpan<int> breaks, int leafLength, int measuredUnits) {
			// Located the offset of the Nth break with guard rails for range checks.
		}
		public static int CountBreaksUpTo(ReadOnlySpan<int> breaks, int offset) {
			// Counted break markers up to the requested offset using a binary search.
		}
		public static int? FindPreviousBreak(ReadOnlySpan<int> breaks, int offset) {
			// Walked backwards through sorted breaks to find the last boundary before offset.
		}
		public static int? FindNextBreak(ReadOnlySpan<int> breaks, int offset) {
			// Located the next available break boundary at or after the offset.
		}
		public static bool IsBreakBoundary(ReadOnlySpan<int> breaks, int offset) {
			// Determined whether the supplied offset exactly matched a break point.
		}
	}
	internal enum DeltaElementKind {
		Copy,
		Insert
	}
	internal readonly struct CopyElement {
		public int Start { get; }
		public int End { get; }
		public int Length => End - Start;
		public CopyElement(int start, int end) {
			// Validated copy range bounds and stored the original span offsets.
		}
	}
	internal readonly struct InsertElement<TLeaf> where TLeaf : class {
		public TLeaf Value { get; }
		public int Length { get; }
		public InsertElement(TLeaf value, int length) {
			// Captured the insert payload and enforced length invariants for the element.
		}
	}
	internal readonly struct DeltaElement<TLeaf> where TLeaf : class {
		private readonly DeltaElementKind _kind;
		private readonly CopyElement _copy;
		private readonly InsertElement<TLeaf> _insert;
		internal DeltaElementKind Kind => _kind;
		internal bool IsCopy => _kind == DeltaElementKind.Copy;
		internal bool IsInsert => _kind == DeltaElementKind.Insert;
		private DeltaElement(CopyElement copy) {
			_kind = DeltaElementKind.Copy;
			_copy = copy;
			_insert = default(InsertElement<TLeaf>);
		}
		private DeltaElement(InsertElement<TLeaf> insert) {
			_kind = DeltaElementKind.Insert;
			_copy = default(CopyElement);
			_insert = insert;
		}
		internal static DeltaElement<TLeaf> Copy(int start, int end) {
			// Wrapped a copy range into a delta element.
		}
		internal static DeltaElement<TLeaf> Insert(TLeaf value, int? length = null) {
			// Wrapped an insert payload into a delta element after resolving its length.
		}
		private static int ResolveInsertLength(TLeaf value) {
			// Derived the insert segment length and rejected unsupported payload types.
		}
		internal CopyElement AsCopy() {
			// Returned the copy payload or threw when the element was not a copy.
		}
		internal InsertElement<TLeaf> AsInsert() {
			// Returned the insert payload or threw when the element was not an insert.
		}
	}
	internal sealed class Delta<TInfo, TLeaf> where TLeaf : class {
		private readonly List<DeltaElement<TLeaf>> _elements;
		internal int BaseLength { get; }
		internal int ElementCount => _elements.Count;
		internal IReadOnlyList<DeltaElement<TLeaf>> Elements => _elements;
		private Delta(List<DeltaElement<TLeaf>> elements, int baseLength) {
			// Stored the canonicalized element list alongside the base document length.
		}
		internal IEnumerable<DeltaElement<TLeaf>> EnumerateElements() {
			// Exposed the ordered delta elements for iteration.
		}
		internal IEnumerable<(bool IsInsert, int Start, int End)> EnumerateElementTriples() {
			// Translated elements into xi-rope compatible triples while tracking new offsets.
		}
		internal static Delta<TInfo, TLeaf> FromElements(int baseLength, IEnumerable<DeltaElement<TLeaf>> elements) {
			// Normalized incoming elements and asserted they stayed within the base length.
		}
		internal static Delta<TInfo, TLeaf> FromElements(int baseLength, IEnumerable<(int? CopyStart, int? CopyEnd, TLeaf? Insert)> elementTuples) {
			// Converted tuple payloads into delta elements before delegating to the main factory.
		}
		internal (Delta<TInfo, TLeaf> InsertDelta, Subset DeletedSubset) Factor() {
			// Split the delta into insertion and deletion components in the original implementation.
		}
		private static void ValidateBaseLength(int baseLength, int maxCopyEnd) {
			// Guarded against negative base lengths and out-of-range copy spans.
		}
	}
	internal static class DeltaJson {
		private sealed class DeltaDto {
			[JsonPropertyName("els")]
			public ElementDto[] Elements { get; set; } = Array.Empty<ElementDto>();
			[JsonPropertyName("base_len")]
			public int BaseLength { get; set; }
		}
		private sealed class ElementDto {
			[JsonPropertyName("copy")]
			public int[]? Copy { get; set; }
			[JsonPropertyName("insert")]
			public string? Insert { get; set; }
		}
		private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions {
			PropertyNamingPolicy = null,
			WriteIndented = false,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		};
		internal static string Serialize<TInfo>(Delta<TInfo, string> delta) {
			// Emitted a xi-rope compatible JSON payload for the supplied delta.
		}
		internal static Delta<TInfo, string> Deserialize<TInfo>(string json) {
			// Parsed the xi-rope delta JSON payload back into a Delta<TInfo, string> instance.
		}
	}
	internal readonly struct RevId {
		public long Session1 { get; }
		public int Session2 { get; }
		public int Number { get; }
		public RevId(long session1, int session2, int number) {
			// Validated revision identifiers and captured the composite revision id.
		}
	}
	internal enum RevisionOperationKind {
		Edit,
		Undo
	}
	internal abstract class RevisionOperation {
		internal abstract RevisionOperationKind Kind { get; }
		internal bool IsEdit => Kind == RevisionOperationKind.Edit;
		internal bool IsUndo => Kind == RevisionOperationKind.Undo;
		internal RevisionEdit AsEdit() {
			// Returned the edit payload after confirming the operation kind.
		}
		internal RevisionUndo AsUndo() {
			// Returned the undo payload after confirming the operation kind.
		}
	}
	internal sealed class RevisionEdit : RevisionOperation {
		internal override RevisionOperationKind Kind => RevisionOperationKind.Edit;
		internal int Priority { get; }
		internal int UndoGroup { get; }
		internal Subset Inserts { get; }
		internal Subset Deletes { get; }
		internal RevisionEdit(int priority, int undoGroup, Subset inserts, Subset deletes) {
			// Captured edit metadata and enforced non-null subsets with non-negative priorities.
		}
	}
	internal sealed class RevisionUndo : RevisionOperation {
		private readonly int[] _toggledGroups;
		private readonly IReadOnlyList<int> _toggledGroupsView;
		internal override RevisionOperationKind Kind => RevisionOperationKind.Undo;
		internal IReadOnlyList<int> ToggledGroups => _toggledGroupsView;
		internal Subset DeletesBitxor { get; }
		internal RevisionUndo(IEnumerable<int> toggledGroups, Subset deletesBitxor) {
			// Stored the toggled undo groups alongside the deletes_bitxor subset snapshot.
		}
		private static int[] CopyGroups(IEnumerable<int> groups) {
			// Copied and validated undo group identifiers before freezing them in an array.
		}
	}
	internal sealed class Revision {
		internal RevId RevId { get; }
		internal int MaxUndoSoFar { get; }
		internal RevisionOperation Operation { get; }
		internal Revision(RevId revId, int maxUndoSoFar, RevisionOperation operation) {
			// Recorded the revision metadata and enforced a non-negative undo depth.
		}
	}
	internal sealed class Engine {
		private readonly string _text;
		private readonly string _tombstones;
		private readonly Subset _deletesFromUnion;
		private readonly int[] _undoneGroups;
		private readonly Revision[] _revisions;
		private readonly IReadOnlyList<int> _undoneGroupsView;
		private readonly IReadOnlyList<Revision> _revisionLogView;
		private Engine(string text, string tombstones, Subset deletesFromUnion, int[] undoneGroups, Revision[] revisions) {
			// Stored serialized fields and materialized read-only views over engine state.
		}
		internal static Engine FromSerializedState(string text, string tombstones, Subset deletesFromUnion, IEnumerable<int> undoneGroups, IEnumerable<Revision> revisions) {
			// Reconstructed an engine from serialized buffers, undone groups, and the revision log.
		}
		private static int[] CopyGroups(IEnumerable<int> source) {
			// Copied and validated group identifiers from the enumerable source.
		}
		private static Revision[] CopyRevisions(IEnumerable<Revision> source) {
			// Copied and validated revision entries from the enumerable source.
		}
		internal string TextSnapshot() {
			// Returned the preserved text snapshot from the engine state.
		}
		internal string TombstonesSnapshot() {
			// Returned the tombstones string captured in the engine state.
		}
		internal Subset DeletesFromUnionSnapshot() {
			// Returned the deletes-from-union subset snapshot.
		}
		internal IReadOnlyList<int> UndoneGroupsSnapshot() {
			// Returned the immutable view over undone groups.
		}
		internal IReadOnlyList<Revision> RevisionLog() {
			// Returned the immutable revision log view.
		}
	}
	internal static class EngineJson {
		private sealed class EngineDto {
			[JsonPropertyName("text")]
			public string? Text { get; set; }
			[JsonPropertyName("tombstones")]
			public string? Tombstones { get; set; }
			[JsonPropertyName("deletes_from_union")]
			public SubsetDto? DeletesFromUnion { get; set; }
			[JsonPropertyName("undone_groups")]
			public int[]? UndoneGroups { get; set; }
			[JsonPropertyName("revs")]
			public RevisionDto[]? Revisions { get; set; }
		}
		private sealed class RevisionDto {
			[JsonPropertyName("rev_id")]
			public RevIdDto? RevId { get; set; }
			[JsonPropertyName("max_undo_so_far")]
			public int MaxUndoSoFar { get; set; }
			[JsonPropertyName("edit")]
			public RevisionEditEnvelopeDto? Edit { get; set; }
		}
		private sealed class RevIdDto {
			[JsonPropertyName("session1")]
			public long Session1 { get; set; }
			[JsonPropertyName("session2")]
			public int Session2 { get; set; }
			[JsonPropertyName("num")]
			public int Number { get; set; }
		}
		private sealed class RevisionEditEnvelopeDto {
			[JsonPropertyName("Edit")]
			public EditPayloadDto? Edit { get; set; }
			[JsonPropertyName("Undo")]
			public UndoPayloadDto? Undo { get; set; }
		}
		private sealed class EditPayloadDto {
			[JsonPropertyName("priority")]
			public int Priority { get; set; }
			[JsonPropertyName("undo_group")]
			public int UndoGroup { get; set; }
			[JsonPropertyName("inserts")]
			public SubsetDto? Inserts { get; set; }
			[JsonPropertyName("deletes")]
			public SubsetDto? Deletes { get; set; }
		}
		private sealed class UndoPayloadDto {
			[JsonPropertyName("toggled_groups")]
			public int[]? ToggledGroups { get; set; }
			[JsonPropertyName("deletes_bitxor")]
			public SubsetDto? DeletesBitxor { get; set; }
		}
		private sealed class SubsetDto {
			[JsonPropertyName("segments")]
			public SegmentDto[]? Segments { get; set; }
		}
		private sealed class SegmentDto {
			[JsonPropertyName("len")]
			public int Length { get; set; }
			[JsonPropertyName("count")]
			public int Count { get; set; }
		}
		private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions {
			PropertyNamingPolicy = null,
			WriteIndented = false,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		};
		internal static string Serialize(Engine engine) {
			// Serialized engine state into the xi-rope JSON fixture format.
		}
		internal static Engine Deserialize(string json) {
			// Rehydrated an Engine instance from the xi-rope JSON payload.
		}
		private static int[] CopyGroups(IReadOnlyList<int> groups) {
			// Created a defensive copy of the undo group list.
		}
		private static int[] ValidateGroups(int[] groups, string fieldName) {
			// Validated undo group values and normalized empty sequences.
		}
		private static RevisionDto[] ToRevisionDtos(IReadOnlyList<Revision> revisions) {
			// Translated the revision log into DTOs for serialization.
		}
		private static RevisionDto ToRevisionDto(Revision revision) {
			// Projected a single revision into its DTO representation.
		}
		private static EditPayloadDto ToEditPayloadDto(RevisionEdit edit) {
			// Converted an edit revision into its DTO payload.
		}
		private static UndoPayloadDto ToUndoPayloadDto(RevisionUndo undo) {
			// Converted an undo revision into its DTO payload.
		}
		private static SubsetDto ToSubsetDto(Subset subset) {
			// Converted a subset into DTO segments for JSON output.
		}
		private static Subset FromSubsetDto(SubsetDto dto) {
			// Built a subset from DTO segments with validation.
		}
	}
	public interface IMetric : ITreeMetric<string, RopeInfo> {
	}
	public readonly struct Interval : IEquatable<Interval> {
		public int Start { get; }
		public int End { get; }
		public int Length => End - Start;
		public bool IsEmpty => Start == End;
		public static Interval Empty => new Interval(0, 0);
		public Interval(int start, int end) {
			// Validated inputs and populated Start/End to describe a half-open interval.
		}
		public bool Contains(int position) {
			// Reported whether the provided position fell within the interval bounds.
		}
		public Interval Intersect(Interval other) {
			// Returned overlap between two intervals or Empty when no intersection existed.
		}
		public Interval Union(Interval other) {
			// Combined two intervals into a minimal covering range, preserving empties.
		}
		public Interval Translate(int delta) {
			// Shifted the interval by a signed delta while preserving length.
		}
		public static Interval EmptyAt(int position) {
			// Produced an empty interval anchored at the requested position.
		}
		public bool Equals(Interval other) {
			// Performed value equality by comparing start and end positions.
		}
		public override bool Equals(object? obj) {
			// Deferred to the strongly-typed Equals implementation.
		}
		public override int GetHashCode() {
			// Combined Start and End for hashing semantics consistent with Equals.
		}
		public override string ToString() {
			// Formatted the interval using half-open notation.
		}
		public static bool operator ==(Interval left, Interval right) {
			// Compared two intervals for equality.
		}
		public static bool operator !=(Interval left, Interval right) {
			// Reported inequality between two intervals.
		}
	}
	public sealed class BaseMetric : IMetric, ITreeMetric<string, RopeInfo> {
		public static BaseMetric Instance { get; } = new BaseMetric();
		public bool CanFragment => false;
		private BaseMetric() {
			// Singleton constructor hidden in the original implementation.
		}
		public int Measure(RopeInfo info, int nodeLength) {
			// Reported node length as the base-unit measurement.
		}
		public int ToBaseUnits(string leaf, int measuredUnits) {
			// Validated UTF-16 boundaries and returned the measured units unchanged.
		}
		public int FromBaseUnits(string leaf, int baseUnits) {
			// Converted base units to metric units with surrogate safety checks.
		}
		public bool IsBoundary(string leaf, int offset) {
			// Determined if an offset was a valid UTF-16 boundary within the leaf.
		}
		public int? GetPreviousBoundary(string leaf, int offset) {
			// Found the preceding UTF-16 boundary relative to the offset.
		}
		public int? GetNextBoundary(string leaf, int offset) {
			// Located the next UTF-16 boundary after the supplied offset.
		}
	}
	public sealed class LinesMetric : IMetric, ITreeMetric<string, RopeInfo> {
		public static LinesMetric Instance { get; } = new LinesMetric();
		public bool CanFragment => true;
		private LinesMetric() {
			// Private constructor retaining singleton semantics.
		}
		public int Measure(RopeInfo info, int nodeLength) {
			// Reported the number of newline-delimited segments encoded in RopeInfo.
		}
		public int ToBaseUnits(string leaf, int measuredUnits) {
			// Walked newline characters to translate line counts into UTF-16 offsets.
		}
		public int FromBaseUnits(string leaf, int baseUnits) {
			// Counted newline characters within a span to compute measured units.
		}
		public bool IsBoundary(string leaf, int offset) {
			// Determined whether a position followed a newline boundary.
		}
		public int? GetPreviousBoundary(string leaf, int offset) {
			// Searched backwards for the prior newline boundary if present.
		}
		public int? GetNextBoundary(string leaf, int offset) {
			// Scanned forward to locate the next newline boundary.
		}
	}
	public sealed class Utf16Metric : IMetric, ITreeMetric<string, RopeInfo> {
		public static Utf16Metric Instance { get; } = new Utf16Metric();
		public bool CanFragment => false;
		private Utf16Metric() {
			// Private constructor maintaining singleton lifetime.
		}
		public int Measure(RopeInfo info, int nodeLength) {
			// Reported the cached UTF-16 code unit count from RopeInfo.
		}
		public int ToBaseUnits(string leaf, int measuredUnits) {
			// Converted rune counts into UTF-16 unit offsets with surrogate validation.
		}
		public int FromBaseUnits(string leaf, int baseUnits) {
			// Ensured the supplied base units were in range and returned them unchanged.
		}
		public bool IsBoundary(string leaf, int offset) {
			// Checked surrogate boundaries via Utf16BoundaryHelper.
		}
		public int? GetPreviousBoundary(string leaf, int offset) {
			// Delegated to Utf16BoundaryHelper for the preceding boundary.
		}
		public int? GetNextBoundary(string leaf, int offset) {
			// Delegated to Utf16BoundaryHelper for the next boundary.
		}
	}
	internal static class Utf16BoundaryHelper {
		public static bool IsBoundary(ReadOnlySpan<char> leaf, int offset) {
			// Determined whether a position falls on a valid UTF-16 surrogate boundary.
		}
		public static int? GetPreviousBoundary(ReadOnlySpan<char> leaf, int offset) {
			// Returned the previous safe boundary, adjusting for trailing surrogates.
		}
		public static int? GetNextBoundary(ReadOnlySpan<char> leaf, int offset) {
			// Returned the next safe boundary, handling high-surrogate pairs.
		}
	}
	public sealed class Rope : ITextBuffer {
		private Node _root = Node.Empty;
		public int Length => _root.Length;
		internal Node DebugRoot => _root;
		public void Append(string? text) {
			// Appended text by delegating to the core Replace pipeline at the tail.
		}
		public void Append(ReadOnlySpan<char> text) {
			// Accepted spans, converting to string before forwarding to Replace.
		}
		public void Clear() {
			// Reset the rope to its canonical empty node.
		}
		public void Replace(int start, int length, string? text) {
			// Validated a range and replaced the corresponding rope region.
		}
		public string Snapshot() {
			// Materialized the rope into a contiguous string snapshot.
		}
		public string GetSlice(int start, int length) {
			// Extracted a substring by slicing the underlying rope structure.
		}
		private static void ValidateRange(int start, int length, int totalLength) {
			// Ensured index and length arguments were within the rope's bounds.
		}
	}
	public readonly struct RopeInfo : ITreeNodeInfo<RopeInfo, string>, IDefaultMetricProvider<RopeInfo, string, BaseMetric> {
		public int LineCount { get; }
		public int Utf16Length { get; }
		public static RopeInfo Identity => new RopeInfo(0, 0);
		static BaseMetric IDefaultMetricProvider<RopeInfo, string, BaseMetric>.DefaultMetric => BaseMetric.Instance;
		private RopeInfo(int lineCount, int utf16Length) {
			// Stored line and UTF-16 aggregates for a rope segment.
		}
		public static RopeInfo FromLeaf(ReadOnlySpan<char> span) {
			// Analyzed a leaf span to produce aggregate line and UTF-16 metrics.
		}
		public static RopeInfo FromLeaf(string leaf) {
			// Overload accepting string leaves before forwarding to the span-based analyzer.
		}
		public RopeInfo Accumulate(RopeInfo other) {
			// Combined metrics from another RopeInfo to support tree aggregation.
		}
		public Interval IntervalForPrefix(int prefixLength) {
			// Projected a prefix length onto an interval bounded by cached UTF-16 length.
		}
		private static (int lines, int utf16) AnalyzeSpan(ReadOnlySpan<char> span) {
			// Counted newline characters and UTF-16 code units across the provided span.
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int AddLength(int baseLength) {
			// Accumulated the stored UTF-16 length with an existing base length.
		}
		static RopeInfo ITreeNodeInfo<RopeInfo, string>.FromLeaf(string leaf) {
			// Invoked the static factory to derive RopeInfo from a leaf string.
		}
	}
	internal readonly struct SubsetSegment {
		public int Length { get; }
		public int Count { get; }
		public SubsetSegment(int length, int count) {
			// Validated arguments and captured the subset segment metadata.
		}
	}
	internal sealed class Subset {
		private readonly SubsetSegment[] _segments;
		private readonly int _totalLength;
		internal static Subset Empty { get; } = new Subset(Array.Empty<SubsetSegment>(), 0);
		internal int SegmentCount => _segments.Length;
		internal bool IsEmpty => _segments.Length == 0 || (_segments.Length == 1 && _segments[0].Count == 0);
		internal int Length => _totalLength;
		private Subset(SubsetSegment[] segments, int totalLength) {
			// Stored the ordered segments and their aggregate length.
		}
		internal static Subset Create(SubsetSegment[] segments, int totalLength) {
			// Constructed a normalized subset from ordered segments and a total length.
		}
		internal int LengthAfterDelete() {
			// Calculated the retained length after removing segments marked for deletion.
		}
		internal IEnumerable<(int Start, int Length, int Count)> SegmentTriples() {
			// Iterated segments yielding triples for downstream serialization helpers.
		}
		internal static Subset FromSegmentTriples(IEnumerable<(int Start, int Length, int Count)> triples) {
			// Built a subset by interpreting ordered triples and coalescing implicit gaps.
		}
	}
	internal sealed class SubsetBuilder {
		private readonly List<SubsetSegment> _segments = new List<SubsetSegment>();
		private int _totalLength;
		internal void PadToLength(int totalLength) {
			// Extended the builder with zero-count padding to reach the requested length.
		}
		internal void AddRange(int begin, int end, int count) {
			// Added a counted range while preserving ordering and coalescing gaps.
		}
		internal void PushSegment(int length, int count) {
			// Added a single segment after validating length/count invariants.
		}
		private void PushSegmentInternal(int length, int count) {
			// Internal helper that updates totals and merges adjacent segments when possible.
		}
		internal Subset Build() {
			// Produced an immutable subset snapshot from accumulated segments.
		}
	}
	internal static class SubsetJson {
		private sealed class SubsetDto {
			[JsonPropertyName("segments")]
			public SegmentDto[] Segments { get; set; } = Array.Empty<SegmentDto>();
		}
		private sealed class SegmentDto {
			[JsonPropertyName("len")]
			public int Length { get; set; }
			[JsonPropertyName("count")]
			public int Count { get; set; }
		}
		private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions {
			PropertyNamingPolicy = null,
			WriteIndented = false
		};
		internal static string Serialize(Subset subset) {
			// Serialized a subset into the xi-rope JSON payload shape.
		}
		internal static Subset Deserialize(string json) {
			// Materialized a subset from the xi-rope JSON payload.
		}
	}
}
namespace Xi.Core.Rope.Tree {
	internal static class LeafSplitter {
		internal const int NewlinePreferenceWindow = 64;
		internal static IEnumerable<string> Split(string leaf) {
			// Delegated to StringLeafOperations to generate appropriately sized leaves.
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
				// Stored the structural metadata, leaf payload, and child array reference.
			}
			public NodeBody Clone(Node[]? overrideChildren = null) {
				// Produced a shallow copy optionally substituting the child array reference.
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
				// Wrapped the immutable node body shared across rope instances.
			}
			public SharedNode EnsureUnique() {
				// Cloned the underlying body to guarantee writable ownership.
			}
			public SharedNode CloneWithChildren(IReadOnlyList<Node> newChildren) {
				// Rebuilt an internal body using a new child list and updated aggregates.
			}
			public SharedNode ReplaceChildRange(int index, int removeCount, IReadOnlyList<Node> replacements) {
				// Produced a new SharedNode with a spliced child range and refreshed aggregates.
			}
			public static SharedNode FromInternal(int height, IReadOnlyList<Node> children) {
				// Materialized an internal node from child references while computing metadata.
			}
			private static (int Length, RopeInfo Info, Node[] Array) MaterializeChildren(int parentHeight, IReadOnlyList<Node> children) {
				// Validated child heights, accumulated metrics, and produced a dense array copy.
			}
			private static (int Length, RopeInfo Info) AggregateChildren(int parentHeight, Node[] children) {
				// Recomputed length and RopeInfo by iterating children of a fixed height.
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
				// Reported the number of child nodes held by this internal node.
			}
		}
		public IReadOnlyList<Node> Children => Body.Children ?? Array.Empty<Node>();
		public ReadOnlySpan<char> LeafSpan => (Body.Leaf == null) ? ReadOnlySpan<char>.Empty : Body.Leaf.AsSpan();
		private Node(NodeBody body)
			: this(new SharedNode(body)) {
			// Wrapped a raw node body inside the shared-node container.
		}
		private Node(SharedNode shared) {
			// Captured the shared node handle for future structural operations.
		}
		private static Node FromShared(SharedNode shared) {
			// Constructed a Node façade around an existing SharedNode instance.
		}
		public static Node FromLeaf(string? text) {
			// Created a leaf node from raw text, computing rope metadata along the way.
		}
		public static Node Concat(Node left, Node right) {
			// Concatenated two nodes, balancing heights by delegating to helper paths.
		}
		public IEnumerable<Node> TraverseLeaves() {
			// Enumerated leaf nodes depth-first for inspection and diagnostics.
		}
		public Node Slice(int start, int length) {
			// Produced a sub-node representing a contiguous range of the original node.
		}
		public override string ToString() {
			// Materialized the rope node and its descendants into a single string.
		}
		public Node Insert(int start, string text) {
			// Inserted text at a position, falling back to structural rebuilds when needed.
		}
		public Node Delete(int start, int length) {
			// Removed a length of text, attempting localized edits before rebuilding.
		}
		public Node Replace(int start, int length, string? text) {
			// Replaced text within the node, using targeted edits or composite operations.
		}
		public Node EnsureWritableLeaf() {
			// Guaranteed a writable leaf by cloning shared strings when necessary.
		}
		public IReadOnlyList<Node> SplitLeafByBounds() {
			// Split oversized leaves into capacity-bounded segments using helper logic.
		}
		private bool TryInsertInSingleLeaf(int start, string text, out Node result, out IReadOnlyList<Node>? splitNodes) {
			// Attempted to mutate a single leaf in-place, cascading splits when capacity overflowed.
		}
		private bool TryDeleteInSingleSegment(int start, int length, out Node result) {
			// Attempted to delete within a single leaf or child segment, updating structure minimally.
		}
		private bool TryReplaceInSingleSegment(int start, int length, string text, out Node result, out IReadOnlyList<Node>? splitNodes) {
			// Tried to rewrite a single segment, handling splits, merges, and rebalancing as needed.
		}
		public Node CloneWithChildren(IReadOnlyList<Node> newChildren) {
			// Returned a new node sharing metadata but with substituted children.
		}
		public IReadOnlyList<string> CollectInvariantIssues(bool enforceLeafMinimum = false) {
			// Collected structural invariant violations for diagnostics and testing.
		}
		public void ValidateInvariants(bool enforceLeafMinimum = false) {
			// Threw an exception when invariant checks uncovered structural problems.
		}
		public Node NormalizeLeafMinimum() {
			// Iteratively resolved underfilled leaves until size invariants were satisfied.
		}
		private static bool TryParseInvariantPath(string issue, out int[] indices) {
			// Parsed an invariant error message into a navigation path for remediation.
		}
		private bool TryResolveLeafUnderflow(ReadOnlySpan<int> path, out Node updated) {
			// Walked a path toward an underflowing leaf and merged or rebalanced it.
		}
		private Node ReplaceChildWithSegments(Node[] children, int index, IReadOnlyList<Node> segments) {
			// Replaced a single child with multiple segments and rebuilt node metadata.
		}
		public Node WithChildReplaced(int index, Node newChild) {
			// Produced a node with a single child replaced while keeping other children intact.
		}
		public (Node Left, Node Right) SplitAt(int index) {
			// Split the node into two parts around the specified index, recursing as needed.
		}
		private static Node ConcatLeftShorter(Node left, Node right) {
			// Balanced concatenation when the left operand was shorter than the right.
		}
		private static Node ConcatRightShorter(Node left, Node right) {
			// Balanced concatenation when the right operand was shorter than the left.
		}
		private Node[] RequireChildren() {
			// Retrieved the internal node's child array, asserting presence of children.
		}
		private static string GetLeafText(Node node) {
			// Exposed the string payload stored in a leaf node.
		}
		private bool TryMergeLeafWithSibling(Node[] children, int index, Node replacement, out Node result) {
			// Attempted to merge a small leaf with adjacent siblings to satisfy capacity rules.
		}
		private bool TryRebalanceLeafWithSibling(Node[] children, int index, Node replacement, out Node result) {
			// Redistributed characters with neighbors to bring an underflowing leaf back in range.
		}
		private bool TryRebalancePair(Node[] children, int firstIndex, int secondIndex, Node first, Node second, out Node result) {
			// Rebuilt a neighboring leaf pair with balanced splits to enforce capacity constraints.
		}
		private static void ValidateNode(Node node, bool isRoot, bool enforceLeafMinimum, List<string> issues, string path) {
			// Validated tree invariants recursively, emitting issues for diagnostics.
		}
		private static string FormatLeafPreview(Node node) {
			// Generated a truncated, escaped preview of a leaf's contents for logging.
		}
		private static string EscapePreview(string text) {
			// Escaped control characters to keep previews readable in diagnostics.
		}
		private static string SummarizeChildren(Node[] children) {
			// Produced a compact textual summary of child lengths for diagnostics.
		}
		private Node BuildMergedNode(Node[] children, int firstIndex, int secondIndex, Node mergedLeaf) {
			// Replaced a consecutive child range with a merged leaf and rebuilt the node.
		}
		private static Node CreateInternal(int height, IReadOnlyList<Node> children) {
			// Created an internal node from children while computing aggregate metadata.
		}
		private static Node BuildFromSegments(List<Node> segments) {
			// Folded a list of node segments into a balanced tree via TreeBuilder.
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
				// Provided access to the leaf payload when the node represented a leaf.
			}
		}
		public IReadOnlyList<Node<TInfo, TLeaf, TLeafOps>> Children => _body.Children ?? Array.Empty<Node<TInfo, TLeaf, TLeafOps>>();
		private Node(NodeBody body) {
			// Captured the provided node body in the original implementation.
		}
		public static Node<TInfo, TLeaf, TLeafOps> FromLeaf(TLeaf leaf) {
			// Constructed a leaf node using leaf operations to populate metadata.
		}
		public static Node<TInfo, TLeaf, TLeafOps> CreateInternal(IReadOnlyList<Node<TInfo, TLeaf, TLeafOps>> children) {
			// Built an internal node from homogeneous children, aggregating metadata generically.
		}
		public IEnumerable<Node<TInfo, TLeaf, TLeafOps>> TraverseLeaves() {
			// Enumerated generic leaf nodes recursively.
		}
	}
	public sealed class NodeCursor {
		public Node Root { get; }
		public int Position { get; private set; }
		public int TotalLength => Root.Length;
		public NodeCursor(Node root, int position) {
			// Intended to couple a traversal cursor with a rope root and position.
		}
		public (string Leaf, int Offset)? GetLeaf() {
			// Would expose the current leaf text and offset under the cursor.
		}
		public void SetPosition(int position) {
			// Planned to update the cursor position within the rope.
		}
		public bool IsBoundary(IMetric metric) {
			// Intended to report whether the cursor is on a metric boundary.
		}
		public int? MoveToPrevious(IMetric metric) {
			// Would move the cursor to the previous boundary according to a metric.
		}
		public int? MoveToNext(IMetric metric) {
			// Would move the cursor to the next boundary according to a metric.
		}
		public int? AtOrNext(IMetric metric) {
			// Intended to snap to the current or next boundary defined by the metric.
		}
		public int? AtOrPrevious(IMetric metric) {
			// Intended to snap to the current or previous boundary defined by the metric.
		}
	}
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	internal readonly struct StringLeafOperations : ILeafOperations<string> {
		public static int MinLeafSize => 511;
		public static int MaxLeafSize => 1024;
		public static string Empty => string.Empty;
		public static int NewlinePreferenceWindow => 64;
		public static int GetLength(string leaf) {
			// Reported the UTF-16 length of the string leaf.
		}
		public static bool IsValidChild(string leaf) {
			// Determined whether a string leaf met the minimum capacity requirement.
		}
		public static string Clone(string leaf) {
			// Produced a new string copy to break sharing of the original leaf.
		}
		public static string Insert(string leaf, int index, string text) {
			// Inserted text into a string leaf while preserving UTF-16 ordering.
		}
		public static string RemoveRange(string leaf, int index, int length) {
			// Removed a substring from the leaf and returned the compacted result.
		}
		public static string ReplaceRange(string leaf, int index, int length, string replacement) {
			// Replaced a substring with new text, combining insertion and removal semantics.
		}
		public static string Merge(string left, string right) {
			// Concatenated two string leaves while preserving order.
		}
		public static bool TryComputeBalancedSplit(string left, string right, out string newLeft, out string newRight) {
			// Calculated balanced split strings ensuring size and surrogate constraints.
		}
		public static IEnumerable<string> SplitByCapacity(string leaf) {
			// Yielded capacity-aware substrings, preferring newline boundaries when splitting.
		}
		private static int PreferNewlineBoundary(string left, string right, int candidate, int minSplit, int maxSplit) {
			// Nudged the split toward a nearby newline when possible.
		}
		private static bool TryEnsureSurrogateBoundary(string left, string right, ref int splitIndex, int minSplit, int maxSplit) {
			// Adjusted split positions to avoid breaking surrogate pairs.
		}
		private static bool IsSafeBoundary(string left, string right, int index) {
			// Checked whether a split index avoided bisecting surrogate pairs.
		}
		private static string CreateCombinedSegment(string left, string right, int start, int length) {
			// Built a substring spanning the virtual concatenation of left and right.
		}
		private static char GetCombinedChar(string left, string right, int index) {
			// Accessed a character from the conceptual concatenation of two strings.
		}
	}
	public sealed class TreeBuilder {
		private readonly List<Node> _pending = new List<Node>();
		public void PushString(string? text) {
			// Split text into leaf-sized segments and appended them as nodes.
		}
		public void PushSpan(ReadOnlySpan<char> span) {
			// Accepted a span and forwarded it through the string-based push path.
		}
		public void PushNode(Node node) {
			// Added an existing node to the builder, skipping empties.
		}
		public Node Build() {
			// Reduced pending nodes into a balanced rope via concatenation.
		}
		public void Reset() {
			// Cleared all accumulated nodes to reuse the builder.
		}
		private void AppendNode(Node node) {
			// Maintained a height-sorted pending list by merging nodes eagerly.
		}
		private static IEnumerable<string> SplitIntoLeaves(string text) {
			// Delegated to LeafSplitter for capacity-aware segmentation.
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
