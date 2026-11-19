namespace Bowling.Tests;

/// <summary>AC1-AC7: scores of finished games, including every example from the brief.</summary>
public class ScoringTests
{
    [Fact]
    public void Gutter_game_scores_0()
    {
        Assert.Equal(0, TestGames.Repeat(0, 20).Scoreboard.Total);
    }

    [Fact]
    public void All_ones_scores_20()
    {
        Assert.Equal(20, TestGames.Repeat(1, 20).Scoreboard.Total);
    }

    [Fact]
    public void Spare_gets_the_next_roll_as_bonus()
    {
        var game = TestGames.With([5, 5, 3, .. Enumerable.Repeat(0, 17)]);

        Assert.Equal(13, game.Scoreboard.Frames[0].Score);   // 10 + bonus 3
        Assert.Equal(16, game.Scoreboard.Total);
    }

    [Fact]
    public void Strike_gets_the_next_two_rolls_as_bonus()
    {
        var game = TestGames.With([10, 3, 4, .. Enumerable.Repeat(0, 16)]);

        Assert.Equal(17, game.Scoreboard.Frames[0].Score);   // 10 + bonus 3 + 4
        Assert.Equal(24, game.Scoreboard.Total);
    }

    [Fact]
    public void Strike_bonus_can_span_two_frames_when_the_next_roll_is_also_a_strike()
    {
        var game = TestGames.With([10, 10, 4, 2, .. Enumerable.Repeat(0, 14)]);

        Assert.Equal(24, game.Scoreboard.Frames[0].Score);   // 10 + bonus 10 + 4
    }

    [Fact]
    public void Perfect_game_scores_300()
    {
        var scoreboard = TestGames.Repeat(10, 12).Scoreboard;

        Assert.Equal(300, scoreboard.Total);
        Assert.All(scoreboard.Frames, frame => Assert.Equal(30, frame.Score));
    }

    [Fact]
    public void All_spares_with_5s_scores_150()
    {
        Assert.Equal(150, TestGames.Repeat(5, 21).Scoreboard.Total);
    }

    [Fact]
    public void Brief_example_gives_the_running_totals_from_the_table()
    {
        var scoreboard = TestGames.With(TestGames.BriefExample).Scoreboard;

        Assert.Equal(
            new int?[] { 5, 14, 29, 49, 60, 61, 77, 97, 117, 133 },
            scoreboard.Frames.Select(f => f.RunningTotal));
        Assert.True(scoreboard.IsComplete);
    }

    [Theory]
    [InlineData(10, 10, 10, 30)]   // "10, 10, 10 in the 10th frame gives 30 points"
    [InlineData(1, 9, 10, 20)]     // "1, 9, 10 in the 10th frame gives 20 points"
    [InlineData(10, 3, 7, 20)]
    [InlineData(3, 4, null, 7)]    // open 10th frame: no bonus roll
    public void Tenth_frame_counts_its_bonus_rolls_once(int first, int second, int? third, int expected)
    {
        var game = TestGames.NineOpenFramesThen(first, second);
        if (third is int pins)
        {
            game.Roll(pins);
        }

        Assert.Equal(expected, game.Scoreboard.Frames[9].Score);
        Assert.Equal(expected, game.Scoreboard.Total);
    }
}

/// <summary>AC9: a game in progress only shows totals for settled frames.</summary>
public class InProgressScoringTests
{
    [Fact]
    public void New_game_has_no_frames_and_total_0()
    {
        var scoreboard = new Game().Scoreboard;

        Assert.Empty(scoreboard.Frames);
        Assert.Equal(0, scoreboard.Total);
        Assert.False(scoreboard.IsComplete);
    }

    [Fact]
    public void Unfinished_open_frame_has_no_score_yet()
    {
        var frame = TestGames.With(3).Scoreboard.Frames.Single();

        Assert.Equal(new[] { 3 }, frame.Pins);
        Assert.Null(frame.Score);
    }

    [Theory]
    [InlineData(10)]       // strike waiting for two bonus rolls
    [InlineData(10, 3)]    // strike waiting for one more bonus roll
    [InlineData(5, 5)]     // spare waiting for its bonus roll
    public void Strike_or_spare_has_no_score_until_its_bonus_rolls_arrive(params int[] rolls)
    {
        var scoreboard = TestGames.With(rolls).Scoreboard;

        Assert.Null(scoreboard.Frames[0].Score);
        Assert.Null(scoreboard.Frames[0].RunningTotal);
        Assert.Equal(0, scoreboard.Total);
    }

    [Fact]
    public void Strike_settles_when_its_second_bonus_roll_arrives()
    {
        var scoreboard = TestGames.With(10, 3, 4).Scoreboard;

        Assert.Equal(new int?[] { 17, 7 }, scoreboard.Frames.Select(f => f.Score));
        Assert.Equal(new int?[] { 17, 24 }, scoreboard.Frames.Select(f => f.RunningTotal));
        Assert.Equal(24, scoreboard.Total);
    }

    [Fact]
    public void Total_counts_only_settled_frames()
    {
        var scoreboard = TestGames.With(3, 4, 10).Scoreboard;   // frame 1 settled (7), frame 2 a pending strike

        Assert.Equal(7, scoreboard.Total);
        Assert.False(scoreboard.IsComplete);
    }

    [Fact]
    public void Tenth_frame_has_no_score_until_its_bonus_roll_is_rolled()
    {
        var game = TestGames.NineOpenFramesThen(10, 10);

        Assert.Null(game.Scoreboard.Frames[9].Score);
        Assert.False(game.Scoreboard.IsComplete);
    }
}
