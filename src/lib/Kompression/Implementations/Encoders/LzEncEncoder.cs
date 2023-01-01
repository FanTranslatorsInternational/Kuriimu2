using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Kompression.Implementations.PriceCalculators;
using Kontract.Kompression.Interfaces.Configuration;
using Kontract.Kompression.Models.PatternMatch;

namespace Kompression.Implementations.Encoders
{
    // TODO: Refactor block class
    public class LzEncEncoder : ILzEncoder
    {
        class Block
        {
            public bool initialRead = true;
            public byte codeByte;
            public long codeBytePosition;
            public long matchEndPosition;
        }

        public void Configure(IInternalMatchOptions matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new LzEncPriceCalculator())
                .FindMatches().WithinLimitations(3, -1, 1, 0xBFFF);
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
        {
            var block = new Block();

            foreach (var match in matches.ToArray())
            {
                if (input.Position < match.Position)
                    WriteRawData(input, output, block, match.Position - input.Position);

                if (block.initialRead)
                    block.initialRead = false;

                WriteMatchData(input, output, block, match);
            }

            if (input.Position < input.Length)
                WriteRawData(input, output, block, input.Length - input.Position);

            // Write ending match flag
            output.WriteByte(0x11);
            output.WriteByte(0);
            output.WriteByte(0);
        }

        private void WriteRawData(Stream input, Stream output, Block block, long length)
        {
            if (block.initialRead)
            {
                // Apply special rules for first raw data read
                if (length <= 0xee)
                {
                    output.WriteByte((byte)(length + 0x11));
                }
                else
                {
                    output.WriteByte(0);
                    Write(output, EncodeLength(length - 3, 4));
                }
            }
            else
            {
                if (length <= 3)
                {
                    block.codeByte |= (byte)length;

                    output.Position = block.codeBytePosition;
                    output.WriteByte(block.codeByte);

                    output.Position = block.matchEndPosition;
                }
                else
                {
                    if (length <= 0x12)
                    {
                        output.WriteByte((byte)(length - 3));
                    }
                    else
                    {
                        output.WriteByte(0);
                        Write(output, EncodeLength(length - 3, 4));
                    }
                }
            }

            for (var i = 0; i < length; i++)
                output.WriteByte((byte)input.ReadByte());
        }

        private void WriteMatchData(Stream input, Stream output, Block block, Match match)
        {
            if (match.Displacement <= 0x4000)
            {
                // Write encoded matchLength
                var localCode = (byte)0x20;
                var length = match.Length - 2;
                if (length <= 0x1F)
                    localCode |= (byte)length;

                output.WriteByte(localCode);
                if (length > 0x1F)
                    Write(output, EncodeLength(length, 5));

                // Remember positions for later edit in raw data write
                block.codeBytePosition = output.Position;
                block.matchEndPosition = output.Position + 2;

                // Write encoded displacement
                block.codeByte = (byte)((match.Displacement - 1) << 2);
                var byte2 = (byte)((match.Displacement - 1) >> 6);

                output.WriteByte(block.codeByte);
                output.WriteByte(byte2);
            }
            else
            {
                // Write encoded matchLength
                var localCode = (byte)0x10;
                var length = match.Length - 2;
                if (length <= 0x7)
                    localCode |= (byte)length;
                if (match.Displacement >= 0x8000)
                    localCode |= 0x8;

                output.WriteByte(localCode);
                if (length > 0x7)
                    Write(output, EncodeLength(length, 3));

                // Remember positions for later edit in raw data write
                block.codeBytePosition = output.Position;
                block.matchEndPosition = output.Position + 2;

                // Write encoded displacement
                block.codeByte = (byte)(match.Displacement << 2);
                var byte2 = (byte)(match.Displacement >> 6);

                output.WriteByte(block.codeByte);
                output.WriteByte(byte2);
            }

            input.Position += match.Length;
        }

        private byte[] EncodeLength(long length, int bitCount)
        {
            var bitValue = (1 << bitCount) - 1;
            if (length <= bitValue)
                throw new ArgumentOutOfRangeException(nameof(length));

            length -= bitValue;
            var fullBytes = length / 0xFF;
            var remainder = (byte)(length - fullBytes * 0xFF);
            var result = new byte[fullBytes + (remainder > 0 ? 1 : 0)];

            // TODO: Use indexer syntax, when moved to net core-only
            result[result.Length - 1] = remainder > 0 ? remainder : (byte)0xFF;

            return result;
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
