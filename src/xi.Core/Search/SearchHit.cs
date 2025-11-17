using Xi.Core.Rope.StageD;

namespace Xi.Core.Search;

/// <summary>Immutable hit representation suitable for search smoke tests.</summary>
public readonly record struct SearchHit(int Index, RangeSnapshot Range, int Line, string? ContextBefore, string? ContextAfter)
{
    public static SearchHit FromDescriptor(SearchHitView view)
    {
        return new SearchHit(view.Index, view.Range, view.Line, view.ContextBefore, view.ContextAfter);
    }
}
