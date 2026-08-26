namespace Elearning.Api.Contracts.Common;

public sealed record CursorPageResponse<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    bool HasMore);
