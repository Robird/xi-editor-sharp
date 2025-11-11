using System;
using System.Text;

namespace Xi.Core;

/// <summary>
/// Minimal mutable text buffer used as a placeholder until the full rope structure is ported.
/// Internally relies on <see cref="StringBuilder"/> and is intentionally simple.
/// </summary>
public sealed class TextBuffer : ITextBuffer
{
    private readonly StringBuilder _builder = new();

    /// <inheritdoc />
    public int Length => _builder.Length;

    /// <summary>
    /// Appends the specified text to the buffer.
    /// </summary>
    /// <param name="text">The text to append. Null is treated as empty.</param>
    public void Append(string? text)
    {
        Replace(Length, 0, text);
    }

    /// <summary>
    /// Appends characters from the provided span.
    /// </summary>
    public void Append(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return;
        }

        _builder.Append(text);
    }

    /// <summary>
    /// Clears all content from the buffer.
    /// </summary>
    public void Clear() => _builder.Clear();

    /// <inheritdoc />
    public void Replace(int start, int length, string? text)
    {
        ValidateRange(start, length, Length);

        if (length > 0)
        {
            _builder.Remove(start, length);
        }

        if (!string.IsNullOrEmpty(text))
        {
            _builder.Insert(start, text);
        }
    }

    /// <inheritdoc />
    public string Snapshot() => _builder.ToString();

    /// <inheritdoc />
    public string GetSlice(int start, int length)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start), start, "Start must be non-negative.");
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "Length must be non-negative.");
        }

        if (start > _builder.Length || start + length > _builder.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "Requested slice exceeds buffer bounds.");
        }

        return _builder.ToString(start, length);
    }

    /// <summary>
    /// Returns the current content of the buffer as a string.
    /// </summary>
    public override string ToString() => Snapshot();

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
            throw new ArgumentOutOfRangeException(nameof(length), length, "Requested range exceeds buffer bounds.");
        }
    }
}
