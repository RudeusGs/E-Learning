using Elearning.Domain;

namespace Elearning.Application.Students;

public sealed record StudentEnrollmentDto(long Id, long CourseId, string CourseTitle, EnrollmentStatus Status);
