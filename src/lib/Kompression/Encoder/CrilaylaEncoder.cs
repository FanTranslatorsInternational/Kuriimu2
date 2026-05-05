using System.Text;
using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.LempelZiv.PriceCalculators;

namespace Kompression.Encoder
{
    public class CrilaylaEncoder : ILempelZivEncoder
    {
        private const int SkipSize_ = 0x100;

        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new CrilaylaPriceCalculator())
                .FindPatternMatches().WithinLimitations(3, -1, 3, 0x2002)
                .AdjustInput(input => input.Skip(SkipSize_).Reverse());
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            var matchArray = matches.ToArray();
            var compressedLength = CalculateCompressedLength(input.Length, matchArray);

            var outputSize = (compressedLength + 0xF & ~0xF) + 0x10;

            using var inputReverseStream = new ReverseStream(input, input.Length);
            using var outputReverseStream = new ReverseStream(output, outputSize);

            using var bw = new BinaryWriterX(outputReverseStream, true, ByteOrder.LittleEndian, BitOrder.MostSignificantBitFirst, 1);

            foreach (var match in matchArray)
            {
                // Write raw bytes
                while (match.Position < input.Length - inputReverseStream.Position)
                {
                    bw.WriteBit(0);
                    bw.WriteBits(inputReverseStream.ReadByte(), 8);
                }

                // Write match
                bw.WriteBit(1);
                bw.WriteBits(match.Displacement - 3, 13);
                WriteLength(bw, match.Length);

                inputReverseStream.Position += match.Length;
            }

            // Write remaining data
            while (inputReverseStream.Position < input.Length - SkipSize_)
            {
                bw.WriteBit(0);
                bw.WriteBits(inputReverseStream.ReadByte(), 8);
            }

            bw.Flush();

            // Write raw start data
            input.Position = 0;
            var rawStart = new byte[0x100];
            input.Read(rawStart, 0, rawStart.Length);

            output.Position = output.Length;
            output.Write(rawStart, 0, rawStart.Length);

            // Write header
            using var outputBw = new BinaryWriterX(output, true);

            output.Position = 0;
            outputBw.WriteString("CRILAYLA", Encoding.ASCII, false, false);
            outputBw.Write((int)(input.Length - SkipSize_));
            outputBw.Write((int)(output.Length - 0x10 - SkipSize_));
        }

        private static long CalculateCompressedLength(long inputLength, LempelZivMatch[] matches)
        {
            var result = 0;

            var lastMatchPosition = inputLength;

            foreach (var match in matches)
            {
                // Add raw bytes
                if (lastMatchPosition > match.Position)
                {
                    var rawLength = (int)(lastMatchPosition - match.Position);
                    result += rawLength * 9;
                }

                var lengthBits = CrilaylaPriceCalculator.CalculateLengthBits(match.Length);
                var bits = lengthBits + 13 + 1;

                result += bits;
                lastMatchPosition = match.Position - match.Length;
            }

            if (lastMatchPosition > SkipSize_)
            {
                var rawLength = (int)(lastMatchPosition - SkipSize_);
                result += rawLength * 9;
            }

            return result / 8 + (result % 8 > 0 ? 1 : 0);
        }

        private static void WriteLength(BinaryWriterX bw, int matchLength)
        {
            matchLength -= 3;

            var toWrite = Math.Min(matchLength, 3);
            bw.WriteBits(toWrite, 2);

            if (toWrite < 3)
                return;

            matchLength -= toWrite;
            toWrite = Math.Min(matchLength, 7);
            bw.WriteBits(toWrite, 3);
            if (toWrite < 7)
                return;

            matchLength -= toWrite;
            toWrite = Math.Min(matchLength, 31);
            bw.WriteBits(toWrite, 5);
            if (toWrite < 31)
                return;

            matchLength -= 31;
            do
            {
                toWrite = Math.Min(matchLength, 255);
                bw.WriteBits(toWrite, 8);

                matchLength -= toWrite;

                if (matchLength == 0 && toWrite == 255)
                    bw.WriteBits(0, 8);
            } while (matchLength > 0);
        }
    }
}
