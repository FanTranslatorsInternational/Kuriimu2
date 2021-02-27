namespace Kontract.Kanvas.Configuration
{
    public interface ITranscodePaletteConfiguration
    {
        IIndexConfiguration With(IColorEncoding encoding);
    }
}
