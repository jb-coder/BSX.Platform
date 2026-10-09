namespace BSX.Modules.Identity.Application.Dtos;

/// <summary>A page of results.</summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int Size, int TotalCount);
