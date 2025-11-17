using System;
using Xi.Core.Rope.StageD;

namespace Xi.Core.Diff;

/// <summary>Immutable placeholder mirroring Rust's DiffOp for Stage D testing.</summary>
public sealed class DiffOperation
{
    public DiffOperation(
        string kind,
        RangeSnapshot? baseRange,
        RangeSnapshot? targetRange,
        int byteLength,
        DiffLineSpan? lineSpan,
        string? insertPreview)
    {
        Kind = kind ?? throw new ArgumentNullException(nameof(kind));
        BaseRange = baseRange;
        TargetRange = targetRange;
        ByteLength = byteLength;
        LineSpan = lineSpan;
        InsertPreview = insertPreview;
    }

    public string Kind { get; }

    public RangeSnapshot? BaseRange { get; }

    public RangeSnapshot? TargetRange { get; }

    public int ByteLength { get; }

    public DiffLineSpan? LineSpan { get; }

    public string? InsertPreview { get; }

    public static DiffOperation FromDescriptor(DiffOpSnapshotView descriptor)
    {
        return new DiffOperation(
            descriptor.Kind,
            descriptor.BaseRange,
            descriptor.TargetRange,
            descriptor.ByteLength,
            descriptor.LineSpan,
            descriptor.InsertPreview);
    }
}
