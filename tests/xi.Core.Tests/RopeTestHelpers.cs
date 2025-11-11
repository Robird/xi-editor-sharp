using Xi.Core.Rope;
using Xunit;

namespace Xi.Core.Tests;

public static class RopeTestHelpers
{
    public static void AssertInvariants(RopeNode node, bool enforceLeafMinimum = false)
    {
        var exception = Record.Exception(() => node.ValidateInvariants(enforceLeafMinimum));
        Assert.Null(exception);
    }

    public static RopeNode GetRoot(RopeTextBuffer buffer) => buffer.DebugRoot;
}
