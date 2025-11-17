using Xi.Core.Rope.StageD;

namespace Xi.Core.Search;

/// <summary>Represents a highlighted span window from Stage D fixtures.</summary>
public readonly record struct SearchSpanWindow(RangeSnapshot Range, int StyleId, string StyleTag, int Priority)
{
    public static SearchSpanWindow FromDescriptor(SearchSpanSegmentView view)
    {
        return new SearchSpanWindow(view.Range, view.StyleId, view.StyleTag, view.Priority);
    }
}
