namespace Kontract.Kanvas.Interfaces.Configuration
{
    public interface ITranscodeConfiguration
    {
        IImageConfiguration With(IColorEncoding encoding);

        IIndexConfiguration With(IIndexEncoding encoding);
    }
}
