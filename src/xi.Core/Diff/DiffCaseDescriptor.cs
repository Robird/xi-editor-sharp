using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Xi.Core.Rope.StageD;

namespace Xi.Core.Diff;

/// <summary>
/// Represents the JSON payload emitted by <c>--diff-regions</c>.
/// </summary>
public sealed class DiffRegionsDocument
{
    [JsonPropertyName("metadata")]
    public DiffRegionsMetadata Metadata { get; set; } = new();

    [JsonPropertyName("diff_cases")]
    public IList<DiffCaseDescriptor> DiffCases { get; set; } = new List<DiffCaseDescriptor>();
}

/// <summary>Metadata describing the diff asset batch.</summary>
public sealed class DiffRegionsMetadata
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; set; } = string.Empty;

    [JsonPropertyName("rust_commit")]
    public string? RustCommit { get; set; }

    [JsonPropertyName("generated_at_unix_millis")]
    public long GeneratedAtUnixMillis { get; set; }

    [JsonPropertyName("case_count")]
    public int CaseCount { get; set; }
}

/// <summary>Single diff sample comprised of ordered diff operations.</summary>
public sealed class DiffCaseDescriptor
{
    [JsonPropertyName("sample")]
    public string Sample { get; set; } = string.Empty;

    [JsonPropertyName("base_path")]
    public string BasePath { get; set; } = string.Empty;

    [JsonPropertyName("target_path")]
    public string TargetPath { get; set; } = string.Empty;

    [JsonPropertyName("base_sha")]
    public string? BaseSha { get; set; }

    [JsonPropertyName("target_sha")]
    public string? TargetSha { get; set; }

    [JsonPropertyName("line_count")]
    public int? LineCount { get; set; }

    [JsonPropertyName("ops")]
    public IList<DiffOpSnapshot> Ops { get; set; } = new List<DiffOpSnapshot>();

    [JsonPropertyName("stats")]
    public DiffCaseStats? Stats { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

/// <summary>Aggregated diff statistics to simplify sanity checks.</summary>
public sealed class DiffCaseStats
{
    [JsonPropertyName("copied_bytes")]
    public int CopiedBytes { get; set; }

    [JsonPropertyName("inserted_bytes")]
    public int InsertedBytes { get; set; }

    [JsonPropertyName("deleted_bytes")]
    public int DeletedBytes { get; set; }
}

/// <summary>Serialised <c>DiffOp</c> entry.</summary>
public sealed class DiffOpSnapshot
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("base_range")]
    public RangeSnapshot? BaseRange { get; set; }

    [JsonPropertyName("target_range")]
    public RangeSnapshot? TargetRange { get; set; }

    [JsonPropertyName("byte_len")]
    public int ByteLength { get; set; }

    [JsonPropertyName("line_span")]
    public DiffLineSpan? LineSpan { get; set; }

    [JsonPropertyName("insert_preview")]
    public string? InsertPreview { get; set; }
}

/// <summary>Captures the logical line span for an operation.</summary>
public sealed class DiffLineSpan
{
    [JsonPropertyName("base")]
    public int[] Base { get; set; } = Array.Empty<int>();

    [JsonPropertyName("target")]
    public int[] Target { get; set; } = Array.Empty<int>();
}

/// <summary>Read-only projection for loading scenarios.</summary>
public readonly record struct DiffCaseDescriptorView(
    string Sample,
    string BasePath,
    string TargetPath,
    IReadOnlyList<DiffOpSnapshotView> Ops,
    DiffCaseStats? Stats,
    string? Notes)
{
    public static DiffCaseDescriptorView FromDescriptor(DiffCaseDescriptor descriptor)
    {
        return new DiffCaseDescriptorView(
            descriptor.Sample,
            descriptor.BasePath,
            descriptor.TargetPath,
            descriptor.Ops.Select(DiffOpSnapshotView.FromSnapshot).ToArray(),
            descriptor.Stats,
            descriptor.Notes);
    }
}

/// <summary>Immutable view for <see cref="DiffOpSnapshot"/>.</summary>
public readonly record struct DiffOpSnapshotView(
    string Kind,
    RangeSnapshot? BaseRange,
    RangeSnapshot? TargetRange,
    int ByteLength,
    DiffLineSpan? LineSpan,
    string? InsertPreview)
{
    public static DiffOpSnapshotView FromSnapshot(DiffOpSnapshot snapshot)
    {
        return new DiffOpSnapshotView(
            snapshot.Kind,
            snapshot.BaseRange,
            snapshot.TargetRange,
            snapshot.ByteLength,
            snapshot.LineSpan,
            snapshot.InsertPreview);
    }
}
