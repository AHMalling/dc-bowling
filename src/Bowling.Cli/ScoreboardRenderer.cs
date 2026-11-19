using System.Text;

namespace Bowling.Cli;

/// <summary>
/// Draws a scoreboard as text, like the sheet at a bowling alley. Pure: scoreboard in, string out.
/// <code>
/// +-----+-----+-----+-----+-----+-----+-----+-----+-----+-------+
/// |  1  |  2  |  3  |  4  |  5  |  6  |  7  |  8  |  9  |  10   |
/// | 1 4 | 4 5 | 6 / | 5 / | X   | - 1 | 7 / | 6 / | X   | 2 / 6 |
/// |   5 |  14 |  29 |  49 |  60 |  61 |  77 |  97 | 117 |   133 |
/// +-----+-----+-----+-----+-----+-----+-----+-----+-----+-------+
/// </code>
/// </summary>
internal static class ScoreboardRenderer
{
    private const int FrameWidth = 3;       // two marks: "7 /"
    private const int LastFrameWidth = 5;   // three marks: "X X X"

    public static string Render(Scoreboard scoreboard)
    {
        var border = new StringBuilder("+");
        var numbers = new StringBuilder("|");
        var marks = new StringBuilder("|");
        var totals = new StringBuilder("|");

        for (var number = 1; number <= Rules.FramesPerGame; number++)
        {
            var width = number == Rules.FramesPerGame ? LastFrameWidth : FrameWidth;
            var frame = number <= scoreboard.Frames.Count ? scoreboard.Frames[number - 1] : null;

            border.Append(new string('-', width + 2)).Append('+');
            numbers.Append($" {Center(number.ToString(), width)} |");
            marks.Append($" {(frame is null ? "" : string.Join(' ', Marks(frame.Pins))).PadRight(width)} |");
            totals.Append($" {(frame?.RunningTotal?.ToString() ?? "").PadLeft(width)} |");
        }

        var status = scoreboard.IsComplete ? "final" : "so far";
        return new StringBuilder()
            .AppendLine(border.ToString())
            .AppendLine(numbers.ToString())
            .AppendLine(marks.ToString())
            .AppendLine(totals.ToString())
            .AppendLine(border.ToString())
            .AppendLine($"Total: {scoreboard.Total} ({status})")
            .ToString();
    }

    /// <summary>
    /// Bowling notation for a frame's rolls: X = strike, / = spare, - = no pins.
    /// Clearing the rack with the first ball on it is a strike; clearing it with the second ball is a spare
    /// (so 0 then 10 is "- /", not "- X").
    /// </summary>
    internal static IEnumerable<string> Marks(IReadOnlyList<int> pins)
    {
        var standing = Rules.PinsPerRack;
        var firstBallOnRack = true;
        foreach (var knocked in pins)
        {
            var clearsRack = knocked == standing;
            yield return clearsRack ? (firstBallOnRack ? "X" : "/")
                : knocked == 0 ? "-"
                : knocked.ToString();

            standing -= knocked;
            firstBallOnRack = clearsRack;   // a cleared rack is reset, so the next ball is the first on it
            if (clearsRack)
            {
                standing = Rules.PinsPerRack;
            }
        }
    }

    private static string Center(string text, int width)
    {
        var left = (width - text.Length) / 2;
        return text.PadLeft(text.Length + left).PadRight(width);
    }
}
