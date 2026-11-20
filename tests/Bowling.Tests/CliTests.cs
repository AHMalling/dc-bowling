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
    public void An_invalid_roll_stops_the_app_and_explains_what_was_and_was_not_scored()
    {
        var result = RunCli(["3", "4", "7", "5", "2", "1"]);

        Assert.Equal(App.InvalidInput, result.ExitCode);
        Assert.Contains("Total: 7 (so far)", result.Output);   // the valid part is still shown
        Assert.Contains("Input: 3 4 7 [5] 2 1", result.Error);
        Assert.Contains(
            "Error: roll 4 (5) is not valid. Only 3 pins are standing in frame 2, so 5 cannot be knocked down.",
            result.Error);
        Assert.Contains(
            "The app stopped. The scoreboard above shows rolls 1-3. Also not scored: rolls 5-6, "
            + "because each roll after an invalid one could end up in the wrong frame.",
            result.Error);
        Assert.Contains("Fix roll 4 and run again.", result.Error);
    }

    [Fact]
    public void The_marker_points_at_the_rejected_roll()
    {
        var result = RunCli(["10", "10", "7", "5"]);

        var lines = result.Error.Split(Environment.NewLine);
        var inputLine = lines.Single(l => l.StartsWith("Input: "));
        var caretLine = lines.Single(l => l.Contains("^ roll 4"));
        Assert.Equal(inputLine.IndexOf('['), caretLine.IndexOf('^'));
    }

    [Fact]
    public void Text_that_is_not_a_number_is_reported_the_same_way()
    {
        var result = RunCli(["3", "x"]);

        Assert.Equal(App.InvalidInput, result.ExitCode);
        Assert.Contains("Error: roll 2 (x) is not valid. 'x' is not a whole number of pins.", result.Error);
        Assert.Contains("The app stopped. The scoreboard above shows roll 1.", result.Error);
        Assert.DoesNotContain("Also not scored", result.Error);   // nothing came after it
    }

    [Fact]
    public void An_invalid_first_roll_reports_that_nothing_was_scored()
    {
        var result = RunCli(["11", "3"]);

        Assert.Contains("The app stopped. No rolls were scored. Also not scored: roll 2,", result.Error);
    }

    [Fact]
    public void Rolls_after_the_game_is_over_are_reported_as_extra()
    {
        var result = RunCli([.. Enumerable.Repeat("0", 20), "3", "4"]);

        Assert.Equal(App.InvalidInput, result.ExitCode);
        Assert.Contains("Total: 0 (final)", result.Output);
        Assert.Contains("The game is complete", result.Error);
        Assert.Contains("Also not scored: roll 22.", result.Error);
        Assert.DoesNotContain("wrong frame", result.Error);   // that reason doesn't apply here
        Assert.Contains("Remove rolls 21-22 and run again.", result.Error);
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
