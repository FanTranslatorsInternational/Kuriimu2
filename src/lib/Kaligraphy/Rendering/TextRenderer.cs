using Kaligraphy.Contract.Rendering;
using Kaligraphy.DataClasses.Rendering;

namespace Kaligraphy.Rendering;

public class TextRenderer : TextRenderer<RenderContext, RenderOptions>
{
    public TextRenderer(RenderOptions options, IGlyphProvider glyphProvider) : base(options, glyphProvider)
    {
    }
}