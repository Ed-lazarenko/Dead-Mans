namespace backend.Application.Configuration;

public static class HttpOriginValidator
{
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalizedOrigin = value.Trim().TrimEnd('/');
        if (!Uri.TryCreate(normalizedOrigin, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && string.IsNullOrEmpty(uri.AbsolutePath.Trim('/'))
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment)
            && string.IsNullOrEmpty(uri.UserInfo);
    }

    public static bool IsHttps(string? value)
    {
        return IsValid(value)
            && Uri.TryCreate(value?.Trim().TrimEnd('/'), UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps;
    }
}
