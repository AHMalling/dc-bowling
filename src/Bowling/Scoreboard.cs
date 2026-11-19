namespace Bowling;

/// <summary>
/// One frame on the scoreboard. <see cref="Score"/> and <see cref="RunningTotal"/> are null until the frame is
/// settled: it is finished and any bonus rolls it is waiting for have been rolled.
/// </summary>
public sealed record FrameScore(int Number, IReadOnlyList<int> Pins, int? Score, int? RunningTotal)
{
    public bool IsSettled => Score is not null;
}

/// <summary>
/// A snapshot of a game, computed from its rolls. Only frames that have been started are listed.
/// </summary>
public sealed record Scoreboard(IReadOnlyList<FrameScore> Frames, bool IsComplete)
{
    /// <summary>The running total of the last settled frame (the final score once the game is complete).</summary>
    public int Total => Frames.LastOrDefault(f => f.IsSettled)?.RunningTotal ?? 0;
}
