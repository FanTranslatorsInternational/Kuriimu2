using System.IO;
using Kompression.Extensions;
using Kompression.Implementations.Encoders.Headerless;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Encoders
{
    public class TalesOf01Encoder : IEncoder
    {
        private Lzss01HeaderlessEncoder _encoder;

        public TalesOf01Encoder(IMatchParser parser)
        {
            _encoder = new Lzss01HeaderlessEncoder(parser);
        }

        public void Encode(Stream input, Stream output)
        {
            output.Position += 9;

            _encoder.Encode(input, output);

            WriteHeaderData(output, (int)input.Length);
        }

        private void WriteHeaderData(Stream output, int decompressedLength)
        {
            var endPosition = output.Position;
            output.Position = 0;

            output.WriteByte(1);
            Write(output, ((int)output.Length).GetArrayLittleEndian());
            Write(output, decompressedLength.GetArrayLittleEndian());

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
