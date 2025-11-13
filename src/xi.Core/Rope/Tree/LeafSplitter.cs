using System.Collections.Generic;

namespace Xi.Core.Rope.Tree;

internal static class LeafSplitter
{
    internal const int NewlinePreferenceWindow = 64;

    internal static IEnumerable<string> Split(string leaf) => StringLeafOperations.SplitByCapacity(leaf);
}
