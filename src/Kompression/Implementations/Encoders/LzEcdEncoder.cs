using System;
using System.IO;
using System.Text;
using Kompression.Configuration;
using Kompression.Extensions;
using Kompression.Interfaces;

namespace Kompression.Implementations.Encoders
{
    public class LzEcdEncoder : IEncoder
    {
        private const int WindowBufferLength = 0x400;
        private IMatchParser _matchParser;

        private byte _codeBlock;
        private int _codeBlockPosition;
        private byte[] _buffer;
        private int _bufferLength;

        public LzEcdEncoder(IMatchParser matchParser)
        {
            _matchParser = matchParser;
        }

        public void Encode(Stream input, Stream output)
        {
            var originalOutputPosition = output.Position;
            output.Position += 0x10;

            _codeBlock = 0;
            _codeBlockPosition = 0;
            _buffer = new byte[8 * 2]; // each buffer can be at max 8 pairs of compressed matches; a compressed match is 2 bytes
            _bufferLength = 0;

            var matches = _matchParser.ParseMatches(input);
            foreach (var match in matches)
            {
                // Write any data before the match, to the uncompressed table
                while (input.Position < match.Position - _matchParser.FindOptions.PreBufferSize)
                {
                    if (_codeBlockPosition == 8)
                        WriteAndResetBuffer(output);

                    _codeBlock |= (byte)(1 << _codeBlockPosition++);
                    _buffer[_bufferLength++] = (byte)input.ReadByte();
                }

                // Write match data to the buffer
                var bufferPosition = (match.Position - match.Displacement) % WindowBufferLength;
                var firstByte = (byte)bufferPosition;
                var secondByte = (byte)(((bufferPosition >> 2) & 0xC0) | (byte)(match.Length - 3));

                if (_codeBlockPosition == 8)
                    WriteAndResetBuffer(output);

                _codeBlockPosition++; // Since a match is flagged with a 0 bit, we don't need a bit shift and just increase the position
                _buffer[_bufferLength++] = firstByte;
                _buffer[_bufferLength++] = secondByte;

                input.Position += match.Length;
            }

            // Write any data after last match, to the buffer
            while (input.Position < input.Length)
            {
                if (_codeBlockPosition == 8)
                    WriteAndResetBuffer(output);

                _codeBlock |= (byte)(1 << _codeBlockPosition++);
                _buffer[_bufferLength++] = (byte)input.ReadByte();
            }

            // Flush remaining buffer to stream
            WriteAndResetBuffer(output);

            // Write header information
            WriteHeaderData(input, output, originalOutputPosition);
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

            // Write header
            output.Position = originalOutputPosition;
            output.Write(Encoding.ASCII.GetBytes("ECD"), 0, 3);
            output.WriteByte(1);
            output.Write(new byte[4], 0, 4);
            output.Write(((int)output.Length - 0x10).GetArrayBigEndian(), 0, 4);
            output.Write(((int)input.Length).GetArrayBigEndian(), 0, 4);

            output.Position = outputEndPosition;
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
