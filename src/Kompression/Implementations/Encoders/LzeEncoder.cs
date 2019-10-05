using System;
using System.IO;
using System.Text;
using Kompression.Configuration;
using Kompression.Extensions;
using Kompression.Interfaces;
using Kompression.Models;

namespace Kompression.Implementations.Encoders
{
    public class LzeEncoder : IEncoder, IPriceCalculator
    {
        private IMatchParser _matchParser;

        private byte _codeBlock;
        private int _codeBlockPosition;
        private byte[] _buffer;
        private int _bufferLength;

        public LzeEncoder(IMatchParser matchParser)
        {
            _matchParser = matchParser;
        }

        public void Encode(Stream input, Stream output)
        {
            var originalOutputPosition = output.Position;
            output.Position += 6;

            _codeBlock = 0;
            _codeBlockPosition = 0;
            _buffer = new byte[4 * 3]; // each buffer can be at max 4 triplets of uncompressed data; a triplet is 3 bytes
            _bufferLength = 0;

            var matches = _matchParser.ParseMatches(input);
            foreach (var match in matches)
            {
                // Compress raw data
                if (input.Position < match.Position)
                    CompressRawData(input, output, (int)(match.Position - input.Position));

                // Compress match
                CompressMatchData(input, output, match);
            }

            // Compress raw data
            if (input.Position < input.Length)
                CompressRawData(input, output, (int)(input.Length - input.Position));

            WriteAndResetBuffer(output);

            // Write header information
            WriteHeaderData(input, output, originalOutputPosition);
        }

        private void CompressRawData(Stream input, Stream output, int length)
        {
            while (length > 0)
            {
                if (_codeBlockPosition == 4)
                    WriteAndResetBuffer(output);

                if (length >= 3)
                {
                    length -= 3;
                    _codeBlock |= (byte)(3 << (_codeBlockPosition++ << 1));
                    _buffer[_bufferLength++] = (byte)input.ReadByte();
                    _buffer[_bufferLength++] = (byte)input.ReadByte();
                    _buffer[_bufferLength++] = (byte)input.ReadByte();
                }
                else
                {
                    length--;
                    _codeBlock |= (byte)(2 << (_codeBlockPosition++ << 1));
                    _buffer[_bufferLength++] = (byte)input.ReadByte();
                }
            }
        }

        private void CompressMatchData(Stream input, Stream output, Match match)
        {
            if (_codeBlockPosition == 4)
                WriteAndResetBuffer(output);

            if (match.Displacement <= 4)
            {
                _codeBlock |= (byte)(1 << (_codeBlockPosition++ << 1));

                var byte1 = ((match.Length - 2) << 2) | (match.Displacement - 1);
                _buffer[_bufferLength++] = (byte)byte1;
            }
            else
            {
                _codeBlockPosition++;

                var byte1 = match.Displacement - 5;
                var byte2 = ((match.Length - 3) << 4) | ((match.Displacement - 5) >> 8);
                _buffer[_bufferLength++] = (byte)byte1;
                _buffer[_bufferLength++] = (byte)byte2;
            }

            input.Position += match.Length;
        }

        private void WriteAndResetBuffer(Stream output)
        {
            // Write data to output
            output.WriteByte(_codeBlock);
            output.Write(_buffer, 0, _bufferLength);

            // Reset codeBlock and buffer
            _codeBlock = 0;
            _codeBlockPosition = 0;
            Array.Clear(_buffer, 0, _bufferLength);
            _bufferLength = 0;
        }

        private void WriteHeaderData(Stream input, Stream output, long originalOutputPosition)
        {
            var outputEndPosition = output.Position;

            // Create header values
            var uncompressedLength = ((int)input.Length).GetArrayLittleEndian();

            // Write header
            output.Position = originalOutputPosition;
            output.Write(Encoding.ASCII.GetBytes("Le"), 0, 2);
            output.Write(uncompressedLength, 0, 4);

            output.Position = outputEndPosition;
        }

        #region Price calculation

        public int CalculateLiteralPrice(IMatchState state, int position, int value)
        {
            var literalCount = state.CountLiterals(position) % 3 + 1;
            if (literalCount == 3)
                return 6;

            return 10;
        }

        public int CalculateMatchPrice(IMatchState state, int position, int displacement, int length)
        {
            if (displacement > 4 && length > 0x12)
                throw new InvalidOperationException("Invalid match for Lze.");

            if (displacement <= 4)
                return 10;

            return 18;
        }

        #endregion

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
