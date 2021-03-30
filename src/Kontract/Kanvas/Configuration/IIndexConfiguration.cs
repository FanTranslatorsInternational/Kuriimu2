namespace Kontract.Kanvas.Configuration
{
    public delegate IColorEncoding CreatePaletteEncoding();

    public interface IIndexConfiguration : IImageConfiguration
    {
        public ITranscodePaletteConfiguration TranscodePalette { get; }
    }
}
