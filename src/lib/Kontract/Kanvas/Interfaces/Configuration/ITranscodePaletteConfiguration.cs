namespace Kontract.Kanvas.Interfaces.Configuration
{
    public interface ITranscodePaletteConfiguration
    {
        IIndexConfiguration With(IColorEncoding encoding);
    }
}
