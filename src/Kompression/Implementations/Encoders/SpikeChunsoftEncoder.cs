using System.IO;
using Kompression.Extensions;
using Kompression.Implementations.Encoders.Headerless;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Encoders
{
    public class SpikeChunsoftEncoder : IEncoder
    {
        private readonly SpikeChunsoftHeaderlessEncoder _encoder;

        public SpikeChunsoftEncoder(IMatchParser parser)
        {
            _encoder = new SpikeChunsoftHeaderlessEncoder(parser);
        }

        public void Encode(Stream input, Stream output)
        {
            _encoder.Encode(input, output);

            WriteHeaderData(output, input.Length);
        }

        private void WriteHeaderData(Stream output, long uncompressedLength)
        {
            var endPosition = output.Position;
            output.Position = 0;

            var magic = new byte[] { 0xFC, 0xAA, 0x55, 0xA7 };
            Write(output, magic);
            Write(output, ((int)uncompressedLength).GetArrayLittleEndian());
            Write(output, ((int)output.Length).GetArrayLittleEndian());

            output.Position = endPosition;
        }

        private void Write(Stream output, byte[] data)
        {
#if NET_CORE_31
            output.Write(data);
#else
            output.Write(data, 0, data.Length);
#endif
        }

        public void Dispose()
        {
        }
    }
}
