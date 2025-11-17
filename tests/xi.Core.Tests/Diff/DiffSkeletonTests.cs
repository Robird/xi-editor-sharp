using Xi.Core.Diff;
using Xi.Core.Rope.StageD;
using Xunit;

namespace Xi.Core.Tests.Diff;

public sealed class DiffSkeletonTests
{
    [Fact]
    public void DiffRegion_from_descriptor_preserves_operations()
    {
        var descriptor = new DiffCaseDescriptorView(
            "sample_diff",
            "base.txt",
            "target.txt",
            new[]
            {
                new DiffOpSnapshotView(
                    "copy",
                    new RangeSnapshot { Start = 0, End = 4 },
                    new RangeSnapshot { Start = 0, End = 4 },
                    4,
                    null,
                    null),
                new DiffOpSnapshotView(
                    "insert",
                    null,
                    new RangeSnapshot { Start = 4, End = 9 },
                    5,
                    null,
                    "hello")
            },
            new DiffCaseStats { CopiedBytes = 4, InsertedBytes = 5, DeletedBytes = 0 },
            "stage-d smoke");

        var region = DiffRegion.FromDescriptor(descriptor);

        Assert.Equal("sample_diff", region.Sample);
        Assert.Equal("base.txt", region.BasePath);
        Assert.Equal("target.txt", region.TargetPath);
        Assert.Equal(2, region.Operations.Count);
        Assert.Contains(region.Operations, op => op.Kind == "insert");
    }
}
