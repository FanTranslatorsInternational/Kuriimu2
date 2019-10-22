using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Komponent.Font
{
    class BinPacker
    {
        private Size _canvasSize;
        private BinPackerBox[] _boxes;
        private BinPackerNode _rootNode;

        public BinPacker(Size canvasSize)
        {
            _canvasSize = canvasSize;
        }

        public BinPackerBox[] Pack(List<WhiteSpaceAdjustment> adjustedGlyphs)
        {
            _boxes = adjustedGlyphs.Select(x => new BinPackerBox(x))
                .OrderByDescending(x => x.volume)
                .ToArray();

            _rootNode = new BinPackerNode { width = _canvasSize.Width, height = _canvasSize.Height };

            PackInternal();
            return _boxes.Where(x => x.position != null).ToArray();
        }

        private void PackInternal()
        {
            foreach (var box in _boxes)
            {
                var node = FindNode(_rootNode, box.adjustedGlyph.GlyphSize.Width, box.adjustedGlyph.GlyphSize.Height);
                if (node != null)
                {
                    // Split rectangles
                    box.position = SplitNode(node, box.adjustedGlyph.GlyphSize.Width, box.adjustedGlyph.GlyphSize.Height);
                }
            }
        }

        private BinPackerNode FindNode(BinPackerNode rootNode, double boxWidth, double boxLength)
        {
            if (rootNode.isOccupied)
            {
                var nextNode = FindNode(rootNode.bottomNode, boxWidth, boxLength) ??
                               FindNode(rootNode.rightNode, boxWidth, boxLength);

                return nextNode;
            }

            if (boxWidth <= rootNode.width && boxLength <= rootNode.height)
                return rootNode;

            return null;
        }

        private BinPackerNode SplitNode(BinPackerNode node, double boxWidth, double boxLength)
        {
            node.isOccupied = true;
            node.bottomNode = new BinPackerNode { posZ = node.posZ, posX = node.posX + boxWidth, height = node.height, width = node.width - boxWidth };
            node.rightNode = new BinPackerNode { posZ = node.posZ + boxLength, posX = node.posX, height = node.height - boxLength, width = boxWidth };

            return node;
        }
    }
}
