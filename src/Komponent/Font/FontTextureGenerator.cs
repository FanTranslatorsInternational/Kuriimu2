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

        public Bitmap[] GenerateFontTextures(IEnumerable<Bitmap> glyphs, int textureCount) =>
            GenerateFontTextures(glyphs, null, textureCount);

        public Bitmap[] GenerateFontTextures(IEnumerable<Bitmap> glyphs, IEnumerable<WhiteSpaceAdjustment> adjustments, int textureCount = -1)
        {
            var boxes = _binPacker.Pack(glyphs, adjustments);
            // TODO: Process further

            return null;
        }
    }
}
