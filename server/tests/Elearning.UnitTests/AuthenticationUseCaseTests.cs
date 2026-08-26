using Elearning.Application.Auth;
using Elearning.Application.Auth.Models;
using Elearning.Application.Auth.Ports;
using Elearning.Application.Auth.Security;
using Elearning.Application.Exceptions;
using Elearning.Domain;
using Xunit;

namespace Elearning.UnitTests;

[Trait("Category", "Security")]
public sealed class AuthenticationUseCaseTests
{
    private static readonly UserDto User = new(42, "Student", "student@example.com", UserRole.Student);
    private static readonly AuthenticatedUser AuthenticatedUser =
        new(User, "student@example.com", "Student", "security-stamp-hash");
    private static readonly AuthSessionResult Session = new(
        User,
        "access-token",
        new DateTimeOffset(2030, 1, 1, 0, 5, 0, TimeSpan.Zero),
        "refresh-token",
        new DateTimeOffset(2030, 1, 31, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task LoginDelegatesAuthenticationAndSessionIssuance()
    {
        var identity = new StubIdentityGateway
        {
            AuthenticationResult = new CredentialAuthenticationResult(AuthenticatedUser, User.Id, null)
        };
        var issuer = new StubSessionIssuer(Session);
        var audit = new RecordingAuditWriter();
        var handler = new LoginCommandHandler(identity, issuer, audit);

        var result = await handler.ExecuteAsync(
            new LoginCommand(User.Email, "valid-password"),
            CancellationToken.None);

        Assert.Same(Session, result);
        Assert.Equal(User.Id, audit.SuccessfulLoginUserId);
        Assert.Equal(1, identity.AuthenticationCalls);
        Assert.Equal(1, issuer.IssueCalls);
    }

    [Fact]
    public async Task InvalidLoginShapeUsesGenericFailureAndEqualizesTiming()
    {
        var identity = new StubIdentityGateway();
        var audit = new RecordingAuditWriter();
        var handler = new LoginCommandHandler(identity, new StubSessionIssuer(Session), audit);

        var exception = await Assert.ThrowsAsync<AuthenticationException>(() =>
            handler.ExecuteAsync(new LoginCommand(string.Empty, "password"), CancellationToken.None));

        Assert.Equal("INVALID_CREDENTIALS", exception.Code);
        Assert.Equal(1, identity.TimingEqualizationCalls);
        Assert.Equal(0, identity.AuthenticationCalls);
        Assert.Equal(AuthenticationFailureReason.InvalidInput, audit.LastLoginFailureReason);
    }

    [Fact]
    public async Task RefreshReplayIsAuditedAndRethrown()
    {
        var audit = new RecordingAuditWriter();
        var handler = new RefreshSessionCommandHandler(
            new ThrowingRefreshRotator(new RefreshTokenReplayException(User.Id)),
            audit);

        await Assert.ThrowsAsync<RefreshTokenReplayException>(() =>
            handler.ExecuteAsync(new RefreshSessionCommand("replayed-token"), CancellationToken.None));

        Assert.Equal(User.Id, audit.ReplayUserId);
        Assert.Equal(User.Id, audit.RefreshFailureUserId);
        Assert.Equal(AuthenticationFailureReason.RefreshTokenReuseDetected, audit.LastRefreshFailureReason);
    }

    [Fact]
    public async Task LogoutWritesAuditOutcomeFromSessionService()
    {
        var audit = new RecordingAuditWriter();
        var handler = new LogoutCommandHandler(
            new StubLogoutService(new SessionLogoutResult(User.Id, true)),
            audit);

        await handler.ExecuteAsync(new LogoutCommand(null, null, null), CancellationToken.None);

        Assert.Equal(User.Id, audit.LogoutUserId);
        Assert.True(audit.LogoutSessionFound);
    }

    private sealed class StubIdentityGateway : IIdentityAuthenticationGateway
    {
        public CredentialAuthenticationResult AuthenticationResult { get; init; } =
            new(null, null, AuthenticationFailureReason.InvalidCredentials);
        public int AuthenticationCalls { get; private set; }
        public int TimingEqualizationCalls { get; private set; }

        public Task<CredentialAuthenticationResult> AuthenticateAsync(
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            AuthenticationCalls++;
            return Task.FromResult(AuthenticationResult);
        }

        public Task<AuthenticatedUser?> FindActiveUserAsync(long userId, CancellationToken cancellationToken) =>
            Task.FromResult<AuthenticatedUser?>(AuthenticatedUser);

        public void EqualizeInvalidCredentialTiming(string providedPassword) => TimingEqualizationCalls++;
    }

    private sealed class StubSessionIssuer(AuthSessionResult result) : IAuthSessionIssuer
    {
        public int IssueCalls { get; private set; }

        public Task<AuthSessionResult> IssueAsync(
            AuthenticatedUser user,
            CancellationToken cancellationToken)
        {
            IssueCalls++;
            return Task.FromResult(result);
        }
    }

    private sealed class ThrowingRefreshRotator(Exception exception) : IRefreshSessionRotator
    {
        public Task<AuthSessionResult> RotateAsync(string refreshToken, CancellationToken cancellationToken) =>
            Task.FromException<AuthSessionResult>(exception);
    }

    private sealed class StubLogoutService(SessionLogoutResult result) : ISessionLogoutService
    {
        public Task<SessionLogoutResult> LogoutAsync(
            LogoutCommand command,
            CancellationToken cancellationToken) => Task.FromResult(result);
    }

    private sealed class RecordingAuditWriter : ISecurityAuditWriter
    {
        public long? SuccessfulLoginUserId { get; private set; }
        public AuthenticationFailureReason? LastLoginFailureReason { get; private set; }
        public long? RefreshFailureUserId { get; private set; }
        public AuthenticationFailureReason? LastRefreshFailureReason { get; private set; }
        public long? ReplayUserId { get; private set; }
        public long? LogoutUserId { get; private set; }
        public bool LogoutSessionFound { get; private set; }

        public void LoginSucceeded(long userId) => SuccessfulLoginUserId = userId;

        public void LoginFailed(long? userId, AuthenticationFailureReason reason) =>
            LastLoginFailureReason = reason;

        public void RefreshFailed(long? userId, AuthenticationFailureReason reason)
        {
            RefreshFailureUserId = userId;
            LastRefreshFailureReason = reason;
        }

        public void RefreshTokenReplayDetected(long userId) => ReplayUserId = userId;

        public void LogoutCompleted(long? userId, bool sessionFound)
        {
            LogoutUserId = userId;
            LogoutSessionFound = sessionFound;
        }

        public void AccountDisabled(long actorUserId, long targetUserId)
        {
        }
    }
}
