using System.Collections.Generic;
using System.Drawing;

namespace Komponent.Font
{
    public class FontTextureGenerator
    {
        private readonly BinPacker _binPacker;

        public Size CanvasSize { get; }

        public FontTextureGenerator(Size canvasSize)
        {
            CanvasSize = canvasSize;
            _binPacker = new BinPacker(canvasSize);
        }

        public Bitmap[] GenerateFontTexture(List<WhiteSpaceAdjustment> adjustedGlyphs, int textureCount = -1)
        {
            var boxes = _binPacker.Pack(adjustedGlyphs);
            // TODO: Process further

            return null;
        }
    }
}
