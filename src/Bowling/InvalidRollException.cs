namespace Bowling;

/// <summary>A roll that breaks the rules of bowling. The game is left unchanged.</summary>
public sealed class InvalidRollException(string message) : Exception(message);
