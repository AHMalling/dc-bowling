namespace Bowling.Tests;

/// <summary>
/// AC8: invalid rolls are rejected with a clear message and leave the game unchanged.
/// </summary>
public class GameValidationTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void Roll_outside_0_to_10_is_rejected(int pins)
    {
        var game = new Game();

        var error = Assert.Throws<InvalidRollException>(() => game.Roll(pins));

        Assert.Contains("between 0 and 10", error.Message);
    }

    [Fact]
    public void Two_rolls_in_a_frame_cannot_exceed_10()
    {
        var game = TestGames.With(7);

        var error = Assert.Throws<InvalidRollException>(() => game.Roll(5));

        Assert.Contains("Only 3 pins are standing in frame 1", error.Message);
    }

    [Fact]
    public void Frame_limit_is_checked_in_later_frames_too()
    {
        var game = TestGames.With(10, 7);   // frame 1 is a strike, frame 2 starts with 7

        var error = Assert.Throws<InvalidRollException>(() => game.Roll(4));

        Assert.Contains("frame 2", error.Message);
    }

    [Fact]
    public void A_strike_ends_the_frame_so_the_next_roll_has_all_10_pins()
    {
        var game = TestGames.With(10);

        game.Roll(10);

        Assert.Equal(new[] { 10, 10 }, game.Rolls);
    }

    [Theory]
    [InlineData(10, 10, 10)]   // strike, then two strikes as bonus
    [InlineData(10, 10, 5)]    // strike, strike, then a fresh rack for the last roll
    [InlineData(10, 3, 7)]     // strike, then 3 + 7 on the new rack
    [InlineData(5, 5, 10)]     // spare, then a fresh rack for the bonus roll
    [InlineData(0, 10, 10)]    // spare on the second roll
    public void Tenth_frame_allows_bonus_rolls_after_a_strike_or_spare(int first, int second, int third)
    {
        var game = TestGames.NineOpenFramesThen(first, second);

        game.Roll(third);

        Assert.True(game.IsComplete);
    }

    [Fact]
    public void Tenth_frame_bonus_rolls_after_a_strike_share_one_rack()
    {
        var game = TestGames.NineOpenFramesThen(10, 5);

        var error = Assert.Throws<InvalidRollException>(() => game.Roll(6));

        Assert.Contains("Only 5 pins are standing in frame 10", error.Message);
    }

    [Fact]
    public void An_open_tenth_frame_ends_the_game_after_two_rolls()
    {
        var game = TestGames.NineOpenFramesThen(3, 5);

        var error = Assert.Throws<InvalidRollException>(() => game.Roll(1));

        Assert.Contains("complete", error.Message);
    }

    [Fact]
    public void No_roll_is_allowed_after_a_perfect_game()
    {
        var game = TestGames.Repeat(10, 12);

        Assert.Throws<InvalidRollException>(() => game.Roll(0));
    }

    [Fact]
    public void A_rejected_roll_leaves_the_game_unchanged()
    {
        var game = TestGames.With(7);

        Assert.Throws<InvalidRollException>(() => game.Roll(5));
        game.Roll(3);   // the game still accepts a valid roll afterwards

        Assert.Equal(new[] { 7, 3 }, game.Rolls);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(19, false)]
    [InlineData(20, true)]
    public void Game_is_complete_after_ten_open_frames(int gutterRolls, bool expected)
    {
        var game = TestGames.Repeat(0, gutterRolls);

        Assert.Equal(expected, game.IsComplete);
    }
}
