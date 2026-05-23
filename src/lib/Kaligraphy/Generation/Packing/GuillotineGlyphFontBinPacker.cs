using RectangleBinPacking;
using SixLabors.ImageSharp;

namespace Kaligraphy.Generation.Packing;

public sealed class GuillotineGlyphFontBinPacker(Size canvasSize, int margin, bool mergeFreeRectangles = false,
    GuillotineBinPack.FreeRectChoiceHeuristic choiceHeuristic = GuillotineBinPack.FreeRectChoiceHeuristic.RectBestShortSideFit,
    GuillotineBinPack.GuillotineSplitHeuristic splitHeuristic = GuillotineBinPack.GuillotineSplitHeuristic.SplitMaximizeArea)
    : GlyphBinPacker(margin)
{
    private GuillotineBinPack? _packer;

    public override Size CanvasSize { get; } = canvasSize;

    protected override void Reset()
    {
        _packer = new GuillotineBinPack(CanvasSize.Width, CanvasSize.Height);
    }

    protected override bool TryInsert(Size size, out Point position)
    {
        Rect rect = _packer!.Insert(size.Width, size.Height, mergeFreeRectangles, choiceHeuristic, splitHeuristic);
        if (rect.Height == 0)
        {
            position = Point.Empty;
            return false;
        }

        position = new Point(rect.X, rect.Y);
        return true;
    }
}