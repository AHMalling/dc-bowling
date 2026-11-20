namespace Bowling;

/// <summary>
/// One player's game of ten-pin bowling.
/// The list of rolls is the only state: frames and scores are derived from it, so they can never drift apart.
/// Every roll is validated before it is stored, so the list always describes a legal (possibly unfinished) game.
/// </summary>
public sealed class Game
{
    private const int MaxRollsPerGame = 21;   // 9 frames x 2 rolls + 3 in the 10th

    private readonly List<int> _rolls = new(MaxRollsPerGame);

    /// <summary>The pins knocked down by each roll, in order: the raw data everything else is computed from.</summary>
    public IReadOnlyList<int> Rolls => _rolls.AsReadOnly();

    public bool IsComplete => FrameForNextRoll() is null;

    /// <summary>Pins standing for the next roll, or 0 when the game is complete.</summary>
    public int PinsStanding => FrameForNextRoll()?.PinsStanding ?? 0;

    /// <summary>
    /// The scoreboard, computed from the rolls each time it is read. At most 21 rolls,
    /// so recomputing is cheaper and simpler than keeping a cached copy in sync.
    /// </summary>
    public Scoreboard Scoreboard => Scorer.Score(_rolls);

    /// <summary>Records a roll.</summary>
    /// <exception cref="InvalidRollException">
    /// The number of pins is outside 0-10, more than the pins standing, or the game is already complete.
    /// </exception>
    public void Roll(int pins)
    {
        if (pins is < 0 or > Rules.PinsPerRack)
        {
            throw new InvalidRollException(
                $"A roll must knock down between 0 and {Rules.PinsPerRack} pins, but was {pins}.");
        }

        var frame = FrameForNextRoll()
            ?? throw new InvalidRollException("The game is complete, so no more rolls are allowed.");

        if (pins > frame.PinsStanding)
        {
            throw new InvalidRollException(
                $"Only {frame.PinsStanding} pins are standing in frame {frame.Number}, so {pins} cannot be knocked down.");
        }

        _rolls.Add(pins);
    }

    /// <summary>The frame the next roll belongs to, or null when the game is complete.</summary>
    private FrameRolls? FrameForNextRoll()
    {
        var frames = FrameSplitter.Split(_rolls);
        if (frames.Count == 0)
        {
            return new FrameRolls(1, 0, []);
        }

        var last = frames[^1];
        if (!last.IsFinished)
        {
            return last;
        }

        return last.IsLast ? null : new FrameRolls(last.Number + 1, _rolls.Count, []);
    }
}
