using Xi.Core.Rope;
using Xunit;

namespace Xi.Core.Tests;

public static class RopeTestHelpers
{
    public static void AssertInvariants(RopeNode node, bool enforceLeafMinimum = false)
    {
        var issues = node.CollectInvariantIssues(enforceLeafMinimum);

        if (issues.Count == 0)
        {
            return;
        }

        var message = string.Join(Environment.NewLine, issues);
        Assert.True(issues.Count == 0, $"Invariant violations detected:{Environment.NewLine}{message}");
    }

    public static RopeNode GetRoot(RopeTextBuffer buffer) => buffer.DebugRoot;
}
