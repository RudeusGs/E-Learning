namespace Elearning.Application.Auth.Models;

public sealed record SessionLogoutResult(long? UserId, bool SessionFound);
