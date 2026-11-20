using Bowling.Cli;

namespace Bowling.Tests;

/// <summary>AC10: the CLI shows the scoreboard, and invalid input gives a readable error and exit code 1.</summary>
public class CliTests
{
    [Fact]
    public void Rolls_as_arguments_print_the_scoreboard()
    {
        var result = RunCli(TestGames.BriefExample.Select(r => r.ToString()).ToArray());

        Assert.Equal(App.Success, result.ExitCode);
        Assert.Contains("| 1 4 | 4 5 | 6 / | 5 / | X   | - 1 | 7 / | 6 / | X   | 2 / 6 |", result.Output);
        Assert.Contains("|   5 |  14 |  29 |  49 |  60 |  61 |  77 |  97 | 117 |   133 |", result.Output);
        Assert.Contains("Total: 133 (final)", result.Output);
        Assert.Empty(result.Error);
    }

    [Fact]
    public void Rolls_can_be_separated_by_commas_as_in_the_brief()
    {
        var result = RunCli(["10,", "10,", "10,10,10,10,10,10,10,10,10,10"]);

        Assert.Contains("Total: 300 (final)", result.Output);
    }

    [Fact]
    public void Rolls_can_be_piped_in()
    {
        var result = RunCli([], input: "10 3\n");

        Assert.Equal(App.Success, result.ExitCode);
        Assert.Contains("| X   | 3   |", result.Output);
        Assert.Contains("Total: 0 (so far)", result.Output);
    }

    [Fact]
    public void An_invalid_roll_stops_the_app_after_showing_the_rolls_before_it()
    {
        var result = RunCli(["3", "4", "7", "5", "2", "1"]);

        Assert.Equal(App.InvalidInput, result.ExitCode);
        Assert.Contains("Total: 7 (so far)", result.Output);   // rolls 1-3; nothing after the invalid roll
        Assert.Equal(
            "Error: roll 4 (5) is not valid. Only 3 pins are standing in frame 2, so 5 cannot be knocked down. "
            + "The app stopped there, so the scoreboard only shows the rolls before it." + Environment.NewLine,
            result.Error);
    }

    [Fact]
    public void Text_that_is_not_a_number_is_reported_the_same_way()
    {
        var result = RunCli(["3", "x"]);

        Assert.Equal(App.InvalidInput, result.ExitCode);
        Assert.Contains("Error: roll 2 (x) is not valid. 'x' is not a whole number of pins.", result.Error);
    }

    [Fact]
    public void A_roll_after_the_game_is_over_is_rejected()
    {
        var result = RunCli([.. Enumerable.Repeat("0", 20), "3"]);

        Assert.Equal(App.InvalidInput, result.ExitCode);
        Assert.Contains("Total: 0 (final)", result.Output);
        Assert.Contains("Error: roll 21 (3) is not valid. The game is complete", result.Error);
    }

    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    public void Help_shows_the_usage(string flag)
    {
        var result = RunCli([flag]);

        Assert.Equal(App.Success, result.ExitCode);
        Assert.Contains("Usage:", result.Output);
    }

    [Theory]
    [InlineData(new[] { 10 }, "X")]
    [InlineData(new[] { 7, 3 }, "7 /")]
    [InlineData(new[] { 0, 10 }, "- /")]
    [InlineData(new[] { 0, 0 }, "- -")]
    [InlineData(new[] { 10, 10, 10 }, "X X X")]   // 10th frame
    [InlineData(new[] { 10, 3, 7 }, "X 3 /")]     // 10th frame: spare on the fresh rack
    [InlineData(new[] { 5, 5, 10 }, "5 / X")]     // 10th frame: strike on the fresh rack
    public void Marks_use_bowling_notation(int[] pins, string expected)
    {
        Assert.Equal(expected, string.Join(' ', ScoreboardRenderer.Marks(pins)));
    }

    private sealed record CliResult(int ExitCode, string Output, string Error);

    private static CliResult RunCli(string[] args, string input = "")
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var exitCode = App.Run(args, new StringReader(input), output, error);
        return new CliResult(exitCode, output.ToString(), error.ToString());
    }
}
