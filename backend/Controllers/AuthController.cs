using backend.Application.Abstractions.Auth;
using backend.Infrastructure.Auth;
using backend.Messaging;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace backend.Controllers;

[ApiController]
[Route("auth/twitch")]
public sealed class AuthController : ControllerBase
{
    private readonly ITwitchLoginService _twitchLoginService;
    private readonly TwitchAuthOptions _twitchAuthOptions;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ITwitchLoginService twitchLoginService,
        IOptions<TwitchAuthOptions> twitchAuthOptions,
        IWebHostEnvironment environment,
        ILogger<AuthController> logger
    )
    {
        _twitchLoginService = twitchLoginService;
        _twitchAuthOptions = twitchAuthOptions.Value;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet("login")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Login()
    {
        var state = TwitchStateGenerator.Create();
        Response.Cookies.Delete(AuthCookieNames.TwitchOAuthState, BuildOAuthStateCookieOptions());
        Response.Cookies.Append(
            AuthCookieNames.TwitchOAuthState,
            state,
            BuildOAuthStateCookieOptions()
        );

        var twitchAuthorizeUrl = _twitchLoginService.BuildAuthorizeUrl(state);
        return Redirect(twitchAuthorizeUrl);
    }

    [HttpGet("callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken
    )
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            _logger.LogWarning(AppMessages.Logs.TwitchOAuthErrorQuery, error);
            return Redirect(BuildFrontendRedirect("error", NormalizeFrontendReason(error)));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            _logger.LogWarning(AppMessages.Logs.TwitchOAuthMissingCode);
            return Redirect(BuildFrontendRedirect("error", "missing_code"));
        }

        if (string.IsNullOrWhiteSpace(state))
        {
            _logger.LogWarning(AppMessages.Logs.TwitchOAuthMissingState);
            return Redirect(BuildFrontendRedirect("error", "missing_state"));
        }

        if (!Request.Cookies.TryGetValue(AuthCookieNames.TwitchOAuthState, out var stateCookie))
        {
            _logger.LogWarning(AppMessages.Logs.TwitchOAuthStateCookieMissing);
            return Redirect(BuildFrontendRedirect("error", "state_cookie_missing"));
        }

        Response.Cookies.Delete(AuthCookieNames.TwitchOAuthState, BuildOAuthStateCookieOptions());

        if (!string.Equals(state, stateCookie, StringComparison.Ordinal))
        {
            _logger.LogWarning(AppMessages.Logs.TwitchOAuthStateMismatch);
            return Redirect(BuildFrontendRedirect("error", "state_mismatch"));
        }

        try
        {
            var authUser = await _twitchLoginService.AuthenticateAsync(code, cancellationToken);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, authUser.UserId.ToString()),
                new("twitch_user_id", authUser.TwitchUserId),
                new(ClaimTypes.Name, authUser.DisplayName)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );
            var principal = new ClaimsPrincipal(claimsIdentity);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                }
            );

            _logger.LogInformation(
                AppMessages.Logs.TwitchUserSignedIn,
                authUser.UserId,
                authUser.IsNewUser
            );
        }
        catch (InactiveUserLoginException ex)
        {
            _logger.LogWarning(ex, AppMessages.Logs.TwitchInactiveUserSignIn, ex.UserId);
            return Redirect(BuildFrontendRedirect("error", "account_inactive"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppMessages.Logs.TwitchAuthCallbackFailed);
            return Redirect(BuildFrontendRedirect("error", "authentication_failed"));
        }

        return Redirect(BuildFrontendRedirect("authenticated"));
    }

    private CookieOptions BuildOAuthStateCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment() || Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(10),
            Path = "/"
        };
    }

    private string BuildFrontendRedirect(string status, string? reason = null)
    {
        var query = new Dictionary<string, string?> { ["status"] = status };
        if (!string.IsNullOrWhiteSpace(reason))
        {
            query["reason"] = reason;
        }

        return QueryHelpers.AddQueryString(_twitchAuthOptions.FrontendRedirectUri, query);
    }

    private static string NormalizeFrontendReason(string reason)
    {
        return reason switch
        {
            "access_denied" => reason,
            _ => "unknown"
        };
    }
}
