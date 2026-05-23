using Kaligraphy.Contract.DataClasses.Generation.Packing;
using SixLabors.ImageSharp;

namespace Kaligraphy.Contract.Generation.Packing;

public interface IBinPacker<in TElement, out TPacked>
    where TPacked : PackedElement<TElement>
{
    Size CanvasSize { get; }

    IEnumerable<TPacked> Pack(IEnumerable<TElement> elements);
}