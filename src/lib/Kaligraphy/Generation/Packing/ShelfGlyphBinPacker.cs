using RectangleBinPacking;
using SixLabors.ImageSharp;

namespace Kaligraphy.Generation.Packing;

public sealed class ShelfGlyphBinPacker(Size canvasSize, int margin,
    ShelfBinPack.ShelfChoiceHeuristic heuristic = ShelfBinPack.ShelfChoiceHeuristic.ShelfFirstFit, bool useWasteMap = true)
    : GlyphBinPacker(margin)
{
    private ShelfBinPack? _packer;

    public override Size CanvasSize { get; } = canvasSize;

    protected override void Reset()
    {
        _packer = new ShelfBinPack(CanvasSize.Width, CanvasSize.Height, useWasteMap);
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