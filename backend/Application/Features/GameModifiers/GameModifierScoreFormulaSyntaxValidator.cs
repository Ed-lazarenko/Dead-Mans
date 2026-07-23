using backend.Application.Contracts;

namespace backend.Application.Features.GameModifiers;

internal static class GameModifierScoreFormulaSyntaxValidator
{
    private static readonly HashSet<string> SupportedFunctions = new(
        ["min", "max", "round", "floor", "ceil", "abs"],
        StringComparer.Ordinal
    );

    private static readonly HashSet<string> SupportedVariables = new(
        [
            "killscount",
            "bountycount",
            "scoreunit",
            "basescore",
            "perkillbonus",
            "failurepenaltypoints",
            "activationcount",
            "totaloutcomecount"
        ],
        StringComparer.Ordinal
    );

    private static readonly ModifierScoreFormulaContext ValidationContext = new(
        KillsCount: 1,
        BountyCount: 1,
        ScoreUnit: 1,
        BaseScore: 1,
        PerKillBonus: 1,
        FailurePenaltyPoints: 1,
        ActivationCount: 1,
        TotalOutcomeCount: 2
    );

    public static bool IsValid(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return false;
        }

        try
        {
            _ = Evaluate(expression, ValidationContext);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static double Evaluate(string expression, ModifierScoreFormulaContext context)
    {
        var tokens = Tokenize(expression);
        var cursor = 0;

        double ParseExpression()
        {
            var value = ParseTerm();

            while (cursor < tokens.Count)
            {
                var token = tokens[cursor];
                if (token.Type != FormulaTokenType.Operator || token.Value is not ("+" or "-"))
                {
                    break;
                }

                cursor += 1;
                var right = ParseTerm();
                value = token.Value == "+" ? value + right : value - right;
            }

            return value;
        }

        double ParseTerm()
        {
            var value = ParseUnary();

            while (cursor < tokens.Count)
            {
                var token = tokens[cursor];
                if (token.Type != FormulaTokenType.Operator || token.Value is not ("*" or "/"))
                {
                    break;
                }

                cursor += 1;
                var right = ParseUnary();
                if (token.Value == "*")
                {
                    value *= right;
                }
                else
                {
                    if (Math.Abs(right) < double.Epsilon)
                    {
                        throw new InvalidOperationException("Division by zero is not allowed.");
                    }

                    value /= right;
                }
            }

            return value;
        }

        double ParseUnary()
        {
            if (cursor < tokens.Count
                && tokens[cursor].Type == FormulaTokenType.Operator
                && tokens[cursor].Value is "+" or "-")
            {
                var token = tokens[cursor];
                cursor += 1;
                var value = ParseUnary();
                return token.Value == "-" ? -value : value;
            }

            return ParsePrimary();
        }

        double ParsePrimary()
        {
            if (cursor >= tokens.Count)
            {
                throw new InvalidOperationException("Unexpected end of formula.");
            }

            var token = tokens[cursor];
            switch (token.Type)
            {
                case FormulaTokenType.Number:
                    cursor += 1;
                    return token.NumberValue;
                case FormulaTokenType.Identifier:
                    cursor += 1;
                    if (cursor < tokens.Count
                        && tokens[cursor].Type == FormulaTokenType.Paren
                        && tokens[cursor].Value == "(")
                    {
                        cursor += 1;
                        var args = new List<double>();
                        if (!(cursor < tokens.Count
                              && tokens[cursor].Type == FormulaTokenType.Paren
                              && tokens[cursor].Value == ")"))
                        {
                            while (true)
                            {
                                args.Add(ParseExpression());
                                if (cursor < tokens.Count
                                    && tokens[cursor].Type == FormulaTokenType.Comma)
                                {
                                    cursor += 1;
                                    continue;
                                }

                                break;
                            }
                        }

                        if (cursor >= tokens.Count
                            || tokens[cursor].Type != FormulaTokenType.Paren
                            || tokens[cursor].Value != ")")
                        {
                            throw new InvalidOperationException("Expected closing parenthesis.");
                        }

                        cursor += 1;
                        return ExecuteFunction(token.Value, args);
                    }

                    return ResolveVariable(token.Value, context);
                case FormulaTokenType.Paren when token.Value == "(":
                    cursor += 1;
                    var value = ParseExpression();
                    if (cursor >= tokens.Count
                        || tokens[cursor].Type != FormulaTokenType.Paren
                        || tokens[cursor].Value != ")")
                    {
                        throw new InvalidOperationException("Expected closing parenthesis.");
                    }

                    cursor += 1;
                    return value;
                default:
                    throw new InvalidOperationException("Unexpected token in formula.");
            }
        }

        var result = ParseExpression();
        if (cursor < tokens.Count)
        {
            throw new InvalidOperationException("Formula contains trailing tokens.");
        }

        return result;
    }

    private static List<FormulaToken> Tokenize(string expression)
    {
        var tokens = new List<FormulaToken>();
        var normalized = expression.Trim();
        var cursor = 0;

        while (cursor < normalized.Length)
        {
            var current = normalized[cursor];
            if (char.IsWhiteSpace(current))
            {
                cursor += 1;
                continue;
            }

            if (char.IsDigit(current) || current == '.')
            {
                var start = cursor;
                cursor += 1;
                while (cursor < normalized.Length
                       && (char.IsDigit(normalized[cursor]) || normalized[cursor] == '.'))
                {
                    cursor += 1;
                }

                if (!double.TryParse(
                        normalized[start..cursor],
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var value)
                    || double.IsNaN(value)
                    || double.IsInfinity(value))
                {
                    throw new InvalidOperationException("Invalid number in formula.");
                }

                tokens.Add(new FormulaToken(FormulaTokenType.Number, string.Empty, value));
                continue;
            }

            if (char.IsLetter(current) || current == '_')
            {
                var start = cursor;
                cursor += 1;
                while (cursor < normalized.Length
                       && (char.IsLetterOrDigit(normalized[cursor]) || normalized[cursor] == '_'))
                {
                    cursor += 1;
                }

                tokens.Add(
                    new FormulaToken(
                        FormulaTokenType.Identifier,
                        normalized[start..cursor].ToLowerInvariant(),
                        0
                    )
                );
                continue;
            }

            if (current is '+' or '-' or '*' or '/')
            {
                tokens.Add(new FormulaToken(FormulaTokenType.Operator, current.ToString(), 0));
                cursor += 1;
                continue;
            }

            if (current is '(' or ')')
            {
                tokens.Add(new FormulaToken(FormulaTokenType.Paren, current.ToString(), 0));
                cursor += 1;
                continue;
            }

            if (current == ',')
            {
                tokens.Add(new FormulaToken(FormulaTokenType.Comma, ",", 0));
                cursor += 1;
                continue;
            }

            throw new InvalidOperationException($"Unsupported character '{current}'.");
        }

        return tokens;
    }

    private static double ResolveVariable(string variableName, ModifierScoreFormulaContext context)
    {
        if (!SupportedVariables.Contains(variableName))
        {
            throw new InvalidOperationException($"Unsupported variable '{variableName}'.");
        }

        return variableName switch
        {
            "killscount" => context.KillsCount,
            "bountycount" => context.BountyCount,
            "scoreunit" => context.ScoreUnit,
            "basescore" => context.BaseScore,
            "perkillbonus" => context.PerKillBonus,
            "failurepenaltypoints" => context.FailurePenaltyPoints,
            "activationcount" => context.ActivationCount,
            "totaloutcomecount" => context.TotalOutcomeCount,
            _ => 0
        };
    }

    private static double ExecuteFunction(string functionName, IReadOnlyList<double> args)
    {
        if (!SupportedFunctions.Contains(functionName))
        {
            throw new InvalidOperationException($"Unsupported function '{functionName}'.");
        }

        return functionName switch
        {
            "min" when args.Count >= 2 => args.Min(),
            "max" when args.Count >= 2 => args.Max(),
            "round" when args.Count == 1 => Math.Round(args[0]),
            "floor" when args.Count == 1 => Math.Floor(args[0]),
            "ceil" when args.Count == 1 => Math.Ceiling(args[0]),
            "abs" when args.Count == 1 => Math.Abs(args[0]),
            _ => throw new InvalidOperationException($"Invalid arguments for function '{functionName}'.")
        };
    }

    private sealed record ModifierScoreFormulaContext(
        int KillsCount,
        int BountyCount,
        int ScoreUnit,
        int BaseScore,
        int PerKillBonus,
        int FailurePenaltyPoints,
        int ActivationCount,
        int TotalOutcomeCount
    );

    private enum FormulaTokenType
    {
        Number,
        Identifier,
        Operator,
        Paren,
        Comma
    }

    private sealed record FormulaToken(FormulaTokenType Type, string Value, double NumberValue);
}
