using Kaligraphy.Contract.Rendering;
using Kaligraphy.DataClasses.Layout;

namespace Kaligraphy.Layout;

public class TextLayouter(LayoutOptions options, IGlyphProvider glyphProvider)
    : TextLayouter<LayoutContext, LayoutOptions>(options, glyphProvider);