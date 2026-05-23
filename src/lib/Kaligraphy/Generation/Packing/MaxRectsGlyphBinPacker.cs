using RectangleBinPacking;
using SixLabors.ImageSharp;

namespace Kaligraphy.Generation.Packing;

public sealed class MaxRectsGlyphBinPacker(Size canvasSize, int margin,
    FreeRectChoiceHeuristic heuristic = FreeRectChoiceHeuristic.RectBestShortSideFit, bool allowRotations = false)
    : GlyphBinPacker(margin)
{
    private MaxRectsBinPack? _packer;

    public override Size CanvasSize { get; } = canvasSize;

    protected override void Reset()
    {
        _packer = new MaxRectsBinPack(CanvasSize.Width, CanvasSize.Height, allowRotations);
    }

    protected override bool TryInsert(Size size, out Point position)
    {
        Rect rect = _packer!.Insert(size.Width, size.Height, heuristic);
        if (rect.Height == 0)
        {
            position = Point.Empty;
            return false;
        }

        position = new Point(rect.X, rect.Y);
        return true;
    }
}