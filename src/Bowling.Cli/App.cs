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
        if (!RollParser.TryParseRolls(text, out var rolls, out var parseError))
        {
            error.WriteLine(parseError);
            return InvalidInput;
        }

        var game = new Game();
        for (var i = 0; i < rolls.Count; i++)
        {
            try
            {
                game.Roll(rolls[i]);
            }
            catch (InvalidRollException e)
            {
                output.Write(ScoreboardRenderer.Render(game.Scoreboard));   // what was valid up to the error
                error.WriteLine($"Roll {i + 1} ({rolls[i]}) was rejected: {e.Message}");
                return InvalidInput;
            }
        }

        output.Write(ScoreboardRenderer.Render(game.Scoreboard));
        return Success;
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

            if (!RollParser.TryParseRoll(line, out var pins, out var parseError))
            {
                error.WriteLine(parseError);
                continue;
            }

            try
            {
                game.Roll(pins);
            }
            catch (InvalidRollException e)
            {
                error.WriteLine(e.Message);
                continue;
            }

            output.Write(ScoreboardRenderer.Render(game.Scoreboard));
        }

        output.WriteLine($"Game complete. Final score: {game.Scoreboard.Total}");
        return Success;
    }
}
