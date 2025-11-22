using SixLabors.ImageSharp;

namespace Kaligraphy.Contract.DataClasses.Generation.Packing;

public class PackedElement<TElement>
{
    public required TElement Element { get; init; }
    public required Point Position { get; init; }
}