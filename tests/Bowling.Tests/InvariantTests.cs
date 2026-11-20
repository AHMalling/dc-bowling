using Xunit.Sdk;

namespace Bowling.Tests;

/// <summary>
/// Plays many random valid games and checks them against an independent model of the rules that shares no code
/// with the library: <see cref="RandomGame"/> makes the rolls frame by frame, straight from the brief, and
/// <see cref="ReferenceScore"/> scores them the classic way, with an index walking the rolls.
/// Rules that must hold for any game are also checked after every roll.
/// The seed is fixed, so a failure is reproducible, and the failing game's rolls are printed.
/// </summary>
public class InvariantTests
{
    private const int Seed = 20261119;
    private const int GameCount = 2_000;

    /// <summary>Share of rolls that knock down every standing pin, so strikes and spares are common.</summary>
    private const double ClearRackProbability = 0.3;

    [Fact]
    public void Random_valid_games_match_the_reference_model_and_keep_the_invariants()
    {
        var random = new Random(Seed);

        for (var i = 0; i < GameCount; i++)
        {
            var rolls = RandomGame(random);
            var game = new Game();
            var previousTotal = 0;

            for (var r = 0; r < rolls.Count; r++)
            {
                var isLastRoll = r == rolls.Count - 1;
                var pins = rolls[r];
                Check(game, rolls, () =>
                {
                    game.Roll(pins);   // every roll is valid, so Game must accept it
                    var scoreboard = game.Scoreboard;
                    AssertHoldsAfterEveryRoll(game, scoreboard, previousTotal);
                    Assert.Equal(isLastRoll, scoreboard.IsComplete);   // complete exactly after the last roll
                    previousTotal = scoreboard.Total;
                });
            }

            Check(game, rolls, () =>
            {
                Assert.Equal(ReferenceScore(rolls), game.Scoreboard.Total);
                AssertHoldsForCompleteGame(game);
            });
        }
    }

    /// <summary>The rolls of a random valid game, made frame by frame from the brief's rules.</summary>
    private static List<int> RandomGame(Random random)
    {
        var rolls = new List<int>();

        for (var frame = 1; frame <= 9; frame++)
        {
            var first = RandomRoll(random, 10);
            rolls.Add(first);
            if (first < 10)   // a strike ends the frame
            {
                rolls.Add(RandomRoll(random, 10 - first));
            }
        }

        // 10th frame: a strike or a spare earns a third roll, and a cleared rack is set up again.
        var tenthFirst = RandomRoll(random, 10);
        var tenthSecond = RandomRoll(random, tenthFirst == 10 ? 10 : 10 - tenthFirst);
        rolls.Add(tenthFirst);
        rolls.Add(tenthSecond);
        if (tenthFirst == 10 || tenthFirst + tenthSecond == 10)
        {
            var standing = tenthFirst == 10 && tenthSecond < 10 ? 10 - tenthSecond : 10;
            rolls.Add(RandomRoll(random, standing));
        }

        return rolls;
    }

    private static int RandomRoll(Random random, int pinsStanding) =>
        random.NextDouble() < ClearRackProbability ? pinsStanding : random.Next(pinsStanding + 1);

    /// <summary>
    /// The classic way to score a complete game: walk the rolls with an index, one frame at a time,
    /// and look ahead for the bonus. It shares nothing with <c>Scorer</c>, so the two check each other.
    /// </summary>
    private static int ReferenceScore(IReadOnlyList<int> rolls)
    {
        var total = 0;
        var i = 0;

        for (var frame = 1; frame <= 10; frame++)
        {
            if (rolls[i] == 10)   // strike: 10 plus the next two rolls
            {
                total += 10 + rolls[i + 1] + rolls[i + 2];
                i += 1;
            }
            else if (rolls[i] + rolls[i + 1] == 10)   // spare: 10 plus the next roll
            {
                total += 10 + rolls[i + 2];
                i += 2;
            }
            else
            {
                total += rolls[i] + rolls[i + 1];
                i += 2;
            }
        }

        return total;
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

    /// <summary>
    /// Runs one step and, if it fails (a broken rule, or Game rejecting a valid roll), says where in which game,
    /// so the case can be reproduced.
    /// </summary>
    private static void Check(Game game, IReadOnlyList<int> rolls, Action step)
    {
        try
        {
            step();
        }
        catch (Exception e) when (e is XunitException or InvalidRollException)
        {
            throw new XunitException(
                $"Failed after rolls [{string.Join(", ", game.Rolls)}] of the game [{string.Join(", ", rolls)}]:\n{e.Message}");
        }
    }
}
