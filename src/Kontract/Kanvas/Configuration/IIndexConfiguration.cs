namespace Kontract.Kanvas.Configuration
{
    public delegate IColorEncoding CreatePaletteEncoding();

    public interface IIndexConfiguration : IImageConfiguration
    {
        IIndexConfiguration TranscodePaletteWith(CreatePaletteEncoding func);
    }
}
