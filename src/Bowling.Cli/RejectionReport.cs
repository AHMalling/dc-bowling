using System.Text;

namespace Bowling.Cli;

/// <summary>
/// The error report when a roll given up front is rejected: which roll, why, what was and wasn't scored,
/// and what to do. Pure: it returns the text, so it can be tested on its own.
/// <code>
/// Input: 3 4 7 [5] 2 1
///              ^ roll 4
/// Error: roll 4 (5) is not valid. Only 3 pins are standing in frame 2, so 5 cannot be knocked down.
/// The app stopped. The scoreboard above shows rolls 1-3. Also not scored: rolls 5-6, because each roll after an invalid one could end up in the wrong frame.
/// Fix roll 4 and run again.
/// </code>
/// </summary>
internal static class RejectionReport
{
    private const string InputLabel = "Input: ";

    /// <param name="tokens">All rolls as given.</param>
    /// <param name="rejectedIndex">Zero-based index of the rejected roll.</param>
    /// <param name="problem">Why it was rejected.</param>
    /// <param name="gameWasComplete">True when the roll came after the game had ended.</param>
    public static string Describe(IReadOnlyList<string> tokens, int rejectedIndex, string problem, bool gameWasComplete)
    {
        var rollNumber = rejectedIndex + 1;

        var markedInput = string.Join(' ', tokens.Select((token, i) => i == rejectedIndex ? $"[{token}]" : token));
        var caretColumn = InputLabel.Length + tokens.Take(rejectedIndex).Sum(token => token.Length + 1);

        var stopped = new StringBuilder("The app stopped. ")
            .Append(rejectedIndex == 0 ? "No rolls were scored." : $"The scoreboard above shows {Rolls(1, rejectedIndex)}.");
        if (rollNumber < tokens.Count)
        {
            stopped.Append($" Also not scored: {Rolls(rollNumber + 1, tokens.Count)}")
                .Append(gameWasComplete ? "." : ", because each roll after an invalid one could end up in the wrong frame.");
        }

        var fix = gameWasComplete
            ? $"Remove {Rolls(rollNumber, tokens.Count)} and run again."
            : $"Fix roll {rollNumber} and run again.";

        return new StringBuilder()
            .AppendLine()
            .AppendLine(InputLabel + markedInput)
            .AppendLine(new string(' ', caretColumn) + $"^ roll {rollNumber}")
            .AppendLine($"Error: roll {rollNumber} ({tokens[rejectedIndex]}) is not valid. {problem}")
            .AppendLine(stopped.ToString())
            .AppendLine(fix)
            .ToString();
    }

    private static string Rolls(int from, int to) => from == to ? $"roll {from}" : $"rolls {from}-{to}";
}
