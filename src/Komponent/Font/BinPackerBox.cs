using System;
using System.Collections.Generic;
using System.Text;

namespace Komponent.Font
{
    class BinPackerBox
    {
        public WhiteSpaceAdjustment adjustedGlyph;
        public double volume;
        public BinPackerNode position;

        public BinPackerBox(WhiteSpaceAdjustment adjustedGlyph1)
        {
            adjustedGlyph = adjustedGlyph1;
            volume = adjustedGlyph1.GlyphSize.Height * adjustedGlyph.GlyphSize.Width;
        }
    }
}
