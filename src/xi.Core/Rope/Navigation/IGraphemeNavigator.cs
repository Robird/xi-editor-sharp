using Xi.Core.Rope.Tree;

namespace Xi.Core.Rope.Navigation;

/// <summary>
/// Abstraction for grapheme-cluster navigation built on top of <see cref="NodeCursor"/>.
/// </summary>
public interface IGraphemeNavigator
{
    /// <summary>Exposes the telemetry sink backing this navigator.</summary>
    GraphemeNavigationMetrics Metrics { get; }

    /// <summary>Moves the supplied cursor to the next grapheme boundary, returning the absolute position or <c>null</c> at EOF.</summary>
    int? MoveNext(NodeCursor cursor);

    /// <summary>Moves the supplied cursor to the previous grapheme boundary, returning the absolute position or <c>null</c> at BOF.</summary>
    int? MovePrevious(NodeCursor cursor);

    /// <summary>Determines whether the cursor currently sits at a grapheme boundary.</summary>
    bool IsBoundary(NodeCursor cursor);
}
