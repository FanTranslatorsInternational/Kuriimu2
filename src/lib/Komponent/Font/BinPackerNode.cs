using System.Drawing;

namespace Komponent.Font
{
    /// <summary>
    /// A node representing part of the canvas in <see cref="BinPacker"/>.
    /// </summary>
    class BinPackerNode
    {
        /// <summary>
        /// The size this node represents on the canvas.
        /// </summary>
        public Size Size { get; }

        /// <summary>
        /// The position this node is set on the canvas.
        /// </summary>
        public Point Position { get; set; }

        /// <summary>
        /// Is this node is already occupied by a box.
        /// </summary>
        public bool IsOccupied { get; set; }

        /// <summary>
        /// The right headed node.
        /// </summary>
        public BinPackerNode RightNode { get; set; }

        /// <summary>
        /// The bottom headed node.
        /// </summary>
        public BinPackerNode BottomNode { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="BinPackerNode"/>.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public BinPackerNode(int width, int height)
        {
            Size = new Size(width, height);
        }
    }
}
