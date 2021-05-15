using System.Drawing;

namespace Kontract.Kanvas.Configuration
{
    public delegate IColorShader CreateShadedColor();

    public interface IShadeColorsConfiguration
    {
        IImageConfiguration With(CreateShadedColor func);
    }
}
