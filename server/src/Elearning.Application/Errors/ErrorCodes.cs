namespace Elearning.Application.Errors;

public static class ErrorCodes
{
    public const string AccountDisabled = "ACCOUNT_DISABLED";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string CheckpointRequired = "CHECKPOINT_REQUIRED";
    public const string LessonNotStarted = "LESSON_NOT_STARTED";
    public const string QuestionNotAvailable = "QUESTION_NOT_AVAILABLE";
    public const string QuizNotPassed = "QUIZ_NOT_PASSED";
    public const string VideoDurationRequired = "VIDEO_DURATION_REQUIRED";
    public const string VideoNotCompleted = "VIDEO_NOT_COMPLETED";
    public const string CourseArchived = "COURSE_ARCHIVED";
    public const string DuplicateLessonOrder = "DUPLICATE_LESSON_ORDER";
    public const string Forbidden = "FORBIDDEN";
    public const string HttpError = "HTTP_ERROR";
    public const string InternalError = "INTERNAL_ERROR";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string InvalidQuestionOption = "INVALID_QUESTION_OPTION";
    public const string InvalidRefreshToken = "INVALID_REFRESH_TOKEN";
    public const string InvalidRoleState = "INVALID_ROLE_STATE";
    public const string LessonLocked = "LESSON_LOCKED";
    public const string QuestionHasHistory = "QUESTION_HAS_HISTORY";
    public const string QuestionOrderConflict = "QUESTION_ORDER_CONFLICT";
    public const string RateLimited = "RATE_LIMITED";
    public const string RefreshRetryRequired = "REFRESH_RETRY_REQUIRED";
    public const string RefreshTokenExpired = "REFRESH_TOKEN_EXPIRED";
    public const string RefreshTokenReuseDetected = "REFRESH_TOKEN_REUSE_DETECTED";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string SessionRevoked = "SESSION_REVOKED";
    public const string Unauthenticated = "UNAUTHENTICATED";
    public const string ValidationFailed = "VALIDATION_FAILED";
}
