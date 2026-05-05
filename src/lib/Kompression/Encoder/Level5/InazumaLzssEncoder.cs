using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.Headerless;

namespace Kompression.Encoder.Level5
{
    public class InazumaLzssEncoder : ILempelZivEncoder
    {
        private readonly Lzss01HeaderlessEncoder _encoder = new();

        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            _encoder.Configure(matchOptions);
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            output.Position = 0x10;
            _encoder.Encode(input, output, matches);

            output.Position = 0;
            output.Write("SSZL"u8.ToArray(), 0, 4);  // SSZL

            output.Position += 4;
            var compressedBuffer = new[]
            {
                (byte)output.Length,
                (byte)(output.Length>>8&0xFF),
                (byte)(output.Length>>16&0xFF),
                (byte)(output.Length>>24&0xFF),
            };
            output.Write(compressedBuffer, 0, 4);

            var decompressedSizeBuffer = new[]
            {
                (byte)input.Length,
                (byte)(input.Length>>8&0xFF),
                (byte)(input.Length>>16&0xFF),
                (byte)(input.Length>>24&0xFF),
            };
            output.Write(decompressedSizeBuffer, 0, 4);

            output.Position = output.Length;
        }
    }
}
