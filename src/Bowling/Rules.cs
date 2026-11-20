namespace Bowling;

/// <summary>
/// The fixed numbers of the game, named so the two 10s can't be mixed up.
/// The rules for strikes, spares and when a frame ends live in <see cref="FrameRolls"/>.
/// </summary>
public static class Rules
{
    public const int PinsPerRack = 10;
    public const int FramesPerGame = 10;
}
