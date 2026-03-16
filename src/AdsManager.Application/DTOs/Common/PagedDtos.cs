namespace AdsManager.Application.DTOs.Common;

public enum SortDirection
{
    Asc,
    Desc
}

public record PagedRequest
{
    private const int MaxPageSize = 200;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public SortDirection SortDirection { get; init; } = SortDirection.Desc;

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize
    {
        get
        {
            if (PageSize < 1)
                return 20;

            return PageSize > MaxPageSize ? MaxPageSize : PageSize;
        }
    }
}

public sealed record PagedResponse<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int Total,
    int TotalPages);
