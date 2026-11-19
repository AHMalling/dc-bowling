namespace Bowling;

/// <summary>
/// Turns a list of rolls into a scoreboard. Pure: the same rolls always give the same scoreboard,
/// and nothing is stored, so it is always safe to compute again.
/// Internal because it trusts that the rolls are valid; <see cref="Game"/> guarantees that.
/// </summary>
internal static class Scorer
{
    public static Scoreboard Score(IReadOnlyList<int> rolls)
    {
        var frames = FrameSplitter.Split(rolls);
        var scores = new List<FrameScore>(frames.Count);
        int? runningTotal = 0;

        foreach (var frame in frames)
        {
            var score = ScoreOf(frame, rolls);
            runningTotal = runningTotal + score;   // null as soon as one frame is unsettled
            scores.Add(new FrameScore(frame.Number, frame.Pins, score, runningTotal));
        }

        var isComplete = frames.Count == Rules.FramesPerGame && frames[^1].IsFinished;
        return new Scoreboard(scores, isComplete);
    }

    /// <summary>The frame's score, or null while it is unfinished or still waiting for bonus rolls.</summary>
    private static int? ScoreOf(FrameRolls frame, IReadOnlyList<int> rolls)
    {
        if (!frame.IsFinished)
        {
            return null;
        }

        // The 10th frame's bonus rolls are rolled inside the frame itself, so its score is just its pins.
        if (frame.IsLast)
        {
            return frame.Pins.Sum();
        }

        var bonusRollCount = frame.IsStrike ? 2 : frame.IsSpare ? 1 : 0;
        var firstBonusIndex = frame.FirstRollIndex + frame.Pins.Count;
        if (firstBonusIndex + bonusRollCount > rolls.Count)
        {
            return null;   // the bonus rolls have not been rolled yet
        }

        return frame.Pins.Sum() + rolls.Skip(firstBonusIndex).Take(bonusRollCount).Sum();
    }
}
