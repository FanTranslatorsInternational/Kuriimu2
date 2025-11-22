using Kaligraphy.Contract.DataClasses.Layout;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace Kaligraphy.Contract.Rendering;

public interface ITextRenderer
{
    void Render(Image<Rgba32> image, TextLayoutData layout);
}