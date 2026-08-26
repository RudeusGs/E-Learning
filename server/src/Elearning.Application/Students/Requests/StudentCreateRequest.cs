namespace Elearning.Application.Students;

public sealed record StudentCreateRequest(string FullName, string Email, string InitialPassword);
