using Kaligraphy.Contract.DataClasses;
using Kaligraphy.Contract.DataClasses.Generation.Packing;
using SixLabors.ImageSharp;

namespace Kaligraphy.Generation.Packing
{
    public class FontBinPacker : BinPacker<GlyphData, PackedGlyphData>
    {
        public FontBinPacker(Size canvasSize, int margin) : base(canvasSize, new Size(margin))
        {
        }

        protected override int CalculateVolume(GlyphData element)
        {
            return (element.Description.Size.Width + Margin.Width) *
                   (element.Description.Size.Height + Margin.Height);
        }

        protected override Size CalculateSize(GlyphData element)
        {
            return new Size(element.Description.Size.Width + Margin.Width,
                element.Description.Size.Height + Margin.Height);
        }

        protected override PackedGlyphData CreatePackedElement(GlyphData element, Point position)
        {
            return new PackedGlyphData
            {
                Element = element,
                Position = position + Margin
            };
        }
    }
}
