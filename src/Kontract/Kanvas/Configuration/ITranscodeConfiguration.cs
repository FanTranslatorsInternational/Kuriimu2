namespace Kontract.Kanvas.Configuration
{
    public interface ITranscodeConfiguration
    {
        IImageConfiguration With(IColorEncoding encoding);

        IIndexConfiguration With(IIndexEncoding encoding);
    }
}
