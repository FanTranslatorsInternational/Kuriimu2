using SixLabors.ImageSharp;

namespace Kaligraphy.Contract.DataClasses;

public class BorderSpaceData
{
    /// <summary>
    /// The position where non-border data starts.
    /// </summary>
    public required Point Position { get; init; }

    /// <summary>
    /// The size of the non-border data.
    /// </summary>
    public required Size Size { get; init; }
}