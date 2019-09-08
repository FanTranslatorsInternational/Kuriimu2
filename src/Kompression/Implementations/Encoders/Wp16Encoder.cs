using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.PatternMatch;

namespace Kompression.Implementations.Encoders
{
    class Wp16Encoder : IPatternMatchEncoder
    {
        private long _flagBuffer;
        private int _flagPosition;
        private byte[] _buffer;
        private int _bufferLength;

        public void Encode(Stream input, Stream output, Match[] matches)
        {
            _flagBuffer = 0;
            _flagPosition = 0;
            _buffer = new byte[32 * 2]; // at max 32 matches, one match is 2 bytes
            _bufferLength = 0;

            var start = Encoding.ASCII.GetBytes("Wp16");
            output.Write(start, 0, 4);
            output.Write(GetLittleEndian(input.Length), 0, 4);

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

            if (_flagPosition > 0)
                WriteAndResetBuffer(input, output);
        }

        private void CompressRawData(Stream input, Stream output, int rawLength)
        {
            while (rawLength > 0)
            {
                if (_flagPosition == 32)
                    WriteAndResetBuffer(input, output);

                rawLength -= 2;
                _flagBuffer |= 1L << _flagPosition++;

                _buffer[_bufferLength++] = (byte)input.ReadByte();
                _buffer[_bufferLength++] = (byte)input.ReadByte();
            }

            if (_flagPosition == 32)
                WriteAndResetBuffer(input, output);
        }

        private void CompressMatchData(Stream input, Stream output, Match match)
        {
            if (_flagPosition == 32)
                WriteAndResetBuffer(input, output);

            _flagPosition++;

            var byte1 = (byte)((match.Length / 2 - 2) & 0x1F);
            byte1 |= (byte)(((match.Displacement / 2) & 0x7) << 5);
            var byte2 = (byte)((match.Displacement / 2) >> 3);

            _buffer[_bufferLength++] = byte1;
            _buffer[_bufferLength++] = byte2;

            if (_flagPosition == 32)
                WriteAndResetBuffer(input, output);

            input.Position += match.Length;
        }

        private void WriteAndResetBuffer(Stream input, Stream output)
        {
            // Write data to output
            var buffer = GetLittleEndian(_flagBuffer);
            output.Write(buffer, 0, 4);
            output.Write(_buffer, 0, _bufferLength);

            // Reset codeBlock and buffer
            _flagBuffer = 0;
            _flagPosition = 0;
            Array.Clear(_buffer, 0, _bufferLength);
            _bufferLength = 0;
        }

        private byte[] GetLittleEndian(long value)
        {
            return new[] { (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24) };
        }

        public void Dispose()
        {
            Array.Clear(_buffer, 0, _bufferLength);
            _buffer = null;
        }
    }
}
