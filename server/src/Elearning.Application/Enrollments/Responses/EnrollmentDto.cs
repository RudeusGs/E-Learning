using Elearning.Domain;

namespace Elearning.Application.Enrollments;

public sealed record EnrollmentDto(long Id, long StudentId, long CourseId, EnrollmentStatus Status);
