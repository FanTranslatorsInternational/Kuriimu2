using Komponent.IO;
using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.Headerless;

namespace Kompression.Encoder
{
    public class ShadeLzEncoder : ILempelZivEncoder
    {
        private readonly ShadeLzHeaderlessEncoder _encoder = new();

        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            _encoder.Configure(matchOptions);
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            output.Position += 0xC;
            _encoder.Encode(input, output, matches);

            WriteHeaderData(output, input.Length);
        }

        private static void WriteHeaderData(Stream output, long uncompressedLength)
        {
            var bkPos = output.Position;
            output.Position = 0;

            using var bw = new BinaryWriterX(output, true);

            var buffer = new byte[] { 0xFC, 0xAA, 0x55, 0xA7 };
            bw.Write(buffer);
            bw.Write((int)uncompressedLength);
            bw.Write((int)output.Length);

            output.Position = bkPos;
        }
    }
}
