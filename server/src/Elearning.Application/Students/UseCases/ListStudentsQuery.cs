namespace Elearning.Application.Students;

public sealed record ListStudentsQuery(int Limit, string? Cursor, string? Search);
