using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Komponent.Font
{
    /// <summary>
    /// Packs a given set of glyphs into a given canvas.
    /// </summary>
    class BinPacker
    {
        private readonly Size _canvasSize;
        private BinPackerNode _rootNode;

        /// <summary>
        /// Creates a new instance of <see cref="BinPacker"/>.
        /// </summary>
        /// <param name="canvasSize">The total size of the canvas.</param>
        public BinPacker(Size canvasSize)
        {
            _canvasSize = canvasSize;
        }

        /// <summary>
        /// Pack an enumeration of white space adjusted glyphs into the given canvas.
        /// </summary>
        /// <param name="glyphs">The enumeration of glyphs.</param>
        /// <param name="adjustments">Optional white space adjustments for each glyph.</param>
        /// <returns>Position information to a glyph.</returns>
        public IEnumerable<(Bitmap glyph, Point position)> Pack(IEnumerable<Bitmap> glyphs, IEnumerable<WhiteSpaceAdjustment> adjustments = null)
        {
            IEnumerable<(Bitmap glyph, Size size)> orderedGlyphInformation;
            if (adjustments != null)
            {
                // Order all glyphs descending by volume in respect to given white space adjustments
                orderedGlyphInformation = glyphs.Zip(adjustments, (b, a) => new { Glyph = b, Adjustment = a })
                    .OrderByDescending(x => x.Adjustment.GlyphSize.Width * x.Adjustment.GlyphSize.Height)
                    .Select(x => (x.Glyph, x.Adjustment.GlyphSize));
            }
            else
            {
                // Order all glyphs descending by volume
                orderedGlyphInformation = glyphs.OrderByDescending(x => x.Width * x.Height)
                    .Select(x => (x, new Size(x.Width, x.Height)));
            }

            _rootNode = new BinPackerNode(_canvasSize.Width, _canvasSize.Height);
            foreach (var glyphInformation in orderedGlyphInformation)
            {
                var foundNode = FindNode(_rootNode, glyphInformation.size);
                if (foundNode != null)
                {
                    SplitNode(foundNode, glyphInformation.size);
                    yield return (glyphInformation.glyph, foundNode.Position);
                }
            }
        }

        /// <summary>
        /// Find a node to fit the box in.
        /// </summary>
        /// <param name="node">The current node to search through.</param>
        /// <param name="boxSize">The size of the box.</param>
        /// <returns>The found node.</returns>
        private BinPackerNode FindNode(BinPackerNode node, Size boxSize)
        {
            if (node.IsOccupied)
            {
                var nextNode = FindNode(node.BottomNode, boxSize) ??
                               FindNode(node.RightNode, boxSize);

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

            node.BottomNode = new BinPackerNode(node.Size.Width - boxSize.Width, node.Size.Height)
            {
                Position = new Point(node.Position.X + boxSize.Width, node.Position.Y)
            };
            node.RightNode = new BinPackerNode(boxSize.Width, node.Size.Height - boxSize.Height)
            {
                Position = new Point(node.Position.X, node.Position.Y + boxSize.Height)
            };
        }
    }
}
