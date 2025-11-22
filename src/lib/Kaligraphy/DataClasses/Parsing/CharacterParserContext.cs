using System.Text;

namespace Kaligraphy.DataClasses.Parsing;

public class CharacterParserContext
{
    public byte[]? Data { get; init; }

    public Decoder? EncodingDecoder { get; init; }
}