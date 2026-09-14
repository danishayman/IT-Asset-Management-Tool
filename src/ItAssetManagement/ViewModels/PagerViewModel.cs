namespace ItAssetManagement.ViewModels;

/// <summary>
/// Everything the pager partial needs. <paramref name="RouteValues"/> carries the current
/// filter so that paging preserves it instead of dropping the user back to an unfiltered
/// page two.
/// </summary>
public record PagerViewModel(int Page, int TotalPages, Dictionary<string, string?> RouteValues)
{
    private const int Window = 2;

    /// <summary>
    /// Page numbers to render: a small window either side of the current page, so the
    /// control stays a fixed width whether there are three pages or three hundred.
    /// </summary>
    public IEnumerable<int> VisiblePages
    {
        get
        {
            var first = Math.Max(1, Page - Window);
            var last = Math.Min(TotalPages, Page + Window);

            for (var i = first; i <= last; i++)
            {
                yield return i;
            }
        }
    }

    public bool ShowsFirst => Page - Window > 1;

    public bool ShowsLast => Page + Window < TotalPages;
}
