using System;
using System.Collections.Generic;
using System.Linq;
using BlockingModel.Core;

namespace BlockingModel.Tests;

public class BlockingPointRegistryTests
{
    [Fact]
    public void Registry_should_cover_all_known_blocks()
    {
        var expected = Enum.GetValues<BlockingPointKind>();
        Assert.Equal(expected.Length, BlockingPointRegistry.All.Count());
    }

    [Fact]
    public void Get_should_return_registered_specs()
    {
        var spec = BlockingPointRegistry.Get(BlockingPointKind.CursorLifecycle);
        Assert.Contains("cursor", spec.Objective, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Get_should_throw_for_unknown_kind()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() => BlockingPointRegistry.Get((BlockingPointKind)999));
        Assert.Contains("未注册", ex.Message);
    }
}
