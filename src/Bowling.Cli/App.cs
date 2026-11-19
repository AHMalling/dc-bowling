namespace Bowling.Cli;

/// <summary>
/// The command-line scoreboard. Two modes share one <see cref="Game"/>, so the rules live in one place;
/// the modes only differ in how they react to an invalid roll:
/// <list type="bullet">
/// <item>Rolls given as arguments (or piped in): score them and stop at the first invalid roll with exit code 1,
/// so a script gets a clear failure.</item>
/// <item>Interactive: ask for one roll at a time; an invalid roll is reported and the person simply tries again.</item>
/// </list>
/// Input and output are parameters, so the whole CLI can be tested without starting a process.
/// </summary>
internal static class App
{
    public const int Success = 0;
    public const int InvalidInput = 1;

    private const string Usage = """
        Bowling scoreboard

        Usage:
          Bowling.Cli                      enter rolls one at a time
          Bowling.Cli 10 7 3 9 0 ...       score the given rolls
          echo "10 7 3 9 0" | Bowling.Cli  score rolls from standard input

        Each roll is the number of pins knocked down (0-10). Rolls may be separated by spaces or commas.
        """;

    public static int Run(string[] args, TextReader input, TextWriter output, TextWriter error, bool interactive)
    {
        if (args is ["-h"] or ["--help"])
        {
            output.WriteLine(Usage);
            return Success;
        }

        return interactive
            ? RunInteractive(input, output, error)
            : ScoreRolls(args.Length > 0 ? string.Join(' ', args) : input.ReadToEnd(), output, error);
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
                output.Write(ScoreboardRenderer.Render(game.Scoreboard));   // what was valid up to the error
                error.Write(RejectionReport.Describe(tokens, i, problem, gameWasComplete: game.IsComplete));
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

    private static int RunInteractive(TextReader input, TextWriter output, TextWriter error)
    {
        var game = new Game();
        output.WriteLine("Enter the pins knocked down by each roll. Type q to quit.");
        output.Write(ScoreboardRenderer.Render(game.Scoreboard));

        while (!game.IsComplete)
        {
            output.Write($"Frame {game.CurrentFrame}, {game.PinsStanding} pins standing > ");
            var line = input.ReadLine()?.Trim();
            if (line is null or "q" or "quit")
            {
                return Success;
            }

            var problem = TryRoll(game, line);
            if (problem is not null)
            {
                error.WriteLine($"{problem} Try again.");
                continue;
            }

            output.Write(ScoreboardRenderer.Render(game.Scoreboard));
        }

        output.WriteLine($"Game complete. Final score: {game.Scoreboard.Total}");
        return Success;
    }
}
