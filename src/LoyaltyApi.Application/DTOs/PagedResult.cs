namespace LoyaltyApi.Application.DTOs;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public bool HasNextPage => Page * PageSize < TotalCount;
}
