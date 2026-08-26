using Elearning.Domain;

namespace Elearning.Api.Contracts.Students.Responses;

public sealed record StudentDetailResponse(
    long Id,
    string FullName,
    string Email,
    AccountStatus Status,
    IReadOnlyList<StudentEnrollmentResponse> Enrollments);
