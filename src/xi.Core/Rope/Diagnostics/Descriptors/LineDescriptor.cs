using System.Collections.Generic;

namespace Xi.Core.Rope.Diagnostics.Descriptors;

/// <summary>
/// Represents entries under <c>line_descriptors</c> in the Stage D chunk descriptor payload.
/// </summary>
internal sealed class LineDescriptor
{
    public string Sample { get; set; } = string.Empty;

    public int LineIndex { get; set; }

    public string Raw { get; set; } = string.Empty;

    public string Logical { get; set; } = string.Empty;

    public DescriptorRange ByteRange { get; set; } = new();

    public DescriptorRange Utf16Range { get; set; } = new();

    public string? NewlineKind { get; set; }

    public IList<string> Tags { get; set; } = new List<string>();
}
