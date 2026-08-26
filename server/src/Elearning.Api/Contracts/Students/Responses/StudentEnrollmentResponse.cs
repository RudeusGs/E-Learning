using Elearning.Domain;

namespace Elearning.Api.Contracts.Students.Responses;

public sealed record StudentEnrollmentResponse(
    long Id,
    long CourseId,
    string CourseTitle,
    EnrollmentStatus Status);
