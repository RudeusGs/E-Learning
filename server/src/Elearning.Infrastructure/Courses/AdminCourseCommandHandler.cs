using Elearning.Application.Courses;
using Elearning.Application.Exceptions;
using Elearning.Application.Security;
using Elearning.Domain;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Courses;

public sealed class AdminCourseCommandHandler(
    ElearningDbContext dbContext,
    TimeProvider timeProvider) : IAdminCourseCommandHandler
{
    public async Task<AdminCourseDetailDto> ExecuteAsync(
        CreateCourseCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Course;
        var course = Course.Create(
            request.Title,
            request.Description,
            ExternalUrlValidator.NormalizeOptionalHttpsUrl(request.ThumbnailUrl, "thumbnailUrl"),
            request.SortOrder,
            request.Status,
            timeProvider.GetUtcNow());

        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CourseMapper.ToDetailDto(course);
    }

    public async Task<AdminCourseDetailDto> ExecuteAsync(
        UpdateCourseCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Course;
        if (request.Version is null)
        {
            throw new RequestValidationException(
                "Version is required",
                "Supply the version returned by the latest read.");
        }

        var course = await dbContext.Courses.SingleOrDefaultAsync(
            course => course.Id == command.CourseId,
            cancellationToken)
            ?? throw new ResourceNotFoundException("Course");
        if (course.Version != request.Version)
        {
            throw new ResourceConcurrencyException("Course");
        }

        course.UpdateDetails(
            request.Title,
            request.Description,
            ExternalUrlValidator.NormalizeOptionalHttpsUrl(request.ThumbnailUrl, "thumbnailUrl"),
            request.SortOrder,
            request.Status,
            timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return CourseMapper.ToDetailDto(course);
    }

    public async Task ExecuteAsync(ArchiveCourseCommand command, CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses.SingleOrDefaultAsync(
            course => course.Id == command.CourseId,
            cancellationToken)
            ?? throw new ResourceNotFoundException("Course");
        course.Archive(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
