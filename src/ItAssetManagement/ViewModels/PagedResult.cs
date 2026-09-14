namespace ItAssetManagement.ViewModels;

/// <summary>One page of results, plus what a pager needs to render itself.</summary>
public class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    /// <summary>Total matching rows across all pages, not just this one.</summary>
    public required int TotalCount { get; init; }

    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    public int FirstItemOnPage => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;

    public int LastItemOnPage => Math.Min(Page * PageSize, TotalCount);
}
