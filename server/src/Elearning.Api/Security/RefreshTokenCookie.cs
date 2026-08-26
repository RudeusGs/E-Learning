namespace Elearning.Api.Security;

public sealed class RefreshTokenCookie(IWebHostEnvironment environment)
{
    private const string ProductionCookieName = "__Secure-elearning-refresh";
    private const string DevelopmentCookieName = "elearning-refresh";
    private const string CookiePath = "/api/auth";

    public string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(GetCookieName(), out var token) ? token : null;

    public void Write(HttpResponse response, string refreshToken, DateTimeOffset expiresAtUtc)
    {
        response.Cookies.Append(
            GetCookieName(),
            refreshToken,
            CreateOptions(expiresAtUtc));
    }

    public void Delete(HttpResponse response)
    {
        response.Cookies.Delete(
            GetCookieName(),
            new CookieOptions
            {
                HttpOnly = true,
                Secure = UseSecureCookie(),
                SameSite = SameSiteMode.Strict,
                Path = CookiePath,
                IsEssential = true
            });
    }

    private CookieOptions CreateOptions(DateTimeOffset expiresAtUtc) => new()
    {
        HttpOnly = true,
        Secure = UseSecureCookie(),
        SameSite = SameSiteMode.Strict,
        Path = CookiePath,
        Expires = expiresAtUtc,
        IsEssential = true
    };

    private string GetCookieName() =>
        UseSecureCookie() ? ProductionCookieName : DevelopmentCookieName;

    private bool UseSecureCookie() => !environment.IsDevelopment();
}
