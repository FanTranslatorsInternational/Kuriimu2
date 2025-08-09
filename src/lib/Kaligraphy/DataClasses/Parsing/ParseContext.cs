using System.Text;

namespace Kaligraphy.DataClasses.Parsing
{
    public class ParseContext
    {
        public required byte[] Data { get; init; }

        public required Encoding Encoding { get; init; }

        public required int MinByteCount { get; init; }

        public required int MaxByteCount { get; init; }
    }
}
