using Kaligraphy.Contract.Rendering;
using Kaligraphy.DataClasses.Rendering;

namespace Kaligraphy.Rendering;

public class TextRenderer(RenderOptions options, IGlyphProvider glyphProvider)
    : TextRenderer<RenderContext, RenderOptions>(options, glyphProvider);