using System;

namespace Xi.Core.Rope;

/// <summary>
/// Rope-backed implementation of <see cref="ITextBuffer"/> built on top of <see cref="RopeNode"/>.
/// Provides the same surface semantics as the placeholder buffer while preparing for advanced rope features.
/// </summary>
public sealed class RopeTextBuffer : ITextBuffer
{
    private RopeNode _root = RopeNode.Empty;

    public int Length => _root.Length;

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

    public void Clear()
    {
        _root = RopeNode.Empty;
    }

    public void Replace(int start, int length, string? text)
    {
        ValidateRange(start, length, _root.Length);
        _root = _root.Replace(start, length, text);
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
}
