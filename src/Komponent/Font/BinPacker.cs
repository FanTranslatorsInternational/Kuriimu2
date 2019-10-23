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
        /// <param name="adjustedGlyphs">The enumeration of glyphs.</param>
        /// <returns>Position information to a glyph.</returns>
        public IEnumerable<(AdjustedGlyph adjustedGlyph, Point position)> Pack(IEnumerable<AdjustedGlyph> adjustedGlyphs)
        {
            var orderedGlyphInformation = adjustedGlyphs
                .OrderByDescending(CalculateVolume)
                .Select(x => new { AdjustedGlyph = x, Size = x.WhiteSpaceAdjustment?.GlyphSize ?? x.Glyph.Size });

            _rootNode = new BinPackerNode(_canvasSize.Width, _canvasSize.Height);
            foreach (var glyphInformation in orderedGlyphInformation)
            {
                var foundNode = FindNode(_rootNode, glyphInformation.Size);
                if (foundNode != null)
                {
                    SplitNode(foundNode, glyphInformation.Size);
                    yield return (glyphInformation.AdjustedGlyph, foundNode.Position);
                }
            }
        }

        /// <summary>
        /// Calculate volume of an adjusted glyph.
        /// </summary>
        /// <param name="adjustedGlyph">The glyph to calculate the volume from.</param>
        /// <returns>The calculated volume.</returns>
        private int CalculateVolume(AdjustedGlyph adjustedGlyph)
        {
            if (adjustedGlyph.WhiteSpaceAdjustment == null)
                return adjustedGlyph.Glyph.Width * adjustedGlyph.Glyph.Height;
            else
                return adjustedGlyph.WhiteSpaceAdjustment.GlyphSize.Width *
                       adjustedGlyph.WhiteSpaceAdjustment.GlyphSize.Height;
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
