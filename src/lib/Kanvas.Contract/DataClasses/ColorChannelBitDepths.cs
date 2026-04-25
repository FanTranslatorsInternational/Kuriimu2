namespace Kanvas.Contract.DataClasses;

public readonly struct ColorChannelBitDepths(int red, int green, int blue, int alpha)
{
    public static readonly ColorChannelBitDepths Unknown = new(-1, -1, -1, -1);

    public int Red { get; } = Math.Max(red, 1);
    public int Green { get; } = Math.Max(green, 1);
    public int Blue { get; } = Math.Max(blue, 1);
    public int Alpha { get; } = Math.Max(alpha, 1);
}