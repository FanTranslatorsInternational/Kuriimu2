using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.DataClasses.Quantization.Ditherer
{
    internal class ErrorDiffusionElement(
        IList<Rgba32> colors,
        int colorIndex,
        IDictionary<int, ColorComponentError> errors,
        IList<int> indices)
    {
        public Rgba32 Color => colors[colorIndex];

        public IDictionary<int, ColorComponentError> Errors { get; } = errors;

        public IList<int> Indices { get; } = indices;
    }
}
