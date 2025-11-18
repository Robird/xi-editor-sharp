using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Xi.Core.Rope.Diagnostics.Descriptors;

/// <summary>
/// Represents entries under <c>line_descriptors</c> in the Stage D chunk descriptor payload.
/// </summary>
public sealed class LineDescriptor
{
    public string Sample { get; set; } = string.Empty;

    [JsonPropertyName("line_index")]
    public int LineIndex { get; set; }

    public string Raw { get; set; } = string.Empty;

    public string Logical { get; set; } = string.Empty;

    [JsonPropertyName("byte_range")]
    public DescriptorRange ByteRange { get; set; } = new();

    [JsonPropertyName("utf16_range")]
    public DescriptorRange Utf16Range { get; set; } = new();

    [JsonPropertyName("newline_kind")]
    public string? NewlineKind { get; set; }

    public IList<string> Tags { get; set; } = new List<string>();
}
