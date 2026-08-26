using Elearning.Domain;

namespace Elearning.Api.Contracts.Lessons.Responses;

public sealed record VideoResponse(VideoProvider Provider, string ExternalId);
