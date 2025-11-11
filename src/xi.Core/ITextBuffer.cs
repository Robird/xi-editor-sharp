using System;

namespace Xi.Core;

/// <summary>
/// Contract for mutable text buffers used by the xi editor core.
/// Provides common operations needed by both the current placeholder implementation
/// and the forthcoming rope-based replacement.
/// </summary>
public interface ITextBuffer
{
    /// <summary>
    /// Gets the number of UTF-16 code units contained in the buffer.
    /// </summary>
    int Length { get; }

    /// <summary>
    /// Appends the specified text to the buffer.
    /// </summary>
    /// <param name="text">The text to append. Null is treated as empty.</param>
    void Append(string? text);

    /// <summary>
    /// Appends characters from the provided span.
    /// </summary>
    /// <param name="text">The span to append.</param>
    void Append(ReadOnlySpan<char> text);

    /// <summary>
    /// Removes all content from the buffer.
    /// </summary>
    void Clear();

    /// <summary>
    /// Replaces the specified range with new text.
    /// </summary>
    /// <param name="start">Start position of the replacement.</param>
    /// <param name="length">Number of UTF-16 code units to remove.</param>
    /// <param name="text">Text to insert at the given position.</param>
    void Replace(int start, int length, string? text);

    /// <summary>
    /// Returns a string snapshot of the entire buffer contents.
    /// </summary>
    string Snapshot();

    /// <summary>
    /// Returns a string slice of the buffer starting at <paramref name="start"/> with the given <paramref name="length"/>.
    /// </summary>
    /// <param name="start">Start offset within the buffer.</param>
    /// <param name="length">Number of UTF-16 code units to copy.</param>
    string GetSlice(int start, int length);
}
