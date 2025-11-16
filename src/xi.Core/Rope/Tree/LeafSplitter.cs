using System.Collections.Generic;

namespace Xi.Core.Rope.Tree;

internal static class LeafSplitter
{
    internal static readonly int NewlinePreferenceWindow = StringLeafOperations.MaxLeafSize - StringLeafOperations.MinLeafSize;

    internal static IEnumerable<string> Split(string leaf) => StringLeafOperations.SplitByCapacity(leaf);
}
