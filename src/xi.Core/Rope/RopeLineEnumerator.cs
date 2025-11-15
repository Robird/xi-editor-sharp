using System;
using System.Collections.Generic;
using System.Text;

namespace Xi.Core.Rope;

/// <summary>
/// Enumerates logical lines from a rope by consuming chunk data and emitting <see cref="ReadOnlyMemory{Char}"/> segments.
/// Line slices currently include their newline terminators when present; the implementation copies data through a temporary buffer per design-divergence-log.md (2025-11-16) before returning.
/// </summary>
public ref struct RopeLineEnumerator
{
    private readonly bool _ropeWasEmpty;
    private RopeChunkEnumerator _chunks;
    private Queue<ReadOnlyMemory<char>>? _pendingLines;
    private StringBuilder? _builder;
    private ReadOnlyMemory<char> _current;
    private bool _pendingCarriageReturn;
    private bool _completed;
    private bool _emittedAnyLine;

    internal RopeLineEnumerator(Rope rope)
    {
        if (rope is null)
        {
            throw new ArgumentNullException(nameof(rope));
        }

        _ropeWasEmpty = rope.Length == 0;
        _chunks = rope.EnumerateChunks();
        _pendingLines = null;
        _builder = null;
        _current = ReadOnlyMemory<char>.Empty;
        _pendingCarriageReturn = false;
        _completed = false;
        _emittedAnyLine = false;
    }

    /// <summary>Gets the current line.</summary>
    public ReadOnlyMemory<char> Current => _current;

    /// <summary>Returns the enumerator to support <c>foreach</c>.</summary>
    public RopeLineEnumerator GetEnumerator() => this;

    /// <summary>Advances to the next line slice.</summary>
    public bool MoveNext()
    {
        if (_completed && !TryDequeueLine(out _current))
        {
            return false;
        }

        while (true)
        {
            if (TryDequeueLine(out _current))
            {
                return true;
            }

            if (!_chunks.MoveNext())
            {
                if (_pendingCarriageReturn)
                {
                    var idx = 0;
                    CompleteDeferredCarriageReturn(ReadOnlySpan<char>.Empty, ref idx);
                    continue;
                }

                if (_builder is { Length: > 0 })
                {
                    EmitLine();
                    continue;
                }

                if (!_emittedAnyLine && _ropeWasEmpty)
                {
                    EnsureBuilder().Clear();
                    EmitLine();
                    continue;
                }

                _completed = true;
                return false;
            }

            ProcessChunk(_chunks.Current.Span);
        }
    }

    private void ProcessChunk(ReadOnlySpan<char> chunk)
    {
        var index = 0;

        if (_pendingCarriageReturn)
        {
            CompleteDeferredCarriageReturn(chunk, ref index);
        }

        var segmentStart = index;

        while (index < chunk.Length)
        {
            var ch = chunk[index];
            if (ch == '\n')
            {
                AppendSlice(chunk.Slice(segmentStart, index - segmentStart + 1));
                EmitLine();
                index++;
                segmentStart = index;
            }
            else if (ch == '\r')
            {
                if (index + 1 < chunk.Length && chunk[index + 1] == '\n')
                {
                    AppendSlice(chunk.Slice(segmentStart, index - segmentStart + 2));
                    EmitLine();
                    index += 2;
                    segmentStart = index;
                }
                else if (index + 1 == chunk.Length)
                {
                    AppendSlice(chunk.Slice(segmentStart, index - segmentStart));
                    _pendingCarriageReturn = true;
                    segmentStart = chunk.Length;
                    break;
                }
                else
                {
                    AppendSlice(chunk.Slice(segmentStart, index - segmentStart + 1));
                    EmitLine();
                    index++;
                    segmentStart = index;
                }
            }
            else
            {
                index++;
            }
        }

        if (segmentStart < chunk.Length)
        {
            AppendSlice(chunk.Slice(segmentStart));
        }
    }

    private void CompleteDeferredCarriageReturn(ReadOnlySpan<char> nextChunk, ref int index)
    {
        AppendLiteral("\r");
        if (nextChunk.Length > 0 && nextChunk[0] == '\n')
        {
            AppendLiteral("\n");
            index = 1;
        }
        else
        {
            index = 0;
        }

        EmitLine();
        _pendingCarriageReturn = false;
    }

    private void AppendSlice(ReadOnlySpan<char> slice)
    {
        if (slice.Length == 0)
        {
            return;
        }

        EnsureBuilder().Append(slice);
    }

    private void AppendLiteral(string literal)
    {
        if (literal.Length == 0)
        {
            return;
        }

        EnsureBuilder().Append(literal);
    }

    private void EmitLine()
    {
        var text = _builder?.ToString() ?? string.Empty;
        // TODO(#design-divergence-log.md 2025-11-16 Rope Chunk/Line 枚举与 Grapheme Telemetry): reuse pooled buffers once zero-copy chunks are available.
        EnqueueLine(text);
        _builder?.Clear();
        _emittedAnyLine = true;
    }

    private void EnqueueLine(string text)
    {
        _pendingLines ??= new Queue<ReadOnlyMemory<char>>();
        _pendingLines.Enqueue(text.Length == 0 ? ReadOnlyMemory<char>.Empty : text.AsMemory());
    }

    private bool TryDequeueLine(out ReadOnlyMemory<char> line)
    {
        if (_pendingLines is { Count: > 0 })
        {
            line = _pendingLines.Dequeue();
            return true;
        }

        line = ReadOnlyMemory<char>.Empty;
        return false;
    }

    private StringBuilder EnsureBuilder()
    {
        _builder ??= new StringBuilder();
        return _builder;
    }
}
