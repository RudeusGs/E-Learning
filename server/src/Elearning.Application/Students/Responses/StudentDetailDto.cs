using Elearning.Domain;

namespace Elearning.Application.Students;

public sealed record StudentDetailDto(
    long Id,
    string FullName,
    string Email,
    AccountStatus Status,
    IReadOnlyList<StudentEnrollmentDto> Enrollments);
