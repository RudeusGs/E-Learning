using Elearning.Domain;

namespace Elearning.Application.Students;

public sealed record StudentListItemDto(
    long Id,
    string FullName,
    string Email,
    int CourseCount,
    AccountStatus Status);
