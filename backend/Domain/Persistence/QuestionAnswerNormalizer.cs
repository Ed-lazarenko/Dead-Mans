using System.Text;

namespace backend.Domain.Persistence;

/// <summary>
/// Canonical normalization for question answers. Used both when storing the
/// expected answer in the catalog and when comparing a submitted answer during
/// a round, so the two always agree on casing, whitespace and the ё/е variant.
/// </summary>
public static class QuestionAnswerNormalizer
{
    public static string Normalize(string? answer)
    {
        var input = (answer ?? string.Empty).Trim().ToLowerInvariant().Replace('ё', 'е');
        if (input.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(input.Length);
        var pendingWhitespace = false;
        foreach (var ch in input)
        {
            if (char.IsWhiteSpace(ch))
            {
                pendingWhitespace = true;
                continue;
            }

            if (pendingWhitespace && builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(ch);
            pendingWhitespace = false;
        }

        return builder.ToString();
    }
}
