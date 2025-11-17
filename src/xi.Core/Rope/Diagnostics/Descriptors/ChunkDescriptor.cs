using System.Collections.Generic;

namespace Xi.Core.Rope.Diagnostics.Descriptors;

/// <summary>
/// Mirrors entries in tests/xi.Core.Tests/Fixtures/chunk_descriptors/chunk_descriptors.json<br />
/// so Stage D ingestion code can validate `[StageD::ParityAssets]` samples on the C# side.
/// TODO(TS-B3): Populate instances by reading the manifest-backed JSON payloads.
/// </summary>
internal sealed class ChunkDescriptor
{
    public string Sample { get; set; } = string.Empty;

    public int ChunkIndex { get; set; }

    public string Text { get; set; } = string.Empty;

    public DescriptorRange ByteRange { get; set; } = new();

    public DescriptorRange Utf16Range { get; set; } = new();

    public DescriptorRange LeafRange { get; set; } = new();

    public bool ContainsCrlf { get; set; }

    public bool IsEmpty { get; set; }

    public IList<string> Tags { get; set; } = new List<string>();

    public IList<DescriptorNodePathEntry> Path { get; set; } = new List<DescriptorNodePathEntry>();

    public DescriptorContext Context { get; set; } = new();
}
