using System.Text;

namespace Kaligraphy.DataClasses.Parsing
{
    public class ParseContext
    {
        public required byte[] Data { get; init; }

        public required Decoder EncodingDecoder { get; init; }
    }
}
