using Elearning.Domain;

namespace Elearning.Api.Contracts.Students.Responses;

public sealed record StudentListItemResponse(
    long Id,
    string FullName,
    string Email,
    int CourseCount,
    AccountStatus Status);
