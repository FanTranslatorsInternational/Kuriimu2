using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.Headerless;

namespace Kompression.Encoder.Nintendo
{
    public class RleEncoder : ILempelZivEncoder
    {
        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            RleHeaderlessEncoder.Configure(matchOptions);
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            if (input.Length > 0xFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var compressionHeader = new byte[]
            {
                0x30, (byte) (input.Length & 0xFF), (byte) (input.Length >> 8 & 0xFF),
                (byte) (input.Length >> 16 & 0xFF)
            };
            output.Write(compressionHeader, 0, 4);

            RleHeaderlessEncoder.Encode(input, output, matches);
        }
    }
}
