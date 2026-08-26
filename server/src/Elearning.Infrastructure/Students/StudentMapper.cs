using Elearning.Application.Enrollments;
using Elearning.Application.Students;
using Elearning.Domain;
using Elearning.Infrastructure.Identity;

namespace Elearning.Infrastructure.Students;

internal static class StudentMapper
{
    public static StudentDetailDto ToDetail(
        ApplicationUser student,
        IReadOnlyList<StudentEnrollmentDto> enrollments) =>
        new(student.Id, student.FullName, student.Email ?? string.Empty, student.Status, enrollments);

    public static EnrollmentDto ToEnrollment(Enrollment enrollment) =>
        new(enrollment.Id, enrollment.StudentId, enrollment.CourseId, enrollment.Status);
}
