using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using backend.Application.Abstractions.Auth;
using backend.Data;
using backend.Data.Entities;
using backend.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Infrastructure.Auth;

public sealed class TwitchLoginService : ITwitchLoginService
{
    private const int MaximumAccessTokenLength = 4096;
    private const int MaximumEmailLength = 320;
    private const int MaximumProfileImageUrlLength = 1024;
    private const int MaximumTwitchTypeLength = 32;

    private readonly HttpClient _httpClient;
    private readonly TwitchAuthOptions _options;
    private readonly ApplicationDbContext _dbContext;
    private readonly IUserRoleService _userRoleService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TwitchLoginService> _logger;

    public TwitchLoginService(
        HttpClient httpClient,
        IOptions<TwitchAuthOptions> options,
        ApplicationDbContext dbContext,
        IUserRoleService userRoleService,
        TimeProvider timeProvider,
        ILogger<TwitchLoginService> logger
    )
    {
        _httpClient = httpClient;
        _options = options.Value;
        _dbContext = dbContext;
        _userRoleService = userRoleService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public string BuildAuthorizeUrl(string state)
    {
        var scope = string.Join(
            ' ',
            _options.Scopes
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.Ordinal)
        );
        return
            "https://id.twitch.tv/oauth2/authorize"
            + $"?client_id={Uri.EscapeDataString(_options.ClientId)}"
            + $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}"
            + "&response_type=code"
            + $"&scope={Uri.EscapeDataString(scope)}"
            + $"&state={Uri.EscapeDataString(state)}";
    }

    public async Task<TwitchAuthenticatedUser> AuthenticateAsync(
        string code,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var token = await ExchangeCodeForTokenAsync(code, cancellationToken);
            var twitchUser = await GetTwitchUserAsync(token.AccessToken, cancellationToken);
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.TwitchUserId == twitchUser.Id, cancellationToken);

            var isNewUser = user is null;
            if (isNewUser)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    TwitchUserId = twitchUser.Id,
                    Login = twitchUser.Login,
                    DisplayName = twitchUser.DisplayName,
                    Email = twitchUser.Email,
                    ProfileImageUrl = twitchUser.ProfileImageUrl,
                    BroadcasterType = twitchUser.BroadcasterType,
                    TwitchUserType = twitchUser.Type,
                    IsActive = true,
                    CreatedAtUtc = utcNow,
                    UpdatedAtUtc = utcNow,
                    LastLoginAtUtc = utcNow
                };
                _dbContext.Users.Add(user);
            }
            else
            {
                if (!user!.IsActive)
                {
                    throw new InactiveUserLoginException(user.Id);
                }

                user!.Login = twitchUser.Login;
                user.DisplayName = twitchUser.DisplayName;
                user.Email = twitchUser.Email;
                user.ProfileImageUrl = twitchUser.ProfileImageUrl;
                user.BroadcasterType = twitchUser.BroadcasterType;
                user.TwitchUserType = twitchUser.Type;
                user.LastLoginAtUtc = utcNow;
                user.UpdatedAtUtc = utcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            var roleCodes = await _userRoleService.EnsureEffectiveRolesAsync(user.Id, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new TwitchAuthenticatedUser(
                user.Id,
                user.TwitchUserId,
                user.DisplayName,
                roleCodes,
                isNewUser
            );
        }
        catch (InactiveUserLoginException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppMessages.Logs.TwitchAuthTokenExchangeFailed);
            throw;
        }
    }

    private async Task<TwitchTokenResponse> ExchangeCodeForTokenAsync(
        string code,
        CancellationToken cancellationToken
    )
    {
        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = _options.RedirectUri
            }
        );

        using var response = await _httpClient.PostAsync(
            "https://id.twitch.tv/oauth2/token",
            content,
            cancellationToken
        );
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                AppMessages.Logs.TwitchTokenExchangeHttpFailed,
                (int)response.StatusCode
            );
            throw new InvalidOperationException(
                AppMessages.Exceptions.TwitchTokenExchangeFailed((int)response.StatusCode)
            );
        }

        var token =
            await response.Content.ReadFromJsonAsync<TwitchTokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException(AppMessages.Exceptions.TwitchTokenResponseEmpty);

        if (
            string.IsNullOrWhiteSpace(token.AccessToken)
            || token.AccessToken.Length > MaximumAccessTokenLength
        )
        {
            throw new InvalidOperationException(AppMessages.Exceptions.TwitchTokenResponseInvalid);
        }

        return token;
    }

    private async Task<TwitchUserDto> GetTwitchUserAsync(
        string accessToken,
        CancellationToken cancellationToken
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.twitch.tv/helix/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Client-Id", _options.ClientId);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                AppMessages.Logs.TwitchHelixUsersRequestFailed,
                (int)response.StatusCode
            );
            throw new InvalidOperationException(
                AppMessages.Exceptions.TwitchUserRequestFailed((int)response.StatusCode)
            );
        }

        var payload =
            await response.Content.ReadFromJsonAsync<TwitchUsersResponse>(cancellationToken)
            ?? throw new InvalidOperationException(AppMessages.Exceptions.TwitchUsersResponseEmpty);
        if (payload.Data is null || payload.Data.Count == 0)
        {
            _logger.LogWarning(AppMessages.Logs.TwitchHelixNoUserEntries);
            throw new InvalidOperationException(AppMessages.Exceptions.TwitchUsersResponseNoUser);
        }

        if (payload.Data.Count != 1 || !HasValidIdentity(payload.Data[0]))
        {
            throw new InvalidOperationException(AppMessages.Exceptions.TwitchUsersResponseInvalid);
        }

        return payload.Data[0];
    }

    private static bool HasValidIdentity(TwitchUserDto user)
    {
        return TwitchIdentityValidator.IsValid(user.Id, user.Login, user.DisplayName)
            && HasValidOptionalValue(user.Email, MaximumEmailLength)
            && HasValidProfileImageUrl(user.ProfileImageUrl)
            && HasValidOptionalValue(user.BroadcasterType, MaximumTwitchTypeLength)
            && HasValidOptionalValue(user.Type, MaximumTwitchTypeLength);
    }

    private static bool HasValidOptionalValue(string? value, int maximumLength)
    {
        return value is null || value.Length <= maximumLength;
    }

    private static bool HasValidProfileImageUrl(string? value)
    {
        return value is null
            || (
                value.Length <= MaximumProfileImageUrlLength
                && Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps
                && string.IsNullOrEmpty(uri.UserInfo)
            );
    }

    private sealed class TwitchTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class TwitchUsersResponse
    {
        public List<TwitchUserDto>? Data { get; set; } = [];
    }

    private sealed class TwitchUserDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("profile_image_url")]
        public string? ProfileImageUrl { get; set; }

        [JsonPropertyName("broadcaster_type")]
        public string? BroadcasterType { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }
}
