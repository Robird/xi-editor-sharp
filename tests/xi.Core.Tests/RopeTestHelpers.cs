using Xi.Core.Rope.Tree;
using Xunit;

namespace Xi.Core.Tests;

public static class RopeTestHelpers
{
    public static void AssertInvariants(Node node, bool enforceLeafMinimum = false)
    {
        var issues = node.CollectInvariantIssues(enforceLeafMinimum);

        if (issues.Count == 0)
        {
            return;
        }

        var message = string.Join(Environment.NewLine, issues);
        Assert.True(issues.Count == 0, $"Invariant violations detected:{Environment.NewLine}{message}");
    }

    public static Node GetRoot(Rope.Rope buffer) => buffer.DebugRoot;
}
