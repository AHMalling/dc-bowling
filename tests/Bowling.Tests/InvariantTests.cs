using Xunit.Sdk;

namespace Bowling.Tests;

/// <summary>
/// Plays many random but valid games and checks rules that must hold for every game and after every roll.
/// The seed is fixed, so a failure is reproducible, and the failing game's rolls are printed.
/// </summary>
public class InvariantTests
{
    private const int Seed = 20261119;
    private const int GameCount = 2_000;

    /// <summary>Share of rolls that knock down every standing pin, so strikes and spares are common.</summary>
    private const double ClearRackProbability = 0.3;

    [Fact]
    public void Random_valid_games_keep_the_scoring_invariants()
    {
        var random = new Random(Seed);

        for (var i = 0; i < GameCount; i++)
        {
            var game = new Game();
            var previousTotal = 0;

            while (!game.IsComplete)
            {
                RollRandomValidRoll(game, random);

                var scoreboard = game.Scoreboard;
                Check(game, () => AssertHoldsAfterEveryRoll(game, scoreboard, previousTotal));
                previousTotal = scoreboard.Total;
            }

            Check(game, () => AssertHoldsForCompleteGame(game));
        }
    }

    /// <summary>
    /// Makes one random valid roll. The test doesn't work out how many pins are standing; it lets <see cref="Game"/>
    /// decide by trying rolls and trying again when one is rejected.
    /// </summary>
    private static void RollRandomValidRoll(Game game, Random random)
    {
        if (random.NextDouble() < ClearRackProbability)
        {
            // Knock down every standing pin: the highest roll Game accepts, found by counting down from 10.
            for (var pins = Rules.PinsPerRack; !TryRoll(game, pins); pins--)
            {
            }
        }
        else
        {
            // Any number 0-10, drawn again until Game accepts it, which is uniform over the valid rolls.
            while (!TryRoll(game, random.Next(Rules.PinsPerRack + 1)))
            {
            }
        }
    }

    private static bool TryRoll(Game game, int pins)
    {
        try
        {
            game.Roll(pins);
            return true;
        }
        catch (InvalidRollException)
        {
            return false;
        }
    }

    private static void AssertHoldsAfterEveryRoll(Game game, Scoreboard scoreboard, int previousTotal)
    {
        // Splitting into frames neither loses nor invents a roll.
        Assert.Equal(game.Rolls, scoreboard.Frames.SelectMany(f => f.Pins));

        // Settled frames come first: a frame never settles before the frame in front of it.
        // The rest show no score and no running total yet.
        var settledCount = scoreboard.Frames.TakeWhile(f => f.IsSettled).Count();
        Assert.All(scoreboard.Frames.Skip(settledCount), f =>
        {
            Assert.False(f.IsSettled);
            Assert.Null(f.RunningTotal);
        });

        // Each running total is the sum of the scores so far, and a frame scores at most 30.
        var sum = 0;
        foreach (var frame in scoreboard.Frames.Take(settledCount))
        {
            Assert.InRange(frame.Score!.Value, 0, 30);
            sum += frame.Score.Value;
            Assert.Equal(sum, frame.RunningTotal);
        }

        Assert.InRange(scoreboard.Total, previousTotal, 300);   // never goes down, never above a perfect game
        Assert.Equal(game.IsComplete, scoreboard.IsComplete);
    }

    private static void AssertHoldsForCompleteGame(Game game)
    {
        var scoreboard = game.Scoreboard;

        Assert.Equal(Rules.FramesPerGame, scoreboard.Frames.Count);
        Assert.All(scoreboard.Frames, f => Assert.True(f.IsSettled));
        // Fewest rolls: nine strikes and an open 10th frame (11). Most: no strikes and a spare in the 10th (21).
        Assert.InRange(game.Rolls.Count, 11, 21);
        Assert.Throws<InvalidRollException>(() => game.Roll(0));
    }

    /// <summary>Runs the assertions and, if one fails, adds the game's rolls so the case can be reproduced.</summary>
    private static void Check(Game game, Action assertions)
    {
        try
        {
            assertions();
        }
        catch (XunitException e)
        {
            throw new XunitException($"Invariant broken for rolls [{string.Join(", ", game.Rolls)}]:\n{e.Message}");
        }
    }
}
