using Elearning.Domain;

namespace Elearning.Application.Progress;

public static class LessonProgressStates
{
    public const string NotStarted = "NOT_STARTED";
    public const string InProgress = "IN_PROGRESS";
    public const string Completed = "COMPLETED";

    public static string ToContractValue(this LessonProgressStatus? status) => status switch
    {
        null => NotStarted,
        LessonProgressStatus.InProgress => InProgress,
        LessonProgressStatus.Completed => Completed,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported lesson progress status.")
    };
}
