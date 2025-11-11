using System.Text;

namespace Xi.Core;

/// <summary>
/// Minimal mutable text buffer used as a placeholder until the full rope structure is ported.
/// Internally relies on <see cref="StringBuilder"/> and is intentionally simple.
/// </summary>
public sealed class TextBuffer
{
    private readonly StringBuilder _builder = new();

    /// <summary>
    /// Appends the specified text to the buffer.
    /// </summary>
    /// <param name="text">The text to append. Null is treated as empty.</param>
    public void Append(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        _builder.Append(text);
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

    /// <summary>
    /// Returns the current content of the buffer as a string.
    /// </summary>
    public override string ToString() => _builder.ToString();
}
