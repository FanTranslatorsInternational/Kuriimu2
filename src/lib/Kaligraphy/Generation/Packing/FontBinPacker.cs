using Kaligraphy.Contract.DataClasses;
using Kaligraphy.Contract.DataClasses.Generation.Packing;
using SixLabors.ImageSharp;

namespace Kaligraphy.Generation.Packing;

public class FontBinPacker(Size canvasSize, int margin)
    : BinPacker<GlyphData, PackedGlyphData>(canvasSize, new Size(margin))
{
    protected override int CalculateVolume(GlyphData element)
    {
        if (element.Description.Size == Size.Empty)
            return 0;

        return (element.Description.Size.Width + Margin.Width) *
               (element.Description.Size.Height + Margin.Height);
    }

    protected override Size CalculateSize(GlyphData element)
    {
        if (element.Description.Size == Size.Empty)
            return Size.Empty;

        return new Size(element.Description.Size.Width + Margin.Width,
            element.Description.Size.Height + Margin.Height);
    }

    protected override PackedGlyphData CreatePackedElement(GlyphData element, Point position)
    {
        return new PackedGlyphData
        {
            Element = element,
            Position = element.Description.Size == Size.Empty 
                ? Point.Empty 
                : position + Margin
        };
    }
}