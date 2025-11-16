using System;
using System.Threading;
using Xi.Core.Rope.Tree;

namespace Xi.Core.Rope;

/// <summary>
/// Rope-backed implementation of <see cref="ITextBuffer"/> built on top of <see cref="Node"/>.
/// Provides the same surface semantics as the placeholder buffer while preparing for advanced rope features and exposing edit-version tracking for cursors.
/// </summary>
public sealed class Rope : ITextBuffer
{
    private Node _root = Node.Empty;
    private long _editVersion;

    public int Length => _root.Length;

    internal Node DebugRoot => _root;

    /// <summary>
    /// Monotonically increasing version that bumps after each structural edit so dependent components can detect mutations.
    /// </summary>
    public long EditVersion => Interlocked.Read(ref _editVersion);

    /// <summary>
    /// Iterates the rope leaf-by-leaf, copying content into temporary buffers (per design-divergence-log.md 2025-11-16) until zero-copy chunk views are wired up.
    /// Optionally accepts a diagnostics collector to track chunk counts and copy sizes.
    /// </summary>
    public RopeChunkEnumerator EnumerateChunks(RopeChunkEnumeratorDiagnostics? diagnostics = null) => new(this, diagnostics);

    /// <summary>
    /// Iterates logical lines, including newline terminators when present. This first-pass implementation streams through chunk copies (see design-divergence-log.md 2025-11-16) and will be replaced by zero-copy spans later.
    /// </summary>
    public RopeLineEnumerator EnumerateLines() => new(this);

    public void Append(string? text)
    {
        Replace(Length, 0, text);
    }

    public void Append(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return;
        }

        Replace(Length, 0, text.ToString());
    }

    /// <summary>
    /// Creates a rope instance that adopts an existing <see cref="Node"/> tree.
    /// </summary>
    /// <param name="root">The root node to attach.</param>
    /// <returns>A rope whose contents mirror the supplied node.</returns>
    internal static Rope FromNode(Node root)
    {
        if (root is null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        var rope = new Rope();
        rope.UpdateRoot(root);
        return rope;
    }

    public void Clear()
    {
        UpdateRoot(Node.Empty);
    }

    public void Replace(int start, int length, string? text)
    {
        ValidateRange(start, length, _root.Length);
        var updated = _root.Replace(start, length, text);
        UpdateRoot(updated);
    }

    public string Snapshot() => _root.ToString();

    public string GetSlice(int start, int length)
    {
        ValidateRange(start, length, _root.Length);
        if (length == 0)
        {
            return string.Empty;
        }

        return _root.Slice(start, length).ToString();
    }

    public int ConvertLinesFromBytes(int offset)
    {
        ValidateOffset(offset, Length, nameof(offset));
        return _root.ConvertFromDefaultMetric(LinesMetric.Instance, offset);
    }

    public int ConvertBytesFromLines(int line)
    {
        var lineCount = LinesMetric.Instance.Measure(_root.Info, _root.Length);
        var maxLineIndex = lineCount + 1;

        ValidateMetricCoordinate(line, maxLineIndex, nameof(line));
        if (line == maxLineIndex)
        {
            return Length;
        }

        return _root.ConvertToDefaultMetric(LinesMetric.Instance, line);
    }

    public int ConvertUtf16FromBytes(int offset)
    {
        ValidateOffset(offset, Length, nameof(offset));
        return _root.ConvertFromDefaultMetric(Utf16Metric.Instance, offset);
    }

    public int ConvertBytesFromUtf16(int units)
    {
        var maxUnits = _root.Info.Utf16Length;
        ValidateMetricCoordinate(units, maxUnits, nameof(units));
        return _root.ConvertToDefaultMetric(Utf16Metric.Instance, units);
    }

    private static void ValidateRange(int start, int length, int totalLength)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start), start, "Start must be non-negative.");
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "Length must be non-negative.");
        }

        if (start > totalLength || start + length > totalLength)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "Requested slice exceeds buffer bounds.");
        }
    }

    private static void ValidateOffset(int offset, int totalLength, string parameterName)
    {
        if (offset < 0 || offset > totalLength)
        {
            throw new ArgumentOutOfRangeException(parameterName, offset, "Offset must be within buffer bounds.");
        }
    }

    private static void ValidateMetricCoordinate(int value, int maxInclusive, string parameterName)
    {
        if (value < 0 || value > maxInclusive)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"Value must be between 0 and {maxInclusive}.");
        }
    }

    private void UpdateRoot(Node newRoot)
    {
        if (newRoot is null)
        {
            throw new ArgumentNullException(nameof(newRoot));
        }

        if (ReferenceEquals(_root, newRoot))
        {
            return;
        }

        _root = newRoot;
        BumpEditVersion();
    }

    private void BumpEditVersion()
    {
        Interlocked.Increment(ref _editVersion);
    }
}
