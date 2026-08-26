using Elearning.Api.Contracts.Auth;
using Elearning.Api.Mappings;
using Elearning.Api.Security;
using Elearning.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Elearning.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route(AuthRoutes.Controller)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(
    ILoginCommandHandler loginHandler,
    IGetCurrentUserQueryHandler currentUserHandler,
    IRefreshSessionCommandHandler refreshSessionHandler,
    ILogoutCommandHandler logoutHandler,
    AuthSessionHttpAdapter httpAdapter) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.Authentication)]
    [HttpPost(AuthRoutes.Login)]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var session = await loginHandler.ExecuteAsync(request.ToCommand(), cancellationToken);
        return Ok(httpAdapter.WriteSession(Response, session));
    }

    [Authorize]
    [HttpGet(AuthRoutes.CurrentUser)]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
    {
        var user = await currentUserHandler.ExecuteAsync(
            new GetCurrentUserQuery(User.GetRequiredUserId()),
            cancellationToken);
        return Ok(user.ToResponse());
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticationSession)]
    [HttpPost(AuthRoutes.Refresh)]
    public async Task<ActionResult<LoginResponse>> Refresh(CancellationToken cancellationToken)
    {
        var session = await refreshSessionHandler.ExecuteAsync(
            httpAdapter.CreateRefreshCommand(Request),
            cancellationToken);
        return Ok(httpAdapter.WriteSession(Response, session));
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticationSession)]
    [HttpPost(AuthRoutes.Logout)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        try
        {
            await logoutHandler.ExecuteAsync(
                httpAdapter.CreateLogoutCommand(Request, User),
                cancellationToken);
        }
        finally
        {
            httpAdapter.DeleteSessionCookie(Response);
        }

        return NoContent();
    }
}
