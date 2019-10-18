using System;
using System.Diagnostics;
using System.IO;
using Kompression.Configuration;
using Kompression.Extensions;
using Kompression.Interfaces;
using Kompression.Models;

namespace Kompression.Implementations.Encoders
{
    public class TalesOf03Encoder : IEncoder
    {
        private const int WindowBufferLength = 0x1000;

        private IMatchParser _matchParser;

        private byte[] _buffer;
        private int _bufferLength;
        private int _flagCount;

        public TalesOf03Encoder(IMatchParser parser)
        {
            _matchParser = parser;
        }

        public void Encode(Stream input, Stream output)
        {
            _buffer = new byte[1 + 8 * 3];
            _bufferLength = 1;
            _flagCount = 0;

            output.Position += 9;

            var matches = _matchParser.ParseMatches(input);
            foreach (var match in matches)
            {
                if (input.Position < match.Position - _matchParser.FindOptions.PreBufferSize)
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

            if (match.Position - _matchParser.FindOptions.PreBufferSize > 0x3c700)
                ;//Debugger.Break();

            if (match.Displacement == 0)
            {
                // Encode RLE
                if (match.Length >= 0x13)
                {
                    var byte2 = (byte)0x0F;
                    var byte1 = (byte)(match.Length - 0x13);

                    _buffer[_bufferLength++] = byte1;
                    _buffer[_bufferLength++] = byte2;
                    _buffer[_bufferLength++] = (byte)input.ReadByte();
                    input.Position += match.Length - 1;
                }
                else
                {
                    var byte2 = (byte)(((match.Length - 3) & 0xF) << 4);
                    byte2 |= 0xF;
                    var byte1 = (byte)input.ReadByte();

                    _buffer[_bufferLength++] = byte1;
                    _buffer[_bufferLength++] = byte2;
                    input.Position += match.Length - 1;
                }
            }
            else
            {
                // Encode LZ
                var bufferPosition = (match.Position - match.Displacement) % WindowBufferLength;

                var byte1 = (byte)bufferPosition;
                var byte2 = (byte)((match.Length - 3) & 0xF);
                byte2 |= (byte)((bufferPosition >> 4) & 0xF0);

                _buffer[_bufferLength++] = byte1;
                _buffer[_bufferLength++] = byte2;
                input.Position += match.Length;
            }

            _flagCount++;
        }

        private void WriteHeaderData(Stream output, int decompressedLength)
        {
            var endPosition = output.Position;
            output.Position = 0;

            output.WriteByte(3);
            output.Write(((int)output.Length).GetArrayLittleEndian());
            output.Write(decompressedLength.GetArrayLittleEndian());

            output.Position = endPosition;
        }

        private void WriteAndResetBuffer(Stream output)
        {
            output.Write(_buffer, 0, _bufferLength);

            Array.Clear(_buffer, 0, _bufferLength);
            _bufferLength = 1;
            _flagCount = 0;
        }

        public void Dispose()
        {
            _matchParser?.Dispose();
            _matchParser = null;
        }
    }
}
