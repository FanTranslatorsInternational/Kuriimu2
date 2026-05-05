using System.Buffers.Binary;
using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.Headerless;

namespace Kompression.Encoder
{
    public class IecpEncoder : ILempelZivEncoder
    {
        private readonly Lzss01HeaderlessEncoder _encoder = new();

        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            _encoder.Configure(matchOptions);
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            output.Position += 8;

            _encoder.Encode(input, output, matches);

            WriteHeaderData(output, (int)input.Length);
        }

        private static void WriteHeaderData(Stream output, int decompressedLength)
        {
            var endPosition = output.Position;
            output.Position = 0;

            var buffer = "IECP"u8.ToArray();
            output.Write(buffer);

            BinaryPrimitives.WriteInt32LittleEndian(buffer, decompressedLength);
            output.Write(buffer);

            output.Position = endPosition;
        }
    }
}
