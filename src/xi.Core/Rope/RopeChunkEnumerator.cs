using System;
using Xi.Core.Rope.Tree;

namespace Xi.Core.Rope;

/// <summary>
/// Enumerates rope data leaf-by-leaf, exposing each chunk as <see cref="ReadOnlyMemory{Char}"/>.
/// This M3 version clones leaf content into temporary buffers (see design-divergence-log.md 2025-11-16 entry) until zero-copy spans are available.
/// </summary>
public ref struct RopeChunkEnumerator
{
    private readonly Rope _rope;
    private readonly NodeCursor? _cursor;
    private readonly RopeChunkEnumeratorDiagnostics? _diagnostics;
    private int _nextOffset;
    private bool _completed;
    private bool _emittedEmptyChunk;
    private ReadOnlyMemory<char> _current;

    internal RopeChunkEnumerator(Rope rope, RopeChunkEnumeratorDiagnostics? diagnostics = null)
    {
        _rope = rope ?? throw new ArgumentNullException(nameof(rope));
        _diagnostics = diagnostics;
        _diagnostics?.Reset();
        _nextOffset = 0;
        _completed = false;
        _emittedEmptyChunk = false;
        _current = ReadOnlyMemory<char>.Empty;
        _cursor = rope.Length == 0 ? null : new NodeCursor(rope, 0);
    }

    /// <summary>Gets the current chunk.</summary>
    public ReadOnlyMemory<char> Current => _current;

    /// <summary>Returns the enumerator so <c>foreach</c> works with the ref struct.</summary>
    public RopeChunkEnumerator GetEnumerator() => this;

    /// <summary>Advances to the next chunk.</summary>
    public bool MoveNext()
    {
        if (_completed)
        {
            return false;
        }

        if (_rope.Length == 0)
        {
            if (_emittedEmptyChunk)
            {
                _completed = true;
                return false;
            }

            _emittedEmptyChunk = true;
            _current = ReadOnlyMemory<char>.Empty;
            _diagnostics?.RecordChunk(0);
            return true;
        }

        if (_nextOffset >= _rope.Length)
        {
            _completed = true;
            _current = ReadOnlyMemory<char>.Empty;
            return false;
        }

        var cursor = _cursor ?? throw new InvalidOperationException("Cursor unavailable for chunk enumeration.");
        cursor.SetPosition(_nextOffset);
        var leafInfo = cursor.GetLeaf();

        if (!leafInfo.HasValue)
        {
            _completed = true;
            _current = ReadOnlyMemory<char>.Empty;
            return false;
        }

        var leafText = leafInfo.Value.Leaf ?? string.Empty;
        if (leafText.Length == 0)
        {
            _nextOffset = _rope.Length;
            _current = ReadOnlyMemory<char>.Empty;
            _diagnostics?.RecordChunk(0);
            return true;
        }

        _current = CloneLeaf(leafText);
        _diagnostics?.RecordChunk(_current.Length);
        _nextOffset = Math.Min(_rope.Length, _nextOffset + leafText.Length);
        return true;
    }

    private static ReadOnlyMemory<char> CloneLeaf(string leaf)
    {
        if (leaf.Length == 0)
        {
            return ReadOnlyMemory<char>.Empty;
        }

        // TODO(#design-divergence-log.md 2025-11-16 Rope Chunk/Line 枚举与 Grapheme Telemetry): switch to zero-copy chunk slices once Node exposes stable spans.
        var buffer = leaf.ToCharArray();
        return buffer.AsMemory();
    }
}

/// <summary>
/// Collects chunk enumeration statistics for diagnostics and micro-benchmarks.
/// Tracks chunk count, the longest chunk encountered, and the total number of UTF-16 characters copied from the rope.
/// </summary>
public sealed class RopeChunkEnumeratorDiagnostics
{
    /// <summary>Total chunks emitted by the enumerator (including empty chunks for empty ropes).</summary>
    public int ChunkCount { get; private set; }

    /// <summary>The length (in UTF-16 chars) of the longest chunk observed.</summary>
    public int MaxChunkLength { get; private set; }

    /// <summary>Total UTF-16 characters copied across every emitted chunk.</summary>
    public long TotalUtf16CharCount { get; private set; }

    /// <summary>Resets all counters to zero so the instance can be reused across enumerations.</summary>
    public void Reset()
    {
        ChunkCount = 0;
        MaxChunkLength = 0;
        TotalUtf16CharCount = 0;
    }

    internal void RecordChunk(int chunkLength)
    {
        ChunkCount++;
        if (chunkLength > MaxChunkLength)
        {
            MaxChunkLength = chunkLength;
        }

        TotalUtf16CharCount += chunkLength;
    }
}
