
// E:\repos\Atelia-org\xi-editor-sharp\src\xi.Core\bin\Debug\net9.0\xi.Core.dll
// xi.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// Global type: <Module>
// Architecture: AnyCPU (64-bit preferred)
// Runtime: v4.0.30319
// This assembly was compiled using the /deterministic option.
// Hash algorithm: SHA1
// Debug info: Loaded from portable PDB: E:\repos\Atelia-org\xi-editor-sharp\src\xi.Core\bin\Debug\net9.0\xi.Core.pdb
#define DEBUG
using Xi.Core.Rope.Tree;
[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.EnableEditAndContinue)]
[assembly: InternalsVisibleTo("xi.Core.Tests")]
[assembly: TargetFramework(".NETCoreApp,Version=v9.0", FrameworkDisplayName = ".NET 9.0")]
[assembly: AssemblyCompany("xi.Core")]
[assembly: AssemblyConfiguration("Debug")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0+e37fa2d30e7de220efd3a78bb28fbeba4d462f92")]
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
		public void Append(string? text) {/* body removed for skeleton view. */}
		public void Append(ReadOnlySpan<char> text) {/* body removed for skeleton view. */}
		public void Clear() {/* body removed for skeleton view. */}
		public void Replace(int start, int length, string? text) {/* body removed for skeleton view. */}
		public string Snapshot() {/* body removed for skeleton view. */}
		public string GetSlice(int start, int length) {/* body removed for skeleton view. */}
		public override string ToString() {/* body removed for skeleton view. */}
		private static void ValidateRange(int start, int length, int totalLength) {/* body removed for skeleton view. */}
	}
}
namespace Xi.Core.Rope {
	internal static class BreaksMetricHelper {
		public static int GetNthBreakOffset(ReadOnlySpan<int> breaks, int leafLength, int measuredUnits) {/* body removed for skeleton view. */}
		public static int CountBreaksUpTo(ReadOnlySpan<int> breaks, int offset) {/* body removed for skeleton view. */}
		public static int? FindPreviousBreak(ReadOnlySpan<int> breaks, int offset) {/* body removed for skeleton view. */}
		public static int? FindNextBreak(ReadOnlySpan<int> breaks, int offset) {/* body removed for skeleton view. */}
		public static bool IsBreakBoundary(ReadOnlySpan<int> breaks, int offset) {/* body removed for skeleton view. */}
	}
	internal enum DeltaElementKind {
		Copy,
		Insert
	}
	internal readonly struct CopyElement {
		public int Start { get; }
		public int End { get; }
		public int Length => End - Start;
		public CopyElement(int start, int end) {/* body removed for skeleton view. */}
	}
	internal readonly struct InsertElement<TLeaf> where TLeaf : class {
		public TLeaf Value { get; }
		public int Length { get; }
		public InsertElement(TLeaf value, int length) {/* body removed for skeleton view. */}
	}
	internal readonly struct DeltaElement<TLeaf> where TLeaf : class {
		private readonly DeltaElementKind _kind;
		private readonly CopyElement _copy;
		private readonly InsertElement<TLeaf> _insert;
		internal DeltaElementKind Kind => _kind;
		internal bool IsCopy => _kind == DeltaElementKind.Copy;
		internal bool IsInsert => _kind == DeltaElementKind.Insert;
		private DeltaElement(CopyElement copy) {/* body removed for skeleton view. */}
		private DeltaElement(InsertElement<TLeaf> insert) {/* body removed for skeleton view. */}
		internal static DeltaElement<TLeaf> Copy(int start, int end) {/* body removed for skeleton view. */}
		internal static DeltaElement<TLeaf> Insert(TLeaf value, int? length = null) {/* body removed for skeleton view. */}
		private static int ResolveInsertLength(TLeaf value) {/* body removed for skeleton view. */}
		internal CopyElement AsCopy() {/* body removed for skeleton view. */}
		internal InsertElement<TLeaf> AsInsert() {/* body removed for skeleton view. */}
	}
	internal sealed class Delta<TInfo, TLeaf> where TLeaf : class {
		private readonly List<DeltaElement<TLeaf>> _elements;
		internal int BaseLength { get; }
		internal int ElementCount => _elements.Count;
		internal IReadOnlyList<DeltaElement<TLeaf>> Elements => _elements;
		private Delta(List<DeltaElement<TLeaf>> elements, int baseLength) {/* body removed for skeleton view. */}
		internal IEnumerable<DeltaElement<TLeaf>> EnumerateElements() {/* body removed for skeleton view. */}
		internal IEnumerable<(bool IsInsert, int Start, int End)> EnumerateElementTriples() {/* body removed for skeleton view. */}
		internal static Delta<TInfo, TLeaf> FromElements(int baseLength, IEnumerable<DeltaElement<TLeaf>> elements) {/* body removed for skeleton view. */}
		internal static Delta<TInfo, TLeaf> FromElements(int baseLength, IEnumerable<(int? CopyStart, int? CopyEnd, TLeaf? Insert)> elementTuples) {/* body removed for skeleton view. */}
		internal (Delta<TInfo, TLeaf> InsertDelta, Subset DeletedSubset) Factor() {/* body removed for skeleton view. */}
		private static void ValidateBaseLength(int baseLength, int maxCopyEnd) {/* body removed for skeleton view. */}
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
		internal static string Serialize<TInfo>(Delta<TInfo, string> delta) {/* body removed for skeleton view. */}
		internal static Delta<TInfo, string> Deserialize<TInfo>(string json) {/* body removed for skeleton view. */}
	}
	internal readonly struct RevId {
		public long Session1 { get; }
		public int Session2 { get; }
		public int Number { get; }
		public RevId(long session1, int session2, int number) {/* body removed for skeleton view. */}
	}
	internal enum RevisionOperationKind {
		Edit,
		Undo
	}
	internal abstract class RevisionOperation {
		internal abstract RevisionOperationKind Kind { get; }
		internal bool IsEdit => Kind == RevisionOperationKind.Edit;
		internal bool IsUndo => Kind == RevisionOperationKind.Undo;
		internal RevisionEdit AsEdit() {/* body removed for skeleton view. */}
		internal RevisionUndo AsUndo() {/* body removed for skeleton view. */}
	}
	internal sealed class RevisionEdit : RevisionOperation {
		internal override RevisionOperationKind Kind => RevisionOperationKind.Edit;
		internal int Priority { get; }
		internal int UndoGroup { get; }
		internal Subset Inserts { get; }
		internal Subset Deletes { get; }
		internal RevisionEdit(int priority, int undoGroup, Subset inserts, Subset deletes) {/* body removed for skeleton view. */}
	}
	internal sealed class RevisionUndo : RevisionOperation {
		private readonly int[] _toggledGroups;
		private readonly IReadOnlyList<int> _toggledGroupsView;
		internal override RevisionOperationKind Kind => RevisionOperationKind.Undo;
		internal IReadOnlyList<int> ToggledGroups => _toggledGroupsView;
		internal Subset DeletesBitxor { get; }
		internal RevisionUndo(IEnumerable<int> toggledGroups, Subset deletesBitxor) {/* body removed for skeleton view. */}
		private static int[] CopyGroups(IEnumerable<int> groups) {/* body removed for skeleton view. */}
	}
	internal sealed class Revision {
		internal RevId RevId { get; }
		internal int MaxUndoSoFar { get; }
		internal RevisionOperation Operation { get; }
		internal Revision(RevId revId, int maxUndoSoFar, RevisionOperation operation) {/* body removed for skeleton view. */}
	}
	internal sealed class Engine {
		private readonly string _text;
		private readonly string _tombstones;
		private readonly Subset _deletesFromUnion;
		private readonly int[] _undoneGroups;
		private readonly Revision[] _revisions;
		private readonly IReadOnlyList<int> _undoneGroupsView;
		private readonly IReadOnlyList<Revision> _revisionLogView;
		private Engine(string text, string tombstones, Subset deletesFromUnion, int[] undoneGroups, Revision[] revisions) {/* body removed for skeleton view. */}
		internal static Engine FromSerializedState(string text, string tombstones, Subset deletesFromUnion, IEnumerable<int> undoneGroups, IEnumerable<Revision> revisions) {/* body removed for skeleton view. */}
		private static int[] CopyGroups(IEnumerable<int> source) {/* body removed for skeleton view. */}
		private static Revision[] CopyRevisions(IEnumerable<Revision> source) {/* body removed for skeleton view. */}
		internal string TextSnapshot() {/* body removed for skeleton view. */}
		internal string TombstonesSnapshot() {/* body removed for skeleton view. */}
		internal Subset DeletesFromUnionSnapshot() {/* body removed for skeleton view. */}
		internal IReadOnlyList<int> UndoneGroupsSnapshot() {/* body removed for skeleton view. */}
		internal IReadOnlyList<Revision> RevisionLog() {/* body removed for skeleton view. */}
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
		internal static string Serialize(Engine engine) {/* body removed for skeleton view. */}
		internal static Engine Deserialize(string json) {/* body removed for skeleton view. */}
		private static int[] CopyGroups(IReadOnlyList<int> groups) {/* body removed for skeleton view. */}
		private static int[] ValidateGroups(int[] groups, string fieldName) {/* body removed for skeleton view. */}
		private static RevisionDto[] ToRevisionDtos(IReadOnlyList<Revision> revisions) {/* body removed for skeleton view. */}
		private static RevisionDto ToRevisionDto(Revision revision) {/* body removed for skeleton view. */}
		private static EditPayloadDto ToEditPayloadDto(RevisionEdit edit) {/* body removed for skeleton view. */}
		private static UndoPayloadDto ToUndoPayloadDto(RevisionUndo undo) {/* body removed for skeleton view. */}
		private static SubsetDto ToSubsetDto(Subset subset) {/* body removed for skeleton view. */}
		private static Subset FromSubsetDto(SubsetDto dto) {/* body removed for skeleton view. */}
	}
	public interface IMetric : ITreeMetric<string, RopeInfo> {
	}
	public readonly struct Interval : IEquatable<Interval> {
		public int Start { get; }
		public int End { get; }
		public int Length => End - Start;
		public bool IsEmpty => Start == End;
		public static Interval Empty => new Interval(0, 0);
		public Interval(int start, int end) {/* body removed for skeleton view. */}
		public bool Contains(int position) {/* body removed for skeleton view. */}
		public Interval Intersect(Interval other) {/* body removed for skeleton view. */}
		public Interval Union(Interval other) {/* body removed for skeleton view. */}
		public Interval Translate(int delta) {/* body removed for skeleton view. */}
		public static Interval EmptyAt(int position) {/* body removed for skeleton view. */}
		public bool Equals(Interval other) {/* body removed for skeleton view. */}
		public override bool Equals(object? obj) {/* body removed for skeleton view. */}
		public override int GetHashCode() {/* body removed for skeleton view. */}
		public override string ToString() {/* body removed for skeleton view. */}
		public static bool operator ==(Interval left, Interval right) {/* body removed for skeleton view. */}
		public static bool operator !=(Interval left, Interval right) {/* body removed for skeleton view. */}
	}
	public sealed class BaseMetric : IMetric, ITreeMetric<string, RopeInfo> {
		public static BaseMetric Instance { get; } = new BaseMetric();
		public bool CanFragment => false;
		private BaseMetric() {/* body removed for skeleton view. */}
		public int Measure(RopeInfo info, int nodeLength) {/* body removed for skeleton view. */}
		public int ToBaseUnits(string leaf, int measuredUnits) {/* body removed for skeleton view. */}
		public int FromBaseUnits(string leaf, int baseUnits) {/* body removed for skeleton view. */}
		public bool IsBoundary(string leaf, int offset) {/* body removed for skeleton view. */}
		public int? GetPreviousBoundary(string leaf, int offset) {/* body removed for skeleton view. */}
		public int? GetNextBoundary(string leaf, int offset) {/* body removed for skeleton view. */}
	}
	public sealed class LinesMetric : IMetric, ITreeMetric<string, RopeInfo> {
		public static LinesMetric Instance { get; } = new LinesMetric();
		public bool CanFragment => true;
		private LinesMetric() {/* body removed for skeleton view. */}
		public int Measure(RopeInfo info, int nodeLength) {/* body removed for skeleton view. */}
		public int ToBaseUnits(string leaf, int measuredUnits) {/* body removed for skeleton view. */}
		public int FromBaseUnits(string leaf, int baseUnits) {/* body removed for skeleton view. */}
		public bool IsBoundary(string leaf, int offset) {/* body removed for skeleton view. */}
		public int? GetPreviousBoundary(string leaf, int offset) {/* body removed for skeleton view. */}
		public int? GetNextBoundary(string leaf, int offset) {/* body removed for skeleton view. */}
	}
	public sealed class Utf16Metric : IMetric, ITreeMetric<string, RopeInfo> {
		public static Utf16Metric Instance { get; } = new Utf16Metric();
		public bool CanFragment => false;
		private Utf16Metric() {/* body removed for skeleton view. */}
		public int Measure(RopeInfo info, int nodeLength) {/* body removed for skeleton view. */}
		public int ToBaseUnits(string leaf, int measuredUnits) {/* body removed for skeleton view. */}
		public int FromBaseUnits(string leaf, int baseUnits) {/* body removed for skeleton view. */}
		public bool IsBoundary(string leaf, int offset) {/* body removed for skeleton view. */}
		public int? GetPreviousBoundary(string leaf, int offset) {/* body removed for skeleton view. */}
		public int? GetNextBoundary(string leaf, int offset) {/* body removed for skeleton view. */}
	}
	internal static class Utf16BoundaryHelper {
		public static bool IsBoundary(ReadOnlySpan<char> leaf, int offset) {/* body removed for skeleton view. */}
		public static int? GetPreviousBoundary(ReadOnlySpan<char> leaf, int offset) {/* body removed for skeleton view. */}
		public static int? GetNextBoundary(ReadOnlySpan<char> leaf, int offset) {/* body removed for skeleton view. */}
	}
	public sealed class Rope : ITextBuffer {
		private Node _root = Node.Empty;
		private long _editVersion;
		public int Length => _root.Length;
		internal Node DebugRoot => _root;
		public long EditVersion => Interlocked.Read(in _editVersion);
		public RopeChunkEnumerator EnumerateChunks(RopeChunkEnumeratorDiagnostics? diagnostics = null) {/* body removed for skeleton view. */}
		public RopeLineEnumerator EnumerateLines() {/* body removed for skeleton view. */}
		public void Append(string? text) {/* body removed for skeleton view. */}
		public void Append(ReadOnlySpan<char> text) {/* body removed for skeleton view. */}
		internal static Rope FromNode(Node root) {/* body removed for skeleton view. */}
		public void Clear() {/* body removed for skeleton view. */}
		public void Replace(int start, int length, string? text) {/* body removed for skeleton view. */}
		public string Snapshot() {/* body removed for skeleton view. */}
		public string GetSlice(int start, int length) {/* body removed for skeleton view. */}
		public int ConvertLinesFromBytes(int offset) {/* body removed for skeleton view. */}
		public int ConvertBytesFromLines(int line) {/* body removed for skeleton view. */}
		public int ConvertUtf16FromBytes(int offset) {/* body removed for skeleton view. */}
		public int ConvertBytesFromUtf16(int units) {/* body removed for skeleton view. */}
		private static void ValidateRange(int start, int length, int totalLength) {/* body removed for skeleton view. */}
		private static void ValidateOffset(int offset, int totalLength, string parameterName) {/* body removed for skeleton view. */}
		private static void ValidateMetricCoordinate(int value, int maxInclusive, string parameterName) {/* body removed for skeleton view. */}
		private void UpdateRoot(Node newRoot) {/* body removed for skeleton view. */}
		private void BumpEditVersion() {/* body removed for skeleton view. */}
	}
	public ref struct RopeChunkEnumerator {
		private readonly Rope _rope;
		private readonly NodeCursor? _cursor;
		private readonly RopeChunkEnumeratorDiagnostics? _diagnostics;
		private int _nextOffset;
		private bool _completed;
		private bool _emittedEmptyChunk;
		private ReadOnlyMemory<char> _current;
		public ReadOnlyMemory<char> Current => _current;
		internal RopeChunkEnumerator(Rope rope, RopeChunkEnumeratorDiagnostics? diagnostics = null) {/* body removed for skeleton view. */}
		public RopeChunkEnumerator GetEnumerator() {/* body removed for skeleton view. */}
		public bool MoveNext() {/* body removed for skeleton view. */}
		private static ReadOnlyMemory<char> CloneLeaf(string leaf) {/* body removed for skeleton view. */}
	}
	public sealed class RopeChunkEnumeratorDiagnostics {
		public int ChunkCount { get; private set; }
		public int MaxChunkLength { get; private set; }
		public long TotalUtf16CharCount { get; private set; }
		public void Reset() {/* body removed for skeleton view. */}
		internal void RecordChunk(int chunkLength) {/* body removed for skeleton view. */}
	}
	public readonly struct RopeInfo : ITreeNodeInfo<RopeInfo, string>, IDefaultMetricProvider<RopeInfo, string, BaseMetric> {
		public int LineCount { get; }
		public int Utf16Length { get; }
		public static RopeInfo Identity => new RopeInfo(0, 0);
		static BaseMetric IDefaultMetricProvider<RopeInfo, string, BaseMetric>.DefaultMetric => BaseMetric.Instance;
		private RopeInfo(int lineCount, int utf16Length) {/* body removed for skeleton view. */}
		public static RopeInfo FromLeaf(ReadOnlySpan<char> span) {/* body removed for skeleton view. */}
		public static RopeInfo FromLeaf(string leaf) {/* body removed for skeleton view. */}
		public RopeInfo Accumulate(RopeInfo other) {/* body removed for skeleton view. */}
		public Interval IntervalForPrefix(int prefixLength) {/* body removed for skeleton view. */}
		private static (int lines, int utf16) AnalyzeSpan(ReadOnlySpan<char> span) {/* body removed for skeleton view. */}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int AddLength(int baseLength) {/* body removed for skeleton view. */}
		static RopeInfo ITreeNodeInfo<RopeInfo, string>.FromLeaf(string leaf) {/* body removed for skeleton view. */}
	}
	public ref struct RopeLineEnumerator {
		private readonly bool _ropeWasEmpty;
		private RopeChunkEnumerator _chunks;
		private Queue<ReadOnlyMemory<char>>? _pendingLines;
		private StringBuilder? _builder;
		private ReadOnlyMemory<char> _current;
		private bool _pendingCarriageReturn;
		private bool _completed;
		private bool _emittedAnyLine;
		public ReadOnlyMemory<char> Current => _current;
		internal RopeLineEnumerator(Rope rope) {/* body removed for skeleton view. */}
		public RopeLineEnumerator GetEnumerator() {/* body removed for skeleton view. */}
		public bool MoveNext() {/* body removed for skeleton view. */}
		private void ProcessChunk(ReadOnlySpan<char> chunk) {/* body removed for skeleton view. */}
		private void CompleteDeferredCarriageReturn(ReadOnlySpan<char> nextChunk, ref int index) {/* body removed for skeleton view. */}
		private void AppendSlice(ReadOnlySpan<char> slice) {/* body removed for skeleton view. */}
		private void AppendLiteral(string literal) {/* body removed for skeleton view. */}
		private void EmitLine() {/* body removed for skeleton view. */}
		private void EnqueueLine(string text) {/* body removed for skeleton view. */}
		private bool TryDequeueLine(out ReadOnlyMemory<char> line) {/* body removed for skeleton view. */}
		private StringBuilder EnsureBuilder() {/* body removed for skeleton view. */}
	}
	internal readonly struct SubsetSegment {
		public int Length { get; }
		public int Count { get; }
		public SubsetSegment(int length, int count) {/* body removed for skeleton view. */}
	}
	internal sealed class Subset {
		private readonly SubsetSegment[] _segments;
		private readonly int _totalLength;
		internal static Subset Empty { get; } = new Subset(Array.Empty<SubsetSegment>(), 0);
		internal int SegmentCount => _segments.Length;
		internal bool IsEmpty => _segments.Length == 0 || (_segments.Length == 1 && _segments[0].Count == 0);
		internal int Length => _totalLength;
		private Subset(SubsetSegment[] segments, int totalLength) {/* body removed for skeleton view. */}
		internal static Subset Create(SubsetSegment[] segments, int totalLength) {/* body removed for skeleton view. */}
		internal int LengthAfterDelete() {/* body removed for skeleton view. */}
		internal IEnumerable<(int Start, int Length, int Count)> SegmentTriples() {/* body removed for skeleton view. */}
		internal static Subset FromSegmentTriples(IEnumerable<(int Start, int Length, int Count)> triples) {/* body removed for skeleton view. */}
	}
	internal sealed class SubsetBuilder {
		private readonly List<SubsetSegment> _segments = new List<SubsetSegment>();
		private int _totalLength;
		internal void PadToLength(int totalLength) {/* body removed for skeleton view. */}
		internal void AddRange(int begin, int end, int count) {/* body removed for skeleton view. */}
		internal void PushSegment(int length, int count) {/* body removed for skeleton view. */}
		private void PushSegmentInternal(int length, int count) {/* body removed for skeleton view. */}
		internal Subset Build() {/* body removed for skeleton view. */}
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
		internal static string Serialize(Subset subset) {/* body removed for skeleton view. */}
		internal static Subset Deserialize(string json) {/* body removed for skeleton view. */}
	}
}
namespace Xi.Core.Rope.Tree {
	public sealed class CursorDescriptor {
		private static readonly CursorDescriptorFrame[] s_emptyFrames = Array.Empty<CursorDescriptorFrame>();
		private readonly CursorDescriptorFrame[] _frames;
		public bool IsValid { get; }
		public int Position { get; }
		public int OffsetOfLeaf { get; }
		public int? LeafLength { get; }
		internal Node? LeafNode { get; }
		public IReadOnlyList<CursorDescriptorFrame> Frames => _frames;
		private CursorDescriptor(bool isValid, int position, int offsetOfLeaf, int? leafLength, Node? leafNode, CursorDescriptorFrame[] frames) {/* body removed for skeleton view. */}
		internal static CursorDescriptor CreateInvalid(int position) {/* body removed for skeleton view. */}
		internal static CursorDescriptor CreateValid(int position, int offsetOfLeaf, Node leafNode, IReadOnlyList<CursorDescriptorFrame> frames) {/* body removed for skeleton view. */}
		public NodeCursor? TryRestore(Rope rope) {/* body removed for skeleton view. */}
	}
	public sealed class CursorDescriptorFrame {
		public int NodeHeight { get; }
		public int NodeLength { get; }
		public int ChildIndex { get; }
		public int ChildOffset { get; }
		internal Node Node { get; }
		internal CursorDescriptorFrame(Node node, int childIndex, int childOffset) {/* body removed for skeleton view. */}
	}
	public sealed class GenericTreeBuilder<TInfo, TLeaf, TLeafOps> where TInfo : struct, ITreeNodeInfo<TInfo, TLeaf> where TLeafOps : ILeafOperations<TLeaf> {
		private readonly List<Node<TInfo, TLeaf, TLeafOps>> _pending = new List<Node<TInfo, TLeaf, TLeafOps>>();
		public void PushString(string? text) {/* body removed for skeleton view. */}
		public void PushNode(Node<TInfo, TLeaf, TLeafOps> node) {/* body removed for skeleton view. */}
		public Node<TInfo, TLeaf, TLeafOps> Build() {/* body removed for skeleton view. */}
		public void Reset() {/* body removed for skeleton view. */}
		private void AppendNode(Node<TInfo, TLeaf, TLeafOps> node) {/* body removed for skeleton view. */}
		private static Node<TInfo, TLeaf, TLeafOps> Concat(Node<TInfo, TLeaf, TLeafOps> left, Node<TInfo, TLeaf, TLeafOps> right) {/* body removed for skeleton view. */}
		private static Node<TInfo, TLeaf, TLeafOps> ConcatLeftShorter(Node<TInfo, TLeaf, TLeafOps> left, Node<TInfo, TLeaf, TLeafOps> right) {/* body removed for skeleton view. */}
		private static Node<TInfo, TLeaf, TLeafOps> ConcatRightShorter(Node<TInfo, TLeaf, TLeafOps> left, Node<TInfo, TLeaf, TLeafOps> right) {/* body removed for skeleton view. */}
		private static TLeaf ConvertStringToLeaf(string segment) {/* body removed for skeleton view. */}
	}
	internal static class LeafSplitter {
		internal static readonly int NewlinePreferenceWindow = StringLeafOperations.MaxLeafSize - StringLeafOperations.MinLeafSize;
		internal static IEnumerable<string> Split(string leaf) {/* body removed for skeleton view. */}
	}
	public sealed class Node {
		private sealed class NodeBody {
			public int Height { get; }
			public int Length { get; }
			public RopeInfo Info { get; }
			public string? Leaf { get; }
			public Node[]? Children { get; }
			public NodeBody(int height, int length, RopeInfo info, string? leaf, Node[]? children) {/* body removed for skeleton view. */}
			public NodeBody Clone(Node[]? overrideChildren = null) {/* body removed for skeleton view. */}
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
			public SharedNode(NodeBody body) {/* body removed for skeleton view. */}
			public SharedNode EnsureUnique() {/* body removed for skeleton view. */}
			public SharedNode CloneWithChildren(IReadOnlyList<Node> newChildren) {/* body removed for skeleton view. */}
			public SharedNode ReplaceChildRange(int index, int removeCount, IReadOnlyList<Node> replacements) {/* body removed for skeleton view. */}
			public static SharedNode FromInternal(int height, IReadOnlyList<Node> children) {/* body removed for skeleton view. */}
			private static (int Length, RopeInfo Info, Node[] Array) MaterializeChildren(int parentHeight, IReadOnlyList<Node> children) {/* body removed for skeleton view. */}
			private static (int Length, RopeInfo Info) AggregateChildren(int parentHeight, Node[] children) {/* body removed for skeleton view. */}
		}
		private const int MinChildren = 4;
		private const int MaxChildren = 8;
		private readonly SharedNode _shared;
		internal static int MaxChildCount => 8;
		internal static int MinChildCount => 4;
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
			get {/* body removed for skeleton view. */}
		}
		public IReadOnlyList<Node> Children => Body.Children ?? Array.Empty<Node>();
		public ReadOnlySpan<char> LeafSpan => (Body.Leaf == null) ? ReadOnlySpan<char>.Empty : Body.Leaf.AsSpan();
		private Node(NodeBody body)
			: this(new SharedNode(body)) {/* body removed for skeleton view. */}
		private Node(SharedNode shared) {/* body removed for skeleton view. */}
		private static Node FromShared(SharedNode shared) {/* body removed for skeleton view. */}
		internal bool IsOkChild() {/* body removed for skeleton view. */}
		public string? GetLeaf() {/* body removed for skeleton view. */}
		public Node[]? GetChildren() {/* body removed for skeleton view. */}
		internal int ConvertFromDefaultMetric(IMetric metric, int offset) {/* body removed for skeleton view. */}
		internal int ConvertToDefaultMetric(IMetric metric, int value) {/* body removed for skeleton view. */}
		private static int ConvertMetrics(Node node, int value, IMetric fromMetric, IMetric toMetric) {/* body removed for skeleton view. */}
		public static Node FromLeaf(string? text) {/* body removed for skeleton view. */}
		public static Node Concat(Node left, Node right) {/* body removed for skeleton view. */}
		public IEnumerable<Node> TraverseLeaves() {/* body removed for skeleton view. */}
		public Node Slice(int start, int length) {/* body removed for skeleton view. */}
		public override string ToString() {/* body removed for skeleton view. */}
		public Node Insert(int start, string text) {/* body removed for skeleton view. */}
		public Node Delete(int start, int length) {/* body removed for skeleton view. */}
		public Node Replace(int start, int length, string? text) {/* body removed for skeleton view. */}
		public Node EnsureWritableLeaf() {/* body removed for skeleton view. */}
		public IReadOnlyList<Node> SplitLeafByBounds() {/* body removed for skeleton view. */}
		private bool TryInsertInSingleLeaf(int start, string text, out Node result, out IReadOnlyList<Node>? splitNodes) {/* body removed for skeleton view. */}
		private bool TryDeleteInSingleSegment(int start, int length, out Node result) {/* body removed for skeleton view. */}
		private bool TryReplaceInSingleSegment(int start, int length, string text, out Node result, out IReadOnlyList<Node>? splitNodes) {/* body removed for skeleton view. */}
		public Node CloneWithChildren(IReadOnlyList<Node> newChildren) {/* body removed for skeleton view. */}
		public IReadOnlyList<string> CollectInvariantIssues(bool enforceLeafMinimum = false) {/* body removed for skeleton view. */}
		public void ValidateInvariants(bool enforceLeafMinimum = false) {/* body removed for skeleton view. */}
		public Node NormalizeLeafMinimum() {/* body removed for skeleton view. */}
		private static bool TryParseInvariantPath(string issue, out int[] indices) {/* body removed for skeleton view. */}
		private bool TryResolveLeafUnderflow(ReadOnlySpan<int> path, out Node updated) {/* body removed for skeleton view. */}
		private Node ReplaceChildWithSegments(Node[] children, int index, IReadOnlyList<Node> segments) {/* body removed for skeleton view. */}
		public Node WithChildReplaced(int index, Node newChild) {/* body removed for skeleton view. */}
		public (Node Left, Node Right) SplitAt(int index) {/* body removed for skeleton view. */}
		private Node[] RequireChildren() {/* body removed for skeleton view. */}
		private static Node MergeLeaves(Node left, Node right) {/* body removed for skeleton view. */}
		private static Node MergeNodes(IReadOnlyList<Node> leftChildren, IReadOnlyList<Node> rightChildren) {/* body removed for skeleton view. */}
		internal static Node FromNodes(IReadOnlyList<Node> children) {/* body removed for skeleton view. */}
		private static Node[] CopyRange(IReadOnlyList<Node> source, int start, int length) {/* body removed for skeleton view. */}
		private static Node[] CombineChildren(IReadOnlyList<Node>? leftChildren, IReadOnlyList<Node>? rightChildren) {/* body removed for skeleton view. */}
		private static string GetLeafText(Node node) {/* body removed for skeleton view. */}
		private bool TryMergeLeafWithSibling(Node[] children, int index, Node replacement, out Node result) {/* body removed for skeleton view. */}
		private bool TryRebalanceLeafWithSibling(Node[] children, int index, Node replacement, out Node result) {/* body removed for skeleton view. */}
		private bool TryRebalancePair(Node[] children, int firstIndex, int secondIndex, Node first, Node second, out Node result) {/* body removed for skeleton view. */}
		private static void ValidateNode(Node node, bool isRoot, bool enforceLeafMinimum, List<string> issues, string path) {/* body removed for skeleton view. */}
		private static string FormatLeafPreview(Node node) {/* body removed for skeleton view. */}
		private static string EscapePreview(string text) {/* body removed for skeleton view. */}
		private static string SummarizeChildren(Node[] children) {/* body removed for skeleton view. */}
		private Node BuildMergedNode(Node[] children, int firstIndex, int secondIndex, Node mergedLeaf) {/* body removed for skeleton view. */}
		private static Node CreateInternal(int height, IReadOnlyList<Node> children) {/* body removed for skeleton view. */}
		private static Node BuildFromSegments(List<Node> segments) {/* body removed for skeleton view. */}
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
			get {/* body removed for skeleton view. */}
		}
		public IReadOnlyList<Node<TInfo, TLeaf, TLeafOps>> Children => _body.Children ?? Array.Empty<Node<TInfo, TLeaf, TLeafOps>>();
		private Node(NodeBody body) {/* body removed for skeleton view. */}
		public static Node<TInfo, TLeaf, TLeafOps> FromLeaf(TLeaf leaf) {/* body removed for skeleton view. */}
		public static Node<TInfo, TLeaf, TLeafOps> CreateInternal(IReadOnlyList<Node<TInfo, TLeaf, TLeafOps>> children) {/* body removed for skeleton view. */}
		public IEnumerable<Node<TInfo, TLeaf, TLeafOps>> TraverseLeaves() {/* body removed for skeleton view. */}
		public List<string> ValidateInvariants(bool enforceLeafMinimum = false) {/* body removed for skeleton view. */}
		public string ToDebugString() {/* body removed for skeleton view. */}
		private void AppendDebugString(StringBuilder builder, int depth) {/* body removed for skeleton view. */}
		private static void ValidateNode(Node<TInfo, TLeaf, TLeafOps> node, bool isRoot, bool enforceLeafMinimum, List<string> issues, string path) {/* body removed for skeleton view. */}
		private static void ValidateLeaf(Node<TInfo, TLeaf, TLeafOps> node, bool isRoot, bool enforceLeafMinimum, List<string> issues, string path) {/* body removed for skeleton view. */}
		private static void ValidateInternal(Node<TInfo, TLeaf, TLeafOps> node, bool isRoot, bool enforceLeafMinimum, List<string> issues, string path) {/* body removed for skeleton view. */}
		private static string FormatChildLengths(Node<TInfo, TLeaf, TLeafOps>[] children) {/* body removed for skeleton view. */}
	}
	public sealed class NodeCursor {
		private readonly struct PathFrame {
			public Node Node { get; }
			public int ChildIndex { get; }
			public PathFrame(Node node, int childIndex) {/* body removed for skeleton view. */}
			public PathFrame WithChildIndex(int childIndex) {/* body removed for skeleton view. */}
		}
		private readonly struct CursorSnapshot {
			public int Position { get; }
			public int OffsetOfLeaf { get; }
			public string? CurrentLeaf { get; }
			public Node? CurrentLeafNode { get; }
			public bool IsValid { get; }
			public PathFrame?[] Cache { get; }
			public CursorSnapshot(int position, int offsetOfLeaf, string? currentLeaf, Node? currentLeafNode, bool isValid, PathFrame?[] cache) {/* body removed for skeleton view. */}
		}
		private const int CacheSizeLimit = 4;
		private Node _root;
		private Node _rootSharedNode;
		private readonly Rope? _owner;
		private long _capturedEditVersion;
		private bool _reattachInProgress;
		private int _position;
		private readonly PathFrame?[] _pathCache;
		private string? _currentLeaf;
		private Node? _currentLeafNode;
		private int _offsetOfLeaf;
		private bool _isValid;
		public Node Root => _root;
		public int Position => _position;
		public int TotalLength => _root.Length;
		public bool IsValid => _isValid;
		public NodeCursor(Node root, int position)
			: this(root, position, null, -1L) {/* body removed for skeleton view. */}
		public NodeCursor(Rope owner, int position)
			: this((owner ?? throw new ArgumentNullException("owner")).DebugRoot, position, owner, owner.EditVersion) {/* body removed for skeleton view. */}
		private NodeCursor(Node root, int position, Rope? owner, long ownerVersionSnapshot) {/* body removed for skeleton view. */}
		private bool EnsureOwnerVersionMatches() {/* body removed for skeleton view. */}
		public (string Leaf, int Offset)? GetLeaf() {/* body removed for skeleton view. */}
		public void SetPosition(int position) {/* body removed for skeleton view. */}
		public bool IsBoundary(IMetric metric) {/* body removed for skeleton view. */}
		public int? MoveToPrevious(IMetric metric) {/* body removed for skeleton view. */}
		public int? MoveToNext(IMetric metric) {/* body removed for skeleton view. */}
		public int? AtOrNext(IMetric metric) {/* body removed for skeleton view. */}
		public int? AtOrPrevious(IMetric metric) {/* body removed for skeleton view. */}
		public CursorDescriptor ToDescriptor() {/* body removed for skeleton view. */}
		public bool TryApplyDescriptor(CursorDescriptor descriptor) {/* body removed for skeleton view. */}
		private void Descend(bool skipVersionCheck = false) {/* body removed for skeleton view. */}
		private bool TryRefreshOwnerState(long ownerVersion) {/* body removed for skeleton view. */}
		private bool PrevLeaf() {/* body removed for skeleton view. */}
		private bool NextLeaf() {/* body removed for skeleton view. */}
		private string? PeekPrevLeaf() {/* body removed for skeleton view. */}
		private int? LastInsideLeaf(IMetric metric, int originalPosition) {/* body removed for skeleton view. */}
		private int? NextInsideLeaf(IMetric metric) {/* body removed for skeleton view. */}
		private int? PreviousInsideLeaf(IMetric metric, int offsetInLeaf) {/* body removed for skeleton view. */}
		private int MeasureLeaf(IMetric metric, int position) {/* body removed for skeleton view. */}
		private void DescendMetric(IMetric metric, int measure) {/* body removed for skeleton view. */}
		private CursorSnapshot CaptureSnapshot() {/* body removed for skeleton view. */}
		private void RestoreSnapshot(CursorSnapshot snapshot) {/* body removed for skeleton view. */}
		private void SetLeafFromNode(Node leafNode, int offset) {/* body removed for skeleton view. */}
		private static Node[] RequireChildren(Node node) {/* body removed for skeleton view. */}
		private void ClearCache() {/* body removed for skeleton view. */}
		private IReadOnlyList<CursorDescriptorFrame> BuildDescriptorFrames() {/* body removed for skeleton view. */}
		private bool ApplyDescriptorInternal(CursorDescriptor descriptor) {/* body removed for skeleton view. */}
		private static bool TryCalculateChildOffset(Node parent, int childIndex, out int offset) {/* body removed for skeleton view. */}
		private void Invalidate() {/* body removed for skeleton view. */}
		private void InvalidateToStart() {/* body removed for skeleton view. */}
		private void InvalidateToEnd() {/* body removed for skeleton view. */}
		[Conditional("DEBUG")]
		private void AssertRootStable() {/* body removed for skeleton view. */}
	}
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	internal readonly struct StringLeafOperations : ILeafOperations<string> {
		public static int MinLeafSize => 511;
		public static int MaxLeafSize => 1024;
		public static string Empty => string.Empty;
		public static int NewlinePreferenceWindow => LeafSplitter.NewlinePreferenceWindow;
		public static int GetLength(string leaf) {/* body removed for skeleton view. */}
		public static bool IsValidChild(string leaf) {/* body removed for skeleton view. */}
		public static string Clone(string leaf) {/* body removed for skeleton view. */}
		public static string Insert(string leaf, int index, string text) {/* body removed for skeleton view. */}
		public static string RemoveRange(string leaf, int index, int length) {/* body removed for skeleton view. */}
		public static string ReplaceRange(string leaf, int index, int length, string replacement) {/* body removed for skeleton view. */}
		public static string Merge(string left, string right) {/* body removed for skeleton view. */}
		public static bool TryComputeBalancedSplit(string left, string right, out string newLeft, out string newRight) {/* body removed for skeleton view. */}
		public static IEnumerable<string> SplitByCapacity(string leaf) {/* body removed for skeleton view. */}
		private static int FindLeafSplit(ReadOnlySpan<char> span, int minSplit) {/* body removed for skeleton view. */}
		private static int PreferNewlineBoundary(string left, string right, int candidate, int minSplit, int maxSplit) {/* body removed for skeleton view. */}
		private static bool RetreatToCombinedSurrogateBoundary(string left, string right, ref int splitIndex) {/* body removed for skeleton view. */}
		private static bool IsSafeBoundary(string left, string right, int index) {/* body removed for skeleton view. */}
		private static string CreateCombinedSegment(string left, string right, int start, int length) {/* body removed for skeleton view. */}
		private static char GetCombinedChar(string left, string right, int index) {/* body removed for skeleton view. */}
	}
	public sealed class TreeBuilder {
		private readonly List<List<Node>> _stack = new List<List<Node>>();
		public void PushString(string? text) {/* body removed for skeleton view. */}
		public void PushSpan(ReadOnlySpan<char> span) {/* body removed for skeleton view. */}
		public void PushNode(Node node) {/* body removed for skeleton view. */}
		public Node Build() {/* body removed for skeleton view. */}
		public void Reset() {/* body removed for skeleton view. */}
		private void PushNodeInternal(Node node) {/* body removed for skeleton view. */}
		private Node PopStackNode() {/* body removed for skeleton view. */}
		private static void MergeLeafIntoFrame(List<Node> frame, Node incoming) {/* body removed for skeleton view. */}
		private static void MergeInternalIntoFrame(List<Node> frame, Node incoming) {/* body removed for skeleton view. */}
		private static IReadOnlyList<Node> CombineInternalChildren(Node left, Node right) {/* body removed for skeleton view. */}
		private static IEnumerable<string> SplitIntoLeaves(string text) {/* body removed for skeleton view. */}
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
namespace Xi.Core.Rope.Navigation {
	public sealed class DegradedGraphemeNavigator : IGraphemeNavigator {
		private static readonly int MaxContextChars = Node.MaxLeafSize * 2;
		private readonly GraphemeNavigationMetrics _metrics;
		public GraphemeNavigationMetrics Metrics => _metrics;
		public DegradedGraphemeNavigator(GraphemeNavigationMetrics? metrics = null) {/* body removed for skeleton view. */}
		public int? MoveNext(NodeCursor cursor) {/* body removed for skeleton view. */}
		public int? MovePrevious(NodeCursor cursor) {/* body removed for skeleton view. */}
		public bool IsBoundary(NodeCursor cursor) {/* body removed for skeleton view. */}
		private string BuildForwardContext(NodeCursor cursor, string leaf, int leafStart, int offsetInLeaf) {/* body removed for skeleton view. */}
		private string BuildBackwardContext(NodeCursor cursor, string leaf, int leafStart, int offsetInLeaf) {/* body removed for skeleton view. */}
		private static bool TryCaptureLeaf(NodeCursor cursor, out string leaf, out int leafStart, out int offsetInLeaf) {/* body removed for skeleton view. */}
		private static (string Leaf, int Start)? TryFetchLeaf(Node root, int absolutePosition) {/* body removed for skeleton view. */}
		private static int? GetFirstTextElementLength(string text) {/* body removed for skeleton view. */}
		private static int? GetLastTextElementLength(string text) {/* body removed for skeleton view. */}
		private static int GetForwardScalarLength(string text) {/* body removed for skeleton view. */}
		private static int GetBackwardScalarLength(string text) {/* body removed for skeleton view. */}
	}
	public sealed class GraphemeNavigationMetrics {
		public readonly record struct Snapshot(long ForwardNeighborRequests, long BackwardNeighborRequests, long ScalarFallbacks, long MoveNextCalls, long MovePreviousCalls);
		private long _forwardNeighborRequests;
		private long _backwardNeighborRequests;
		private long _scalarFallbacks;
		private long _moveNextCalls;
		private long _movePreviousCalls;
		public void RecordNeighborRequest(bool forward) {/* body removed for skeleton view. */}
		public void RecordScalarFallback() {/* body removed for skeleton view. */}
		public void RecordMoveNext() {/* body removed for skeleton view. */}
		public void RecordMovePrevious() {/* body removed for skeleton view. */}
		public void Reset() {/* body removed for skeleton view. */}
		public Snapshot GetSnapshot() {/* body removed for skeleton view. */}
	}
	public interface IGraphemeNavigator {
		GraphemeNavigationMetrics Metrics { get; }
		int? MoveNext(NodeCursor cursor);
		int? MovePrevious(NodeCursor cursor);
		bool IsBoundary(NodeCursor cursor);
	}
}
