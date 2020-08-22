using System.IO;
using Kompression.Implementations.Encoders.Headerless;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Encoders
{
    public class LzssEncoder : IEncoder
    {
        private Lz10HeaderlessEncoder _encoder;

        public LzssEncoder(IMatchParser matchParser)
        {
            _encoder = new Lz10HeaderlessEncoder(matchParser);
        }

        public void Encode(Stream input, Stream output)
        {
            var outputStartPos = output.Position;
            output.Position += 0x10;
            _encoder.Encode(input, output);

            var outputPos = output.Position;
            output.Position = outputStartPos;
            output.Write(new byte[] { 0x53, 0x53, 0x5A, 0x4C }, 0, 4);
            output.Position += 8;
            var decompressedSizeBuffer = new[]
            {
                (byte)input.Length,
                (byte)((input.Length>>8)&0xFF),
                (byte)((input.Length>>16)&0xFF),
                (byte)((input.Length>>24)&0xFF),
            };
            output.Write(decompressedSizeBuffer, 0, 4);

            output.Position = outputPos;
        }

        public void Dispose()
        {
            _encoder = null;
        }
    }
}
