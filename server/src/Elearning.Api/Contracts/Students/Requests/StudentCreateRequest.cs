namespace Elearning.Api.Contracts.Students.Requests;

public sealed record StudentCreateRequest(
    string FullName,
    string Email,
    string InitialPassword);
