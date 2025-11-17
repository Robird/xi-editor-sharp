#define DEBUG
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Xi.Core.Rope.Tree;
[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.EnableEditAndContinue)]
[assembly: InternalsVisibleTo("xi.Core.Tests")]
[assembly: TargetFramework(".NETCoreApp,Version=v9.0", FrameworkDisplayName = ".NET 9.0")]
[assembly: AssemblyCompany("xi.Core")]
[assembly: AssemblyConfiguration("Debug")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0+7507c82c850308f25e6b5c241a7f52a84c1474d5")]
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
		public void Append(string? text) {/*...*/}
		public void Append(ReadOnlySpan<char> text) {/*...*/}
		public void Clear() {/*...*/}
		public void Replace(int start, int length, string? text) {/*...*/}
		public string Snapshot() {/*...*/}
		public string GetSlice(int start, int length) {/*...*/}
		public override string ToString() {/*...*/}
		private static void ValidateRange(int start, int length, int totalLength) {/*...*/}
	}
}
namespace Xi.Core.Rope {
	internal static class BreaksMetricHelper {
		public static int GetNthBreakOffset(ReadOnlySpan<int> breaks, int leafLength, int measuredUnits) {/*...*/}
		public static int CountBreaksUpTo(ReadOnlySpan<int> breaks, int offset) {/*...*/}
		public static int? FindPreviousBreak(ReadOnlySpan<int> breaks, int offset) {/*...*/}
		public static int? FindNextBreak(ReadOnlySpan<int> breaks, int offset) {/*...*/}
		public static bool IsBreakBoundary(ReadOnlySpan<int> breaks, int offset) {/*...*/}
	}
	internal enum DeltaElementKind {
		Copy,
		Insert
	}
	internal readonly struct CopyElement {
		public int Start { get; }
		public int End { get; }
		public int Length => End - Start;
		public CopyElement(int start, int end) {/*...*/}
	}
	internal readonly struct InsertElement<TLeaf> where TLeaf : class {
		public TLeaf Value { get; }
		public int Length { get; }
		public InsertElement(TLeaf value, int length) {/*...*/}
	}
	internal readonly struct DeltaElement<TLeaf> where TLeaf : class {
		private readonly DeltaElementKind _kind;
		private readonly CopyElement _copy;
		private readonly InsertElement<TLeaf> _insert;
		internal DeltaElementKind Kind => _kind;
		internal bool IsCopy => _kind == DeltaElementKind.Copy;
		internal bool IsInsert => _kind == DeltaElementKind.Insert;
		private DeltaElement(CopyElement copy) {/*...*/}
		private DeltaElement(InsertElement<TLeaf> insert) {/*...*/}
		internal static DeltaElement<TLeaf> Copy(int start, int end) {/*...*/}
		internal static DeltaElement<TLeaf> Insert(TLeaf value, int? length = null) {/*...*/}
		private static int ResolveInsertLength(TLeaf value) {/*...*/}
		internal CopyElement AsCopy() {/*...*/}
		internal InsertElement<TLeaf> AsInsert() {/*...*/}
	}
	internal sealed class Delta<TInfo, TLeaf> where TLeaf : class {
		private readonly List<DeltaElement<TLeaf>> _elements;
		internal int BaseLength { get; }
		internal int ElementCount => _elements.Count;
		internal IReadOnlyList<DeltaElement<TLeaf>> Elements => _elements;
		private Delta(List<DeltaElement<TLeaf>> elements, int baseLength) {/*...*/}
		internal IEnumerable<DeltaElement<TLeaf>> EnumerateElements() {/*...*/}
		internal IEnumerable<(bool IsInsert, int Start, int End)> EnumerateElementTriples() {/*...*/}
		internal static Delta<TInfo, TLeaf> FromElements(int baseLength, IEnumerable<DeltaElement<TLeaf>> elements) {/*...*/}
		internal static Delta<TInfo, TLeaf> FromElements(int baseLength, IEnumerable<(int? CopyStart, int? CopyEnd, TLeaf? Insert)> elementTuples) {/*...*/}
		internal (Delta<TInfo, TLeaf> InsertDelta, Subset DeletedSubset) Factor() {/*...*/}
		private static void ValidateBaseLength(int baseLength, int maxCopyEnd) {/*...*/}
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
		internal static string Serialize<TInfo>(Delta<TInfo, string> delta) {/*...*/}
		internal static Delta<TInfo, string> Deserialize<TInfo>(string json) {/*...*/}
	}
	internal readonly struct RevId {
		public long Session1 { get; }
		public int Session2 { get; }
		public int Number { get; }
		public RevId(long session1, int session2, int number) {/*...*/}
	}
	internal enum RevisionOperationKind {
		Edit,
		Undo
	}
	internal abstract class RevisionOperation {
		internal abstract RevisionOperationKind Kind { get; }
		internal bool IsEdit => Kind == RevisionOperationKind.Edit;
		internal bool IsUndo => Kind == RevisionOperationKind.Undo;
		internal RevisionEdit AsEdit() {/*...*/}
		internal RevisionUndo AsUndo() {/*...*/}
	}
	internal sealed class RevisionEdit : RevisionOperation {
		internal override RevisionOperationKind Kind => RevisionOperationKind.Edit;
		internal int Priority { get; }
		internal int UndoGroup { get; }
		internal Subset Inserts { get; }
		internal Subset Deletes { get; }
		internal RevisionEdit(int priority, int undoGroup, Subset inserts, Subset deletes) {/*...*/}
	}
	internal sealed class RevisionUndo : RevisionOperation {
		private readonly int[] _toggledGroups;
		private readonly IReadOnlyList<int> _toggledGroupsView;
		internal override RevisionOperationKind Kind => RevisionOperationKind.Undo;
		internal IReadOnlyList<int> ToggledGroups => _toggledGroupsView;
		internal Subset DeletesBitxor { get; }
		internal RevisionUndo(IEnumerable<int> toggledGroups, Subset deletesBitxor) {/*...*/}
		private static int[] CopyGroups(IEnumerable<int> groups) {/*...*/}
	}
	internal sealed class Revision {
		internal RevId RevId { get; }
		internal int MaxUndoSoFar { get; }
		internal RevisionOperation Operation { get; }
		internal Revision(RevId revId, int maxUndoSoFar, RevisionOperation operation) {/*...*/}
	}
	internal sealed class Engine {
		private readonly string _text;
		private readonly string _tombstones;
		private readonly Subset _deletesFromUnion;
		private readonly int[] _undoneGroups;
		private readonly Revision[] _revisions;
		private readonly IReadOnlyList<int> _undoneGroupsView;
		private readonly IReadOnlyList<Revision> _revisionLogView;
		private Engine(string text, string tombstones, Subset deletesFromUnion, int[] undoneGroups, Revision[] revisions) {/*...*/}
		internal static Engine FromSerializedState(string text, string tombstones, Subset deletesFromUnion, IEnumerable<int> undoneGroups, IEnumerable<Revision> revisions) {/*...*/}
		private static int[] CopyGroups(IEnumerable<int> source) {/*...*/}
		private static Revision[] CopyRevisions(IEnumerable<Revision> source) {/*...*/}
		internal string TextSnapshot() {/*...*/}
		internal string TombstonesSnapshot() {/*...*/}
		internal Subset DeletesFromUnionSnapshot() {/*...*/}
		internal IReadOnlyList<int> UndoneGroupsSnapshot() {/*...*/}
		internal IReadOnlyList<Revision> RevisionLog() {/*...*/}
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
		internal static string Serialize(Engine engine) {/*...*/}
		internal static Engine Deserialize(string json) {/*...*/}
		private static int[] CopyGroups(IReadOnlyList<int> groups) {/*...*/}
		private static int[] ValidateGroups(int[] groups, string fieldName) {/*...*/}
		private static RevisionDto[] ToRevisionDtos(IReadOnlyList<Revision> revisions) {/*...*/}
		private static RevisionDto ToRevisionDto(Revision revision) {/*...*/}
		private static EditPayloadDto ToEditPayloadDto(RevisionEdit edit) {/*...*/}
		private static UndoPayloadDto ToUndoPayloadDto(RevisionUndo undo) {/*...*/}
		private static SubsetDto ToSubsetDto(Subset subset) {/*...*/}
		private static Subset FromSubsetDto(SubsetDto dto) {/*...*/}
	}
	public interface IMetric : ITreeMetric<string, RopeInfo> {
	}
	public readonly struct Interval : IEquatable<Interval> {
		public int Start { get; }
		public int End { get; }
		public int Length => End - Start;
		public bool IsEmpty => Start == End;
		public static Interval Empty => new Interval(0, 0);
		public Interval(int start, int end) {/*...*/}
		public bool Contains(int position) {/*...*/}
		public Interval Intersect(Interval other) {/*...*/}
		public Interval Union(Interval other) {/*...*/}
		public Interval Translate(int delta) {/*...*/}
		public static Interval EmptyAt(int position) {/*...*/}
		public bool Equals(Interval other) {/*...*/}
		public override bool Equals(object? obj) {/*...*/}
		public override int GetHashCode() {/*...*/}
		public override string ToString() {/*...*/}
		public static bool operator ==(Interval left, Interval right) {/*...*/}
		public static bool operator !=(Interval left, Interval right) {/*...*/}
	}
	public sealed class BaseMetric : IMetric, ITreeMetric<string, RopeInfo> {
		public static BaseMetric Instance { get; } = new BaseMetric();
		public bool CanFragment => false;
		private BaseMetric() {/*...*/}
		public int Measure(RopeInfo info, int nodeLength) {/*...*/}
		public int ToBaseUnits(string leaf, int measuredUnits) {/*...*/}
		public int FromBaseUnits(string leaf, int baseUnits) {/*...*/}
		public bool IsBoundary(string leaf, int offset) {/*...*/}
		public int? GetPreviousBoundary(string leaf, int offset) {/*...*/}
		public int? GetNextBoundary(string leaf, int offset) {/*...*/}
	}
	public sealed class LinesMetric : IMetric, ITreeMetric<string, RopeInfo> {
		public static LinesMetric Instance { get; } = new LinesMetric();
		public bool CanFragment => true;
		private LinesMetric() {/*...*/}
		public int Measure(RopeInfo info, int nodeLength) {/*...*/}
		public int ToBaseUnits(string leaf, int measuredUnits) {/*...*/}
		public int FromBaseUnits(string leaf, int baseUnits) {/*...*/}
		public bool IsBoundary(string leaf, int offset) {/*...*/}
		public int? GetPreviousBoundary(string leaf, int offset) {/*...*/}
		public int? GetNextBoundary(string leaf, int offset) {/*...*/}
	}
	public sealed class Utf16Metric : IMetric, ITreeMetric<string, RopeInfo> {
		public static Utf16Metric Instance { get; } = new Utf16Metric();
		public bool CanFragment => false;
		private Utf16Metric() {/*...*/}
		public int Measure(RopeInfo info, int nodeLength) {/*...*/}
		public int ToBaseUnits(string leaf, int measuredUnits) {/*...*/}
		public int FromBaseUnits(string leaf, int baseUnits) {/*...*/}
		public bool IsBoundary(string leaf, int offset) {/*...*/}
		public int? GetPreviousBoundary(string leaf, int offset) {/*...*/}
		public int? GetNextBoundary(string leaf, int offset) {/*...*/}
	}
	internal static class Utf16BoundaryHelper {
		public static bool IsBoundary(ReadOnlySpan<char> leaf, int offset) {/*...*/}
		public static int? GetPreviousBoundary(ReadOnlySpan<char> leaf, int offset) {/*...*/}
		public static int? GetNextBoundary(ReadOnlySpan<char> leaf, int offset) {/*...*/}
	}
	public sealed class Rope : ITextBuffer {
		private Node _root = Node.Empty;
		private long _editVersion;
		public int Length => _root.Length;
		internal Node DebugRoot => _root;
		public long EditVersion => Interlocked.Read(in _editVersion);
		public RopeChunkEnumerator EnumerateChunks(RopeChunkEnumeratorDiagnostics? diagnostics = null) {/*...*/}
		public RopeLineEnumerator EnumerateLines() {/*...*/}
		public void Append(string? text) {/*...*/}
		public void Append(ReadOnlySpan<char> text) {/*...*/}
		internal static Rope FromNode(Node root) {/*...*/}
		public void Clear() {/*...*/}
		public void Replace(int start, int length, string? text) {/*...*/}
		public string Snapshot() {/*...*/}
		public string GetSlice(int start, int length) {/*...*/}
		public int ConvertLinesFromBytes(int offset) {/*...*/}
		public int ConvertBytesFromLines(int line) {/*...*/}
		public int ConvertUtf16FromBytes(int offset) {/*...*/}
		public int ConvertBytesFromUtf16(int units) {/*...*/}
		private static void ValidateRange(int start, int length, int totalLength) {/*...*/}
		private static void ValidateOffset(int offset, int totalLength, string parameterName) {/*...*/}
		private static void ValidateMetricCoordinate(int value, int maxInclusive, string parameterName) {/*...*/}
		private void UpdateRoot(Node newRoot) {/*...*/}
		private void BumpEditVersion() {/*...*/}
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
		internal RopeChunkEnumerator(Rope rope, RopeChunkEnumeratorDiagnostics? diagnostics = null) {/*...*/}
		public RopeChunkEnumerator GetEnumerator() {/*...*/}
		public bool MoveNext() {/*...*/}
		private static ReadOnlyMemory<char> CloneLeaf(string leaf) {/*...*/}
	}
	public sealed class RopeChunkEnumeratorDiagnostics {
		public int ChunkCount { get; private set; }
		public int MaxChunkLength { get; private set; }
		public long TotalUtf16CharCount { get; private set; }
		public void Reset() {/*...*/}
		internal void RecordChunk(int chunkLength) {/*...*/}
	}
	public readonly struct RopeInfo : ITreeNodeInfo<RopeInfo, string>, IDefaultMetricProvider<RopeInfo, string, BaseMetric> {
		public int LineCount { get; }
		public int Utf16Length { get; }
		public static RopeInfo Identity => new RopeInfo(0, 0);
		static BaseMetric IDefaultMetricProvider<RopeInfo, string, BaseMetric>.DefaultMetric => BaseMetric.Instance;
		private RopeInfo(int lineCount, int utf16Length) {/*...*/}
		public static RopeInfo FromLeaf(ReadOnlySpan<char> span) {/*...*/}
		public static RopeInfo FromLeaf(string leaf) {/*...*/}
		public RopeInfo Accumulate(RopeInfo other) {/*...*/}
		public Interval IntervalForPrefix(int prefixLength) {/*...*/}
		private static (int lines, int utf16) AnalyzeSpan(ReadOnlySpan<char> span) {/*...*/}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int AddLength(int baseLength) {/*...*/}
		static RopeInfo ITreeNodeInfo<RopeInfo, string>.FromLeaf(string leaf) {/*...*/}
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
		internal RopeLineEnumerator(Rope rope) {/*...*/}
		public RopeLineEnumerator GetEnumerator() {/*...*/}
		public bool MoveNext() {/*...*/}
		private void ProcessChunk(ReadOnlySpan<char> chunk) {/*...*/}
		private void CompleteDeferredCarriageReturn(ReadOnlySpan<char> nextChunk, ref int index) {/*...*/}
		private void AppendSlice(ReadOnlySpan<char> slice) {/*...*/}
		private void AppendLiteral(string literal) {/*...*/}
		private void EmitLine() {/*...*/}
		private void EnqueueLine(string text) {/*...*/}
		private bool TryDequeueLine(out ReadOnlyMemory<char> line) {/*...*/}
		private StringBuilder EnsureBuilder() {/*...*/}
	}
	internal readonly struct SubsetSegment {
		public int Length { get; }
		public int Count { get; }
		public SubsetSegment(int length, int count) {/*...*/}
	}
	internal sealed class Subset {
		private readonly SubsetSegment[] _segments;
		private readonly int _totalLength;
		internal static Subset Empty { get; } = new Subset(Array.Empty<SubsetSegment>(), 0);
		internal int SegmentCount => _segments.Length;
		internal bool IsEmpty => _segments.Length == 0 || (_segments.Length == 1 && _segments[0].Count == 0);
		internal int Length => _totalLength;
		private Subset(SubsetSegment[] segments, int totalLength) {/*...*/}
		internal static Subset Create(SubsetSegment[] segments, int totalLength) {/*...*/}
		internal int LengthAfterDelete() {/*...*/}
		internal IEnumerable<(int Start, int Length, int Count)> SegmentTriples() {/*...*/}
		internal static Subset FromSegmentTriples(IEnumerable<(int Start, int Length, int Count)> triples) {/*...*/}
	}
	internal sealed class SubsetBuilder {
		private readonly List<SubsetSegment> _segments = new List<SubsetSegment>();
		private int _totalLength;
		internal void PadToLength(int totalLength) {/*...*/}
		internal void AddRange(int begin, int end, int count) {/*...*/}
		internal void PushSegment(int length, int count) {/*...*/}
		private void PushSegmentInternal(int length, int count) {/*...*/}
		internal Subset Build() {/*...*/}
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
		internal static string Serialize(Subset subset) {/*...*/}
		internal static Subset Deserialize(string json) {/*...*/}
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
		private CursorDescriptor(bool isValid, int position, int offsetOfLeaf, int? leafLength, Node? leafNode, CursorDescriptorFrame[] frames) {/*...*/}
		internal static CursorDescriptor CreateInvalid(int position) {/*...*/}
		internal static CursorDescriptor CreateValid(int position, int offsetOfLeaf, Node leafNode, IReadOnlyList<CursorDescriptorFrame> frames) {/*...*/}
		public NodeCursor? TryRestore(Rope rope) {/*...*/}
	}
	public sealed class CursorDescriptorFrame {
		public int NodeHeight { get; }
		public int NodeLength { get; }
		public int ChildIndex { get; }
		public int ChildOffset { get; }
		internal Node Node { get; }
		internal CursorDescriptorFrame(Node node, int childIndex, int childOffset) {/*...*/}
	}
	public sealed class GenericTreeBuilder<TInfo, TLeaf, TLeafOps> where TInfo : struct, ITreeNodeInfo<TInfo, TLeaf> where TLeafOps : ILeafOperations<TLeaf> {
		private readonly List<Node<TInfo, TLeaf, TLeafOps>> _pending = new List<Node<TInfo, TLeaf, TLeafOps>>();
		public void PushString(string? text) {/*...*/}
		public void PushNode(Node<TInfo, TLeaf, TLeafOps> node) {/*...*/}
		public Node<TInfo, TLeaf, TLeafOps> Build() {/*...*/}
		public void Reset() {/*...*/}
		private void AppendNode(Node<TInfo, TLeaf, TLeafOps> node) {/*...*/}
		private static Node<TInfo, TLeaf, TLeafOps> Concat(Node<TInfo, TLeaf, TLeafOps> left, Node<TInfo, TLeaf, TLeafOps> right) {/*...*/}
		private static Node<TInfo, TLeaf, TLeafOps> ConcatLeftShorter(Node<TInfo, TLeaf, TLeafOps> left, Node<TInfo, TLeaf, TLeafOps> right) {/*...*/}
		private static Node<TInfo, TLeaf, TLeafOps> ConcatRightShorter(Node<TInfo, TLeaf, TLeafOps> left, Node<TInfo, TLeaf, TLeafOps> right) {/*...*/}
		private static TLeaf ConvertStringToLeaf(string segment) {/*...*/}
	}
	internal static class LeafSplitter {
		internal static readonly int NewlinePreferenceWindow = StringLeafOperations.MaxLeafSize - StringLeafOperations.MinLeafSize;
		internal static IEnumerable<string> Split(string leaf) {/*...*/}
	}
	public sealed class Node {
		private sealed class NodeBody {
			public int Height { get; }
			public int Length { get; }
			public RopeInfo Info { get; }
			public string? Leaf { get; }
			public Node[]? Children { get; }
			public NodeBody(int height, int length, RopeInfo info, string? leaf, Node[]? children) {/*...*/}
			public NodeBody Clone(Node[]? overrideChildren = null) {/*...*/}
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
			public SharedNode(NodeBody body) {/*...*/}
			public SharedNode EnsureUnique() {/*...*/}
			public SharedNode CloneWithChildren(IReadOnlyList<Node> newChildren) {/*...*/}
			public SharedNode ReplaceChildRange(int index, int removeCount, IReadOnlyList<Node> replacements) {/*...*/}
			public static SharedNode FromInternal(int height, IReadOnlyList<Node> children) {/*...*/}
			private static (int Length, RopeInfo Info, Node[] Array) MaterializeChildren(int parentHeight, IReadOnlyList<Node> children) {/*...*/}
			private static (int Length, RopeInfo Info) AggregateChildren(int parentHeight, Node[] children) {/*...*/}
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
			get {/*...*/}
		}
		public IReadOnlyList<Node> Children => Body.Children ?? Array.Empty<Node>();
		public ReadOnlySpan<char> LeafSpan => (Body.Leaf == null) ? ReadOnlySpan<char>.Empty : Body.Leaf.AsSpan();
		private Node(NodeBody body)
			: this(new SharedNode(body)) {/*...*/}
		private Node(SharedNode shared) {/*...*/}
		private static Node FromShared(SharedNode shared) {/*...*/}
		internal bool IsOkChild() {/*...*/}
		public string? GetLeaf() {/*...*/}
		public Node[]? GetChildren() {/*...*/}
		internal int ConvertFromDefaultMetric(IMetric metric, int offset) {/*...*/}
		internal int ConvertToDefaultMetric(IMetric metric, int value) {/*...*/}
		private static int ConvertMetrics(Node node, int value, IMetric fromMetric, IMetric toMetric) {/*...*/}
		public static Node FromLeaf(string? text) {/*...*/}
		public static Node Concat(Node left, Node right) {/*...*/}
		public IEnumerable<Node> TraverseLeaves() {/*...*/}
		public Node Slice(int start, int length) {/*...*/}
		public override string ToString() {/*...*/}
		public Node Insert(int start, string text) {/*...*/}
		public Node Delete(int start, int length) {/*...*/}
		public Node Replace(int start, int length, string? text) {/*...*/}
		public Node EnsureWritableLeaf() {/*...*/}
		public IReadOnlyList<Node> SplitLeafByBounds() {/*...*/}
		private bool TryInsertInSingleLeaf(int start, string text, out Node result, out IReadOnlyList<Node>? splitNodes) {/*...*/}
		private bool TryDeleteInSingleSegment(int start, int length, out Node result) {/*...*/}
		private bool TryReplaceInSingleSegment(int start, int length, string text, out Node result, out IReadOnlyList<Node>? splitNodes) {/*...*/}
		public Node CloneWithChildren(IReadOnlyList<Node> newChildren) {/*...*/}
		public IReadOnlyList<string> CollectInvariantIssues(bool enforceLeafMinimum = false) {/*...*/}
		public void ValidateInvariants(bool enforceLeafMinimum = false) {/*...*/}
		public Node NormalizeLeafMinimum() {/*...*/}
		private static bool TryParseInvariantPath(string issue, out int[] indices) {/*...*/}
		private bool TryResolveLeafUnderflow(ReadOnlySpan<int> path, out Node updated) {/*...*/}
		private Node ReplaceChildWithSegments(Node[] children, int index, IReadOnlyList<Node> segments) {/*...*/}
		public Node WithChildReplaced(int index, Node newChild) {/*...*/}
		public (Node Left, Node Right) SplitAt(int index) {/*...*/}
		private Node[] RequireChildren() {/*...*/}
		private static Node MergeLeaves(Node left, Node right) {/*...*/}
		private static Node MergeNodes(IReadOnlyList<Node> leftChildren, IReadOnlyList<Node> rightChildren) {/*...*/}
		internal static Node FromNodes(IReadOnlyList<Node> children) {/*...*/}
		private static Node[] CopyRange(IReadOnlyList<Node> source, int start, int length) {/*...*/}
		private static Node[] CombineChildren(IReadOnlyList<Node>? leftChildren, IReadOnlyList<Node>? rightChildren) {/*...*/}
		private static string GetLeafText(Node node) {/*...*/}
		private bool TryMergeLeafWithSibling(Node[] children, int index, Node replacement, out Node result) {/*...*/}
		private bool TryRebalanceLeafWithSibling(Node[] children, int index, Node replacement, out Node result) {/*...*/}
		private bool TryRebalancePair(Node[] children, int firstIndex, int secondIndex, Node first, Node second, out Node result) {/*...*/}
		private static void ValidateNode(Node node, bool isRoot, bool enforceLeafMinimum, List<string> issues, string path) {/*...*/}
		private static string FormatLeafPreview(Node node) {/*...*/}
		private static string EscapePreview(string text) {/*...*/}
		private static string SummarizeChildren(Node[] children) {/*...*/}
		private Node BuildMergedNode(Node[] children, int firstIndex, int secondIndex, Node mergedLeaf) {/*...*/}
		private static Node CreateInternal(int height, IReadOnlyList<Node> children) {/*...*/}
		private static Node BuildFromSegments(List<Node> segments) {/*...*/}
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
			get {/*...*/}
		}
		public IReadOnlyList<Node<TInfo, TLeaf, TLeafOps>> Children => _body.Children ?? Array.Empty<Node<TInfo, TLeaf, TLeafOps>>();
		private Node(NodeBody body) {/*...*/}
		public static Node<TInfo, TLeaf, TLeafOps> FromLeaf(TLeaf leaf) {/*...*/}
		public static Node<TInfo, TLeaf, TLeafOps> CreateInternal(IReadOnlyList<Node<TInfo, TLeaf, TLeafOps>> children) {/*...*/}
		public IEnumerable<Node<TInfo, TLeaf, TLeafOps>> TraverseLeaves() {/*...*/}
		public List<string> ValidateInvariants(bool enforceLeafMinimum = false) {/*...*/}
		public string ToDebugString() {/*...*/}
		private void AppendDebugString(StringBuilder builder, int depth) {/*...*/}
		private static void ValidateNode(Node<TInfo, TLeaf, TLeafOps> node, bool isRoot, bool enforceLeafMinimum, List<string> issues, string path) {/*...*/}
		private static void ValidateLeaf(Node<TInfo, TLeaf, TLeafOps> node, bool isRoot, bool enforceLeafMinimum, List<string> issues, string path) {/*...*/}
		private static void ValidateInternal(Node<TInfo, TLeaf, TLeafOps> node, bool isRoot, bool enforceLeafMinimum, List<string> issues, string path) {/*...*/}
		private static string FormatChildLengths(Node<TInfo, TLeaf, TLeafOps>[] children) {/*...*/}
	}
	public sealed class NodeCursor {
		private readonly struct PathFrame {
			public Node Node { get; }
			public int ChildIndex { get; }
			public PathFrame(Node node, int childIndex) {/*...*/}
			public PathFrame WithChildIndex(int childIndex) {/*...*/}
		}
		private readonly struct CursorSnapshot {
			public int Position { get; }
			public int OffsetOfLeaf { get; }
			public string? CurrentLeaf { get; }
			public Node? CurrentLeafNode { get; }
			public bool IsValid { get; }
			public PathFrame?[] Cache { get; }
			public CursorSnapshot(int position, int offsetOfLeaf, string? currentLeaf, Node? currentLeafNode, bool isValid, PathFrame?[] cache) {/*...*/}
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
			: this(root, position, null, -1L) {/*...*/}
		public NodeCursor(Rope owner, int position)
			: this((owner ?? throw new ArgumentNullException("owner")).DebugRoot, position, owner, owner.EditVersion) {/*...*/}
		private NodeCursor(Node root, int position, Rope? owner, long ownerVersionSnapshot) {/*...*/}
		private bool EnsureOwnerVersionMatches() {/*...*/}
		public (string Leaf, int Offset)? GetLeaf() {/*...*/}
		public void SetPosition(int position) {/*...*/}
		public bool IsBoundary(IMetric metric) {/*...*/}
		public int? MoveToPrevious(IMetric metric) {/*...*/}
		public int? MoveToNext(IMetric metric) {/*...*/}
		public int? AtOrNext(IMetric metric) {/*...*/}
		public int? AtOrPrevious(IMetric metric) {/*...*/}
		public CursorDescriptor ToDescriptor() {/*...*/}
		public bool TryApplyDescriptor(CursorDescriptor descriptor) {/*...*/}
		private void Descend(bool skipVersionCheck = false) {/*...*/}
		private bool TryRefreshOwnerState(long ownerVersion) {/*...*/}
		private bool PrevLeaf() {/*...*/}
		private bool NextLeaf() {/*...*/}
		private string? PeekPrevLeaf() {/*...*/}
		private int? LastInsideLeaf(IMetric metric, int originalPosition) {/*...*/}
		private int? NextInsideLeaf(IMetric metric) {/*...*/}
		private int? PreviousInsideLeaf(IMetric metric, int offsetInLeaf) {/*...*/}
		private int MeasureLeaf(IMetric metric, int position) {/*...*/}
		private void DescendMetric(IMetric metric, int measure) {/*...*/}
		private CursorSnapshot CaptureSnapshot() {/*...*/}
		private void RestoreSnapshot(CursorSnapshot snapshot) {/*...*/}
		private void SetLeafFromNode(Node leafNode, int offset) {/*...*/}
		private static Node[] RequireChildren(Node node) {/*...*/}
		private void ClearCache() {/*...*/}
		private IReadOnlyList<CursorDescriptorFrame> BuildDescriptorFrames() {/*...*/}
		private bool ApplyDescriptorInternal(CursorDescriptor descriptor) {/*...*/}
		private static bool TryCalculateChildOffset(Node parent, int childIndex, out int offset) {/*...*/}
		private void Invalidate() {/*...*/}
		private void InvalidateToStart() {/*...*/}
		private void InvalidateToEnd() {/*...*/}
		[Conditional("DEBUG")]
		private void AssertRootStable() {/*...*/}
	}
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	internal readonly struct StringLeafOperations : ILeafOperations<string> {
		public static int MinLeafSize => 511;
		public static int MaxLeafSize => 1024;
		public static string Empty => string.Empty;
		public static int NewlinePreferenceWindow => LeafSplitter.NewlinePreferenceWindow;
		public static int GetLength(string leaf) {/*...*/}
		public static bool IsValidChild(string leaf) {/*...*/}
		public static string Clone(string leaf) {/*...*/}
		public static string Insert(string leaf, int index, string text) {/*...*/}
		public static string RemoveRange(string leaf, int index, int length) {/*...*/}
		public static string ReplaceRange(string leaf, int index, int length, string replacement) {/*...*/}
		public static string Merge(string left, string right) {/*...*/}
		public static bool TryComputeBalancedSplit(string left, string right, out string newLeft, out string newRight) {/*...*/}
		public static IEnumerable<string> SplitByCapacity(string leaf) {/*...*/}
		private static int FindLeafSplit(ReadOnlySpan<char> span, int minSplit) {/*...*/}
		private static int PreferNewlineBoundary(string left, string right, int candidate, int minSplit, int maxSplit) {/*...*/}
		private static bool RetreatToCombinedSurrogateBoundary(string left, string right, ref int splitIndex) {/*...*/}
		private static bool IsSafeBoundary(string left, string right, int index) {/*...*/}
		private static string CreateCombinedSegment(string left, string right, int start, int length) {/*...*/}
		private static char GetCombinedChar(string left, string right, int index) {/*...*/}
	}
	public sealed class TreeBuilder {
		private readonly List<List<Node>> _stack = new List<List<Node>>();
		public void PushString(string? text) {/*...*/}
		public void PushSpan(ReadOnlySpan<char> span) {/*...*/}
		public void PushNode(Node node) {/*...*/}
		public Node Build() {/*...*/}
		public void Reset() {/*...*/}
		private void PushNodeInternal(Node node) {/*...*/}
		private Node PopStackNode() {/*...*/}
		private static void MergeLeafIntoFrame(List<Node> frame, Node incoming) {/*...*/}
		private static void MergeInternalIntoFrame(List<Node> frame, Node incoming) {/*...*/}
		private static IReadOnlyList<Node> CombineInternalChildren(Node left, Node right) {/*...*/}
		private static IEnumerable<string> SplitIntoLeaves(string text) {/*...*/}
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
		public DegradedGraphemeNavigator(GraphemeNavigationMetrics? metrics = null) {/*...*/}
		public int? MoveNext(NodeCursor cursor) {/*...*/}
		public int? MovePrevious(NodeCursor cursor) {/*...*/}
		public bool IsBoundary(NodeCursor cursor) {/*...*/}
		private string BuildForwardContext(NodeCursor cursor, string leaf, int leafStart, int offsetInLeaf) {/*...*/}
		private string BuildBackwardContext(NodeCursor cursor, string leaf, int leafStart, int offsetInLeaf) {/*...*/}
		private static bool TryCaptureLeaf(NodeCursor cursor, out string leaf, out int leafStart, out int offsetInLeaf) {/*...*/}
		private static (string Leaf, int Start)? TryFetchLeaf(Node root, int absolutePosition) {/*...*/}
		private static int? GetFirstTextElementLength(string text) {/*...*/}
		private static int? GetLastTextElementLength(string text) {/*...*/}
		private static int GetForwardScalarLength(string text) {/*...*/}
		private static int GetBackwardScalarLength(string text) {/*...*/}
	}
	public sealed class GraphemeNavigationMetrics {
		public readonly record struct Snapshot(long ForwardNeighborRequests, long BackwardNeighborRequests, long ScalarFallbacks, long MoveNextCalls, long MovePreviousCalls);
		private long _forwardNeighborRequests;
		private long _backwardNeighborRequests;
		private long _scalarFallbacks;
		private long _moveNextCalls;
		private long _movePreviousCalls;
		public void RecordNeighborRequest(bool forward) {/*...*/}
		public void RecordScalarFallback() {/*...*/}
		public void RecordMoveNext() {/*...*/}
		public void RecordMovePrevious() {/*...*/}
		public void Reset() {/*...*/}
		public Snapshot GetSnapshot() {/*...*/}
	}
	public interface IGraphemeNavigator {
		GraphemeNavigationMetrics Metrics { get; }
		int? MoveNext(NodeCursor cursor);
		int? MovePrevious(NodeCursor cursor);
		bool IsBoundary(NodeCursor cursor);
	}
}
namespace xi.Core {
	public class Class1 {
	}
}
