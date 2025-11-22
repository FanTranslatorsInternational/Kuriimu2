using SixLabors.ImageSharp;

namespace Kaligraphy.DataClasses.Generation.Packing;

/// <summary>
/// A node representing part of the canvas in packing.
/// </summary>
internal class BinPackerNode
{
    /// <summary>
    /// The position this node is set on the canvas.
    /// </summary>
    public required Point Position { get; init; }

    /// <summary>
    /// The size this node represents on the canvas.
    /// </summary>
    public required Size Size { get; init; }

    /// <summary>
    /// Is this node is already occupied by a box.
    /// </summary>
    public bool IsOccupied { get; set; }

    /// <summary>
    /// The right headed node.
    /// </summary>
    public BinPackerNode? RightNode { get; set; }

    /// <summary>
    /// The bottom headed node.
    /// </summary>
    public BinPackerNode? BottomNode { get; set; }
}