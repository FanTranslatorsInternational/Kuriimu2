using Kaligraphy.Contract.DataClasses.Generation.Packing;

namespace Kaligraphy.Contract.Generation.Packing;

public interface IBinPacker<in TElement, out TPacked>
    where TPacked : PackedElement<TElement>
{
    IEnumerable<TPacked> Pack(IEnumerable<TElement> elements);
}