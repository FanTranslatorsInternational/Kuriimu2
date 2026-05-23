using RectangleBinPacking;
using SixLabors.ImageSharp;

namespace Kaligraphy.Generation.Packing;

public sealed class SkylineGlyphBinPacker(Size canvasSize, int margin,
    SkylineBinPack.LevelChoiceHeuristic heuristic = SkylineBinPack.LevelChoiceHeuristic.LevelBottomLeft, bool useWasteMap = true)
    : GlyphBinPacker(margin)
{
    private SkylineBinPack? _packer;

    public override Size CanvasSize { get; } = canvasSize;

    protected override void Reset()
    {
        _packer = new SkylineBinPack(CanvasSize.Width, CanvasSize.Height, useWasteMap);
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