using Kaligraphy.Contract.DataClasses.Generation.Packing;
using Kaligraphy.Contract.Generation.Packing;
using SixLabors.ImageSharp;

namespace Kaligraphy.Generation.Packing;

public abstract class BinPacker<TElement, TPacked> : IBinPacker<TElement, TPacked>
    where TPacked : PackedElement<TElement>
{
    public abstract Size CanvasSize { get; }

    /// <summary>
    /// Pack an enumeration of white space adjusted glyphs into the given canvas.
    /// </summary>
    /// <param name="elements">The enumeration of glyphs.</param>
    /// <returns>Position information to a glyph.</returns>
    public IEnumerable<TPacked> Pack(IEnumerable<TElement> elements)
    {
        Reset();

        foreach (TElement element in elements.OrderByDescending(CalculateVolume))
        {
            Size elementSize = CalculateSize(element);

            if (elementSize == Size.Empty)
            {
                yield return CreatePackedElement(element, Point.Empty);
                continue;
            }

            if (!TryInsert(elementSize, out Point position))
                continue;

            yield return CreatePackedElement(element, position);
        }
    }

    /// <summary>
    /// Resets the instance to receive a new set of elements.
    /// </summary>
    protected abstract void Reset();

    /// <summary>
    /// Tries to insert a given element with <param name="size" />.
    /// </summary>
    /// <param name="size">The size of the element.</param>
    /// <param name="position">The position at which the element was inserted.</param>
    /// <returns>If the element was inserted.</returns>
    protected abstract bool TryInsert(Size size, out Point position);

    /// <summary>
    /// Calculates the volume of an element.
    /// </summary>
    /// <param name="element">The element to calculate the volume from.</param>
    /// <returns>The calculated volume.</returns>
    protected abstract int CalculateVolume(TElement element);

    /// <summary>
    /// Calculates the size of the element.
    /// </summary>
    /// <param name="element">The element to calculate the size from.</param>
    /// <returns>The calculated size.</returns>
    protected abstract Size CalculateSize(TElement element);

    /// <summary>
    /// Creates the packed element.
    /// </summary>
    /// <param name="element">The element to pack.</param>
    /// <param name="position">The position of the element.</param>
    /// <returns>The packed element.</returns>
    protected abstract TPacked CreatePackedElement(TElement element, Point position);
}