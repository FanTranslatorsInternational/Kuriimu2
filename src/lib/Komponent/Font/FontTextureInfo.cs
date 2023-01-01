using System.Collections.Generic;
using System.Drawing;

namespace Komponent.Font
{
    public class FontTextureInfo
    {
        public Bitmap FontTexture { get; }

        public IList<(Bitmap, Point)> Glyphs { get; }

        public FontTextureInfo(Bitmap fontTexture, IList<(Bitmap, Point)> glyphs)
        {
            FontTexture = fontTexture;
            Glyphs = glyphs;
        }
    }
}
