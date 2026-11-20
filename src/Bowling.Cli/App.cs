namespace Bowling.Cli;

/// <summary>
/// The command-line scoreboard: scores rolls given as arguments or piped in, and stops at the first invalid roll
/// with exit code 1, so a script gets a clear failure. What counts as invalid is decided by <see cref="Game"/>.
/// Input and output are parameters, so the whole CLI can be tested without starting a process.
/// </summary>
internal static class App
{
    public const int Success = 0;
    public const int InvalidInput = 1;

    private const string Usage = """
        Bowling scoreboard

        Usage:
          Bowling.Cli 10 7 3 9 0 ...       score the given rolls
          echo "10 7 3 9 0" | Bowling.Cli  score rolls from standard input

        Each roll is the number of pins knocked down (0-10). Rolls may be separated by spaces or commas.
        An invalid roll stops the app with exit code 1.
        """;

    public static int Run(string[] args, TextReader input, TextWriter output, TextWriter error)
    {
        if (args is ["-h"] or ["--help"])
        {
            output.WriteLine(Usage);
            return Success;
        }

        return ScoreRolls(args.Length > 0 ? string.Join(' ', args) : input.ReadToEnd(), output, error);
    }

    private static int ScoreRolls(string text, TextWriter output, TextWriter error)
    {
        var tokens = RollParser.Split(text);
        var game = new Game();

        for (var i = 0; i < tokens.Length; i++)
        {
            var problem = TryRoll(game, tokens[i]);
            if (problem is not null)
            {
                // Stop rather than skip: every roll after an invalid one could land in the wrong frame.
                output.Write(ScoreboardRenderer.Render(game.Scoreboard));   // the valid rolls before it
                error.WriteLine(
                    $"Error: roll {i + 1} ({tokens[i]}) is not valid. {problem} The app stopped there, "
                    + "so the scoreboard only shows the rolls before it.");
                return InvalidInput;
            }
        }

        output.Write(ScoreboardRenderer.Render(game.Scoreboard));
        return Success;
    }

    /// <summary>Parses the token and rolls it. Returns why it was rejected, or null if the roll was recorded.</summary>
    private static string? TryRoll(Game game, string token)
    {
        if (!RollParser.TryParseRoll(token, out var pins, out var parseError))
        {
            return parseError;
        }

        try
        {
            game.Roll(pins);
            return null;
        }
        catch (InvalidRollException e)
        {
            return e.Message;
        }
    }
}
