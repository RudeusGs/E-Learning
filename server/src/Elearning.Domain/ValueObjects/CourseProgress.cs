namespace Elearning.Domain;

public readonly record struct CourseProgress
{
    private CourseProgress(int completedLessons, int totalLessons)
    {
        CompletedLessons = completedLessons;
        TotalLessons = totalLessons;
        Percentage = totalLessons == 0
            ? 0
            : (int)Math.Round(completedLessons * 100m / totalLessons, MidpointRounding.AwayFromZero);
    }

    public int CompletedLessons { get; }
    public int TotalLessons { get; }
    public int Percentage { get; }

    public static CourseProgress Calculate(int completedLessons, int totalLessons)
    {
        if (completedLessons < 0 || totalLessons < 0 || completedLessons > totalLessons)
        {
            throw new DomainValidationException("Course progress counts are inconsistent.");
        }

        return new CourseProgress(completedLessons, totalLessons);
    }
}
