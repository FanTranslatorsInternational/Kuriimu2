using Kanvas.Contract.Encoding;
using Kanvas.Contract;
using Konnect.Contract.DataClasses.Plugin.File.Image;

namespace Konnect.Contract.Plugin.File.Image;

public interface IEncodingDefinition
{
    IReadOnlyDictionary<int, IColorEncoding> ColorEncodings { get; }
    IReadOnlyDictionary<int, IndexEncodingDefinition> IndexEncodings { get; }
    IReadOnlyDictionary<int, IColorEncoding> PaletteEncodings { get; }

    bool ContainsColorEncoding(int imageFormat);
    bool ContainsPaletteEncoding(int paletteFormat);
    bool ContainsIndexEncoding(int indexFormat);
    bool ContainsColorShader(int imageFormat);
    bool ContainsPaletteShader(int paletteFormat);

    IColorEncoding? GetColorEncoding(int imageFormat);
    IColorEncoding? GetPaletteEncoding(int paletteFormat);
    IndexEncodingDefinition? GetIndexEncoding(int indexFormat);
    IColorShader? GetColorShader(int imageFormat);
    IColorShader? GetPaletteShader(int paletteFormat);
}