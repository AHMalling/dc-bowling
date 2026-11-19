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

    /// <summary>Splits input like "10, 7 3" into one token per roll.</summary>
    public static string[] Split(string text) =>
        text.Split(Separators, StringSplitOptions.RemoveEmptyEntries);

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
