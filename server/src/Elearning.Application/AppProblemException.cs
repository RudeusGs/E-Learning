namespace Elearning.Application;

public abstract class AppProblemException : Exception
{
    public AppProblemException(int status, string code, string title, string? detail = null)
        : base(detail ?? title)
    {
        Status = status;
        Code = code;
        Title = title;
        Detail = detail;
    }

    public int Status { get; }
    public string Code { get; }
    public string Title { get; }
    public string? Detail { get; }
}
