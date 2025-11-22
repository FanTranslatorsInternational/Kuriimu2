using Kaligraphy.Contract.DataClasses.Generation.Packing;
using Kaligraphy.Contract.Generation.Packing;
using Kaligraphy.DataClasses.Generation.Packing;
using SixLabors.ImageSharp;

namespace Kaligraphy.Generation.Packing;

public abstract class BinPacker<TElement, TPacked> : IBinPacker<TElement, TPacked>
    where TPacked : PackedElement<TElement>
{
    /// <summary>
    /// Gets the total size of the canvas.
    /// </summary>
    protected Size CanvasSize { get; }

    /// <summary>
    /// The margin between all elements.
    /// </summary>
    protected Size Margin { get; }

    /// <summary>
    /// Creates a new instance of <see cref="BinPacker{TElement,TPacked}"/>"/>.
    /// </summary>
    /// <param name="canvasSize">The total size of the canvas.</param>
    /// <param name="margin">The margin between all elements.</param>
    protected BinPacker(Size canvasSize, Size margin)
    {
        CanvasSize = canvasSize;
        Margin = new Size(margin);
    }

    /// <summary>
    /// Pack an enumeration of white space adjusted glyphs into the given canvas.
    /// </summary>
    /// <param name="elements">The enumeration of glyphs.</param>
    /// <returns>Position information to a glyph.</returns>
    public IEnumerable<TPacked> Pack(IEnumerable<TElement> elements)
    {
        var rootNode = new BinPackerNode
        {
            Position = Point.Empty,
            Size = CanvasSize - Margin
        };

        foreach (TElement element in elements.OrderByDescending(CalculateVolume))
        {
            Size elementSize = CalculateSize(element);

            if (elementSize == Size.Empty)
            {
                yield return CreatePackedElement(element, Point.Empty);
                continue;
            }

            BinPackerNode? foundNode = FindNode(rootNode, elementSize);
            if (foundNode == null)
                continue;

            SplitNode(foundNode, elementSize);
            yield return CreatePackedElement(element, foundNode.Position);
        }
    }

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

    /// <summary>
    /// Find a node to fit the box in.
    /// </summary>
    /// <param name="node">The current node to search through.</param>
    /// <param name="boxSize">The size of the box.</param>
    /// <returns>The found node.</returns>
    private BinPackerNode? FindNode(BinPackerNode node, Size boxSize)
    {
        if (node.IsOccupied)
        {
            BinPackerNode? nextNode = null;
            if (node.BottomNode is not null)
            {
                nextNode = FindNode(node.BottomNode, boxSize);
                if (nextNode is null && node.RightNode is not null)
                    nextNode = FindNode(node.RightNode, boxSize);
            }
            else
            {
                if (node.RightNode is not null)
                    nextNode = FindNode(node.RightNode, boxSize);
            }

            return nextNode;
        }

        if (boxSize.Width <= node.Size.Width && boxSize.Height <= node.Size.Height)
            return node;

        return null;
    }

    /// <summary>
    /// Splits a node to fit the box.
    /// </summary>
    /// <param name="node">The node to split.</param>
    /// <param name="boxSize">The size of the box.</param>
    private void SplitNode(BinPackerNode node, Size boxSize)
    {
        node.IsOccupied = true;

        node.RightNode = new BinPackerNode
        {
            Position = new Point(node.Position.X + boxSize.Width, node.Position.Y),
            Size = new Size(node.Size.Width - boxSize.Width, node.Size.Height)
        };
        node.BottomNode = new BinPackerNode
        {
            Position = new Point(node.Position.X, node.Position.Y + boxSize.Height),
            Size = new Size(boxSize.Width, node.Size.Height - boxSize.Height)
        };
    }
}