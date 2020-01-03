using System;
using System.Diagnostics;
using System.IO;
using Kompression.Configuration;
using Kompression.Extensions;
using Kompression.Interfaces;
using Kompression.Models;

namespace Kompression.Implementations.Encoders
{
    public class TalesOf01Encoder : IEncoder
    {
        private const int WindowBufferLength = 0x1000;

        private IMatchParser _matchParser;

        private byte[] _buffer;
        private int _bufferLength;
        private int _flagCount;

        public TalesOf01Encoder(IMatchParser parser)
        {
            _matchParser = parser;
        }

        public void Encode(Stream input, Stream output)
        {
            _buffer = new byte[1 + 8 * 2];
            _bufferLength = 1;
            _flagCount = 0;

            output.Position += 9;

            var matches = _matchParser.ParseMatches(input);
            foreach (var match in matches)
            {
                if (input.Position < match.Position-_matchParser.FindOptions.PreBufferSize)
                    WriteRawData(input, output, match.Position - _matchParser.FindOptions.PreBufferSize - input.Position);

                WriteMatchData(input, output, match);
            }

            if (input.Position < input.Length)
                WriteRawData(input, output, input.Length - input.Position);

            WriteAndResetBuffer(output);

            WriteHeaderData(output, (int)input.Length);
        }

        private void WriteRawData(Stream input, Stream output, long rawLength)
        {
            for (var i = 0; i < rawLength; i++)
            {
                if (_flagCount == 8)
                    WriteAndResetBuffer(output);

                _buffer[0] |= (byte)(1 << _flagCount++);
                _buffer[_bufferLength++] = (byte)input.ReadByte();
            }
        }

        private void WriteMatchData(Stream input, Stream output, Match match)
        {
            if (_flagCount == 8)
                WriteAndResetBuffer(output);

            var bufferPosition = (match.Position - match.Displacement) % WindowBufferLength;

            var byte2 = (byte)((match.Length - 3) & 0xF);
            byte2 |= (byte)((bufferPosition >> 4) & 0xF0);
            var byte1 = (byte)bufferPosition;

            _flagCount++;
            _buffer[_bufferLength++] = byte1;
            _buffer[_bufferLength++] = byte2;
            input.Position += match.Length;
        }

        private void WriteHeaderData(Stream output, int decompressedLength)
        {
            var endPosition = output.Position;
            output.Position = 0;

            output.WriteByte(1);
            Write(output, ((int)output.Length).GetArrayLittleEndian());
            Write(output, decompressedLength.GetArrayLittleEndian());

            output.Position = endPosition;
        }

        private void WriteAndResetBuffer(Stream output)
        {
            output.Write(_buffer, 0, _bufferLength);

            Array.Clear(_buffer, 0, _bufferLength);
            _bufferLength = 1;
            _flagCount = 0;
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

            _buffer = null;
        }
    }
}
