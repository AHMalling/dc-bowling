namespace Bowling;

/// <summary>
/// The one place that knows where a frame ends and the next begins.
/// Used both to validate the next roll and (later) to score the game.
/// </summary>
internal static class FrameSplitter
{
    public static IReadOnlyList<FrameRolls> Split(IReadOnlyList<int> rolls)
    {
        var frames = new List<FrameRolls>(Rules.FramesPerGame);
        var index = 0;

        while (index < rolls.Count)
        {
            var number = frames.Count + 1;
            var remaining = rolls.Count - index;
            var length = number == Rules.FramesPerGame ? remaining   // the 10th frame takes the rest (at most 3, Game ensures it)
                : rolls[index] == Rules.PinsPerRack ? 1                // a strike ends the frame
                : Math.Min(2, remaining);                              // two rolls, or one while the frame is in progress

            frames.Add(new FrameRolls(number, index, rolls.Skip(index).Take(length).ToArray()));
            index += length;
        }

        return frames;
    }
}
