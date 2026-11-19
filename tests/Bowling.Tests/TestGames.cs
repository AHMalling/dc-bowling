namespace Bowling.Tests;

/// <summary>Helpers for building games from a list of rolls in tests.</summary>
internal static class TestGames
{
    public static Game With(params int[] rolls)
    {
        var game = new Game();
        foreach (var pins in rolls)
        {
            game.Roll(pins);
        }
        return game;
    }

    /// <summary>Nine gutter frames, so the given rolls land in the 10th frame.</summary>
    public static Game NineOpenFramesThen(params int[] tenthFrame) =>
        With([.. Enumerable.Repeat(0, 18), .. tenthFrame]);

    public static Game Repeat(int pins, int times) => With(Enumerable.Repeat(pins, times).ToArray());

    /// <summary>The example game from the brief's "Example score" table.</summary>
    public static readonly int[] BriefExample = [1, 4, 4, 5, 6, 4, 5, 5, 10, 0, 1, 7, 3, 6, 4, 10, 2, 8, 6];
}
