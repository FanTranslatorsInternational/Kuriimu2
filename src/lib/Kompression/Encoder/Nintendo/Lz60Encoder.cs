using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;

namespace Kompression.Encoder.Nintendo
{
    public class Lz60Encoder : ILempelZivEncoder
    {
        private readonly Lz40Encoder _lz40Encoder = new();

        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            _lz40Encoder.Configure(matchOptions);
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            if (input.Length > 0xFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var compressionHeader = new byte[] { 0x60, (byte)(input.Length & 0xFF), (byte)(input.Length >> 8 & 0xFF), (byte)(input.Length >> 16 & 0xFF) };
            output.Write(compressionHeader, 0, 4);

            Lz40Encoder.WriteCompressedData(input, output, [.. matches]);
        }
    }
}
