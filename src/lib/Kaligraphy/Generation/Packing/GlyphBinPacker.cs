using Kaligraphy.Contract.DataClasses;
using Kaligraphy.Contract.DataClasses.Generation.Packing;
using SixLabors.ImageSharp;

namespace Kaligraphy.Generation.Packing;

public abstract class GlyphBinPacker(int margin)
    : BinPacker<GlyphData, PackedGlyphData>
{
    protected override int CalculateVolume(GlyphData element)
    {
        if (element.Description.Size == Size.Empty)
            return 0;

        return (element.Description.Size.Width + margin) *
               (element.Description.Size.Height + margin);
    }

    protected override Size CalculateSize(GlyphData element)
    {
        if (element.Description.Size == Size.Empty)
            return Size.Empty;

        return new Size(
            element.Description.Size.Width + margin,
            element.Description.Size.Height + margin);
    }

    protected override PackedGlyphData CreatePackedElement(GlyphData element, Point position)
    {
        return new PackedGlyphData
        {
            Element = element,
            Position = element.Description.Size == Size.Empty
                ? Point.Empty
                : position + new Size(margin)
        };
    }
}