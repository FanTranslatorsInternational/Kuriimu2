using System.Buffers.Binary;
using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.LempelZiv.PriceCalculators;

namespace Kompression.Encoder.Headerless
{
    internal class Lz4HeaderlessEncoder : ILempelZivEncoder
    {
        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new Lz4PriceCalculator())
                .FindPatternMatches().WithinLimitations(0x4, 0x100, 1, 0xFFFF);
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            var startPosition = output.Position;

            output.Position += 8;
            foreach (var match in matches)
            {
                // Write token
                var literalLength = (int)(match.Position - input.Position);
                var token = Math.Min(0xF, literalLength) << 4 | Math.Min(0xF, match.Length - 4);
                output.WriteByte((byte)token);

                // Write literal length
                literalLength -= 0xF;
                while (literalLength >= 0xFF)
                {
                    var value = (byte)Math.Min(0xFF, literalLength);
                    output.WriteByte(value);
                    literalLength -= value;
                }
                if (literalLength >= 0)
                    output.WriteByte((byte)literalLength);

                // Write literals
                var buffer = new byte[match.Position - input.Position];
                input.Read(buffer);
                output.Write(buffer);

                // Write match displacement
                output.WriteByte((byte)match.Displacement);
                output.WriteByte((byte)(match.Displacement >> 8));

                // Write match length
                var matchLength = match.Length - 0x13;
                while (matchLength >= 0xFF)
                {
                    var value = (byte)Math.Min(0xFF, matchLength);
                    output.WriteByte(value);
                    matchLength -= value;
                }
                if (matchLength >= 0)
                    output.WriteByte((byte)matchLength);

                input.Position += match.Length;
            }

            // Write block header
            var endPosition = output.Position;
            output.Position = startPosition;

            var buffer1 = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buffer1, (int)~(input.Length - 1));
            output.Write(buffer1);

            BinaryPrimitives.WriteInt32LittleEndian(buffer1, (int)(endPosition - startPosition - 8));
            output.Write(buffer1);

            output.Position = endPosition;
        }
    }
}
