using Komponent.IO;
using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.Headerless;

namespace Kompression.Encoder
{
    public class LzssEncoder : ILempelZivEncoder
    {
        private readonly Lz10HeaderlessEncoder _encoder = new();

        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            _encoder.Configure(matchOptions);
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            var outputStartPos = output.Position;
            output.Position += 0x10;
            _encoder.Encode(input, output, matches);

            using var bw = new BinaryWriterX(output, true);

            var outputPos = output.Position;
            var buffer = "SSZL"u8.ToArray();

            output.Position = outputStartPos;
            bw.Write(buffer);

            output.Position += 8;
            bw.Write((int)input.Length);

            output.Position = outputPos;
        }
    }
}
