using System.Security.Cryptography;
using System.Text;
using backend.Application.Abstractions.Auth;
using backend.Messaging;
using Microsoft.Extensions.Options;

namespace backend.Application.Features.Auth;

public sealed class TwitchAuthFlowService : ITwitchAuthFlowService
{
    private readonly ITwitchLoginService _twitchLoginService;
    private readonly TwitchAuthOptions _twitchAuthOptions;
    private readonly ILogger<TwitchAuthFlowService> _logger;

    public TwitchAuthFlowService(
        ITwitchLoginService twitchLoginService,
        IOptions<TwitchAuthOptions> twitchAuthOptions,
        ILogger<TwitchAuthFlowService> logger
    )
    {
        _twitchLoginService = twitchLoginService;
        _twitchAuthOptions = twitchAuthOptions.Value;
        _logger = logger;
    }

    public TwitchLoginChallenge BeginLogin()
    {
        var state = CreateState();
        return new TwitchLoginChallenge(state, _twitchLoginService.BuildAuthorizeUrl(state));
    }

    public async Task<TwitchLoginCompletion> CompleteLoginAsync(
        string code,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var authUser = await _twitchLoginService.AuthenticateAsync(code, cancellationToken);
            return new TwitchLoginCompletion(authUser, BuildFrontendRedirect("authenticated"));
        }
        catch (InactiveUserLoginException ex)
        {
            _logger.LogWarning(ex, AppMessages.Logs.TwitchInactiveUserSignIn, ex.UserId);
            return new TwitchLoginCompletion(
                null,
                BuildFrontendRedirect("error", "account_inactive")
            );
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppMessages.Logs.TwitchAuthCallbackFailed);
            return new TwitchLoginCompletion(
                null,
                BuildFrontendRedirect("error", "authentication_failed")
            );
        }
    }

    public string BuildFrontendRedirect(string status, string? reason = null)
    {
        var query = new Dictionary<string, string?> { ["status"] = status };
        if (!string.IsNullOrWhiteSpace(reason))
        {
            query["reason"] = reason;
        }

        return AddQueryString(_twitchAuthOptions.FrontendRedirectUri, query);
    }

    private static string CreateState()
    {
        Span<byte> randomBytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        return Convert.ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string AddQueryString(string uri, IReadOnlyDictionary<string, string?> queryParameters)
    {
        if (queryParameters.Count == 0)
        {
            return uri;
        }

        var fragmentIndex = uri.IndexOf('#');
        var baseUri = fragmentIndex >= 0 ? uri[..fragmentIndex] : uri;
        var fragment = fragmentIndex >= 0 ? uri[fragmentIndex..] : string.Empty;
        var separator = baseUri.Contains('?') ? '&' : '?';

        var queryBuilder = new StringBuilder(baseUri);
        foreach (var (key, value) in queryParameters)
        {
            queryBuilder.Append(separator);
            queryBuilder.Append(Uri.EscapeDataString(key));
            queryBuilder.Append('=');
            queryBuilder.Append(Uri.EscapeDataString(value ?? string.Empty));
            separator = '&';
        }

        queryBuilder.Append(fragment);
        return queryBuilder.ToString();
    }
}
