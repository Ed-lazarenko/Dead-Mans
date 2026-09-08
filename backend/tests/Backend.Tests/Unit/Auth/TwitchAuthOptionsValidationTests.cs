using backend.Application.Abstractions.Auth;

namespace Backend.Tests.Unit.Auth;

public sealed class TwitchAuthOptionsValidationTests
{
    [Theory]
    [InlineData("https://example.com/auth/callback", true, true)]
    [InlineData("http://localhost:5285/auth/callback", false, true)]
    [InlineData("http://example.com/auth/callback", true, false)]
    [InlineData("https://user@example.com/auth/callback", true, false)]
    [InlineData("https://example.com/auth/callback#token", true, false)]
    [InlineData("javascript:alert(1)", false, false)]
    public void IsValidRedirectUri_EnforcesSchemeAndProductionHttps(
        string uri,
        bool requireHttps,
        bool expected
    )
    {
        Assert.Equal(expected, TwitchAuthOptions.IsValidRedirectUri(uri, requireHttps));
    }

    [Fact]
    public void HasValidScopes_RequiresUniqueNonBlankValues()
    {
        Assert.True(TwitchAuthOptions.HasValidScopes(["openid", "user:read:email"]));
        Assert.False(TwitchAuthOptions.HasValidScopes(["openid", "openid"]));
        Assert.False(TwitchAuthOptions.HasValidScopes(["openid", " "]));
        Assert.False(TwitchAuthOptions.HasValidScopes([]));
    }
}
