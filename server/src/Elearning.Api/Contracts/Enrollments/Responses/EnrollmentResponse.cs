using Elearning.Domain;

namespace Elearning.Api.Contracts.Enrollments.Responses;

public sealed record EnrollmentResponse(
    long Id,
    long StudentId,
    long CourseId,
    EnrollmentStatus Status);
