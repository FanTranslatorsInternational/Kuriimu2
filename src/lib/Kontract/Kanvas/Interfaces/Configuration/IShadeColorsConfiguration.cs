namespace Kontract.Kanvas.Interfaces.Configuration
{
    public delegate IColorShader CreateShadedColor();

    public interface IShadeColorsConfiguration
    {
        IImageConfiguration With(CreateShadedColor func);
    }
}
