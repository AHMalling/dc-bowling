using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bowling.Cli;

/// <summary>
/// Turns text into numbers. It only checks that each token is a whole number;
/// whether the roll is legal is the <see cref="Game"/>'s job.
/// </summary>
internal static class RollParser
{
    private static readonly char[] Separators = [' ', ',', '\t', '\r', '\n'];

    public static bool TryParseRolls(
        string text, out List<int> rolls, [NotNullWhen(false)] out string? error)
    {
        rolls = [];
        var tokens = text.Split(Separators, StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < tokens.Length; i++)
        {
            if (!TryParseRoll(tokens[i], out var pins, out var tokenError))
            {
                error = $"Roll {i + 1}: {tokenError}";
                return false;
            }
            rolls.Add(pins);
        }

        error = null;
        return true;
    }

    public static bool TryParseRoll(string token, out int pins, [NotNullWhen(false)] out string? error)
    {
        if (int.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out pins))
        {
            error = null;
            return true;
        }

        error = $"'{token}' is not a whole number of pins.";
        return false;
    }
}
