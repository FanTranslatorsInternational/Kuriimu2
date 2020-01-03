using System;
using System.IO;
using Kompression.Configuration;
using Kompression.Interfaces;
using Kompression.Models;

namespace Kompression.Implementations.Encoders
{
    public class LzEncEncoder : IEncoder
    {
        private bool _initialRead;
        private byte _codeByte;
        private long _codeBytePosition;
        private long _matchEndPosition;

        private IMatchParser _matchParser;

        public LzEncEncoder(IMatchParser parser)
        {
            _matchParser = parser;
        }

        public void Encode(Stream input, Stream output)
        {
            _initialRead = true;
            _codeByte = 0;

            var matches = _matchParser.ParseMatches(input);
            foreach (var match in matches)
            {
                if (input.Position < match.Position)
                    WriteRawData(input, output, match.Position - input.Position);

                if (_initialRead)
                    _initialRead = false;

                WriteMatchData(input, output, match);
            }

            if (input.Position < input.Length)
                WriteRawData(input, output, input.Length - input.Position);

            // Write ending match flag
            output.WriteByte(0x11);
            output.WriteByte(0);
            output.WriteByte(0);
        }

        private void WriteRawData(Stream input, Stream output, long length)
        {
            if (_initialRead)
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
                    _codeByte |= (byte)length;

                    output.Position = _codeBytePosition;
                    output.WriteByte(_codeByte);

                    output.Position = _matchEndPosition;
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

        private void WriteMatchData(Stream input, Stream output, Match match)
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
                _codeBytePosition = output.Position;
                _matchEndPosition = output.Position + 2;

                // Write encoded displacement
                _codeByte = (byte)((match.Displacement - 1) << 2);
                var byte2 = (byte)((match.Displacement - 1) >> 6);

                output.WriteByte(_codeByte);
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
                _codeBytePosition = output.Position;
                _matchEndPosition = output.Position + 2;

                // Write encoded displacement
                _codeByte = (byte)(match.Displacement << 2);
                var byte2 = (byte)(match.Displacement >> 6);

                output.WriteByte(_codeByte);
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
            _matchParser?.Dispose();
            _matchParser = null;
        }
    }
}
