namespace Kontract.Kanvas.Interfaces.Configuration
{
    public delegate IColorEncoding CreatePaletteEncoding();

    public interface IIndexConfiguration : IImageConfiguration
    {
        public ITranscodePaletteConfiguration TranscodePalette { get; }
    }
}
