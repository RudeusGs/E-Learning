using Elearning.Application.Errors;
using Elearning.Application.Exceptions;
using Elearning.Application.Lessons;
using Elearning.Application.Security;
using Elearning.Domain;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Elearning.Infrastructure.Lessons;

public sealed class AdminLessonCommandHandler(
    ElearningDbContext dbContext,
    IContentSanitizer contentSanitizer,
    IVideoNormalizer videoNormalizer,
    TimeProvider timeProvider) : IAdminLessonCommandHandler
{
    public async Task<LessonAdminDto> ExecuteAsync(
        CreateLessonCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Lesson;
        var courseStatus = await dbContext.Courses
            .AsNoTracking()
            .Where(course => course.Id == command.CourseId)
            .Select(course => (CourseStatus?)course.Status)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ResourceNotFoundException("Course");

        if (courseStatus == CourseStatus.Archived)
        {
            throw new ConflictException(
                ErrorCodes.CourseArchived,
                "Cannot add a lesson to an archived course");
        }

        var video = videoNormalizer.Normalize(request.VideoUrl);
        var lesson = Lesson.Create(
            command.CourseId,
            request.Title,
            request.Description,
            contentSanitizer.Sanitize(request.ContentHtml),
            video?.Provider,
            video?.ExternalId,
            request.SortOrder,
            request.Status,
            timeProvider.GetUtcNow());
        dbContext.Lessons.Add(lesson);
        await SaveWithOrderConflictAsync(cancellationToken);
        return LessonMapper.ToAdminDto(lesson);
    }

    public async Task<LessonAdminDto> ExecuteAsync(
        UpdateLessonCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Lesson;
        if (request.Version is null)
        {
            throw new RequestValidationException(
                "Version is required",
                "Supply the version returned by the latest read.");
        }

        var lesson = await dbContext.Lessons.SingleOrDefaultAsync(
            candidate => candidate.Id == command.LessonId,
            cancellationToken)
            ?? throw new ResourceNotFoundException("Lesson");
        if (lesson.Version != request.Version)
        {
            throw new ResourceConcurrencyException("Lesson");
        }

        var video = videoNormalizer.Normalize(request.VideoUrl);
        lesson.UpdateDetails(
            request.Title,
            request.Description,
            contentSanitizer.Sanitize(request.ContentHtml),
            video?.Provider,
            video?.ExternalId,
            request.SortOrder,
            request.Status,
            timeProvider.GetUtcNow());
        await SaveWithOrderConflictAsync(cancellationToken);
        return LessonMapper.ToAdminDto(lesson);
    }

    public async Task ExecuteAsync(ArchiveLessonCommand command, CancellationToken cancellationToken)
    {
        var lesson = await dbContext.Lessons.SingleOrDefaultAsync(
            candidate => candidate.Id == command.LessonId,
            cancellationToken)
            ?? throw new ResourceNotFoundException("Lesson");
        lesson.Archive(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SaveWithOrderConflictAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException(
                ErrorCodes.DuplicateLessonOrder,
                "Lesson order conflicts with another lesson",
                "Choose a unique sort order within the course.");
        }
    }
}
