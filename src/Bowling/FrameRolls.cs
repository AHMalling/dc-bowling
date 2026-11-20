namespace Bowling;

/// <summary>
/// The rolls that belong to one frame, and where the frame starts in the game's list of rolls.
/// A frame can be unfinished (the game is in progress).
/// </summary>
internal sealed record FrameRolls(int Number, int FirstRollIndex, IReadOnlyList<int> Pins)
{
    public bool IsLast => Number == Rules.FramesPerGame;

    public bool IsStrike => Pins.Count >= 1 && Pins[0] == Rules.PinsPerRack;

    public bool IsSpare => !IsStrike && Pins.Count >= 2 && Pins[0] + Pins[1] == Rules.PinsPerRack;

    /// <summary>
    /// Frames 1-9 end after a strike or two rolls. The 10th ends after two rolls,
    /// or after three if the first two were a strike or a spare.
    /// </summary>
    public bool IsFinished => IsLast
        ? Pins.Count == 3 || (Pins.Count == 2 && !IsStrike && !IsSpare)
        : IsStrike || Pins.Count == 2;

    /// <summary>The 10th frame is finished, so the game is over. The one definition of a complete game.</summary>
    public bool EndsGame => IsLast && IsFinished;

    /// <summary>
    /// Pins standing for the next roll in this frame. When all pins are down the rack is reset,
    /// which only matters in the 10th frame (in frames 1-9 the frame is finished at that point).
    /// </summary>
    public int PinsStanding
    {
        get
        {
            var standing = Rules.PinsPerRack;
            foreach (var pins in Pins)
            {
                standing -= pins;
                if (standing == 0)
                {
                    standing = Rules.PinsPerRack;
                }
            }
            return standing;
        }
    }
}
