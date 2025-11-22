using Kanvas.Contract.Encoding;

namespace Konnect.Contract.DataClasses.Plugin.File.Image;

public class IndexEncodingDefinition
{
    public required IIndexEncoding IndexEncoding { get; init; }
    public required IList<int> PaletteEncodingIndices { get; init; }
}