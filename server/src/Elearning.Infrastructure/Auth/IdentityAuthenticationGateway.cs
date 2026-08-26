using System.Globalization;
using Elearning.Application.Auth.Models;
using Elearning.Application.Auth.Ports;
using Elearning.Application.Auth.Security;
using Elearning.Application.Exceptions;
using Elearning.Domain;
using Elearning.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Auth;

public sealed class IdentityAuthenticationGateway(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : IIdentityAuthenticationGateway
{
    private static readonly ApplicationUser DummyUser = new();
    private static readonly PasswordHasher<ApplicationUser> DummyPasswordHasher = new();
    private static readonly string DummyPasswordHash =
        DummyPasswordHasher.HashPassword(DummyUser, "not-a-real-elearning-password");

    public async Task<CredentialAuthenticationResult> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = userManager.NormalizeEmail(email.Trim());
        var user = await userManager.Users.SingleOrDefaultAsync(
            candidate => candidate.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (user is null || user.Status != AccountStatus.Active)
        {
            EqualizeInvalidCredentialTiming(password);
            return new(null, user?.Id, AuthenticationFailureReason.InvalidCredentials);
        }

        var result = await signInManager.CheckPasswordSignInAsync(
            user,
            password,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                EqualizeInvalidCredentialTiming(password);
            }

            return new(
                null,
                user.Id,
                result.IsLockedOut
                    ? AuthenticationFailureReason.LockedOut
                    : AuthenticationFailureReason.InvalidCredentials);
        }

        return new(await MapAsync(user), user.Id, null);
    }

    public async Task<AuthenticatedUser?> FindActiveUserAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        var user = await userManager.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == userId && candidate.Status == AccountStatus.Active,
            cancellationToken);
        return user is null ? null : await MapAsync(user);
    }

    public void EqualizeInvalidCredentialTiming(string providedPassword) =>
        _ = DummyPasswordHasher.VerifyHashedPassword(DummyUser, DummyPasswordHash, providedPassword);

    private async Task<AuthenticatedUser> MapAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        if (roles.Count != 1)
        {
            throw AuthenticationException.InvalidRoleState(user.Id);
        }

        return AuthUserMapper.Map(user, roles[0]);
    }
}
