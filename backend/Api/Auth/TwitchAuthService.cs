using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using backend.Data;
using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Api.Auth;

public interface ITwitchAuthService
{
    string BuildAuthorizeUrl(string state);

    Task<TwitchAuthenticatedUser> AuthenticateAsync(string code, CancellationToken cancellationToken);
}

public sealed class TwitchAuthService : ITwitchAuthService
{
    private readonly HttpClient _httpClient;
    private readonly TwitchAuthOptions _options;
    private readonly ApplicationDbContext _dbContext;
    private readonly IUserRoleService _userRoleService;

    public TwitchAuthService(
        HttpClient httpClient,
        IOptions<TwitchAuthOptions> options,
        ApplicationDbContext dbContext,
        IUserRoleService userRoleService
    )
    {
        _httpClient = httpClient;
        _options = options.Value;
        _dbContext = dbContext;
        _userRoleService = userRoleService;
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
        var token = await ExchangeCodeForTokenAsync(code, cancellationToken);
        var twitchUser = await GetTwitchUserAsync(token.AccessToken, cancellationToken);
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;
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
                EmailVerified = null, // Helix /users does not provide this flag.
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
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Twitch token exchange failed with status {(int)response.StatusCode}: {error}"
            );
        }

        var token =
            await response.Content.ReadFromJsonAsync<TwitchTokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Twitch token response is empty.");

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
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Twitch user request failed with status {(int)response.StatusCode}: {error}"
            );
        }

        var payload =
            await response.Content.ReadFromJsonAsync<TwitchUsersResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Twitch users response is empty.");
        var user = payload.Data.FirstOrDefault();
        if (user is null)
        {
            throw new InvalidOperationException("Twitch users response contains no user.");
        }

        return user;
    }
}

public static class TwitchStateGenerator
{
    public static string Create()
    {
        Span<byte> randomBytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        return Convert.ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

public sealed record TwitchAuthenticatedUser(
    Guid UserId,
    string TwitchUserId,
    string DisplayName,
    string[] Roles,
    bool IsNewUser
);

public sealed class TwitchTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
}

public sealed class TwitchUsersResponse
{
    public List<TwitchUserDto> Data { get; set; } = [];
}

public sealed class TwitchUserDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("broadcaster_type")]
    public string BroadcasterType { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("profile_image_url")]
    public string? ProfileImageUrl { get; set; }
}
