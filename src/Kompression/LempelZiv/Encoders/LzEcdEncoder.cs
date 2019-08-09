using System;
using System.IO;
using System.Text;

namespace Kompression.LempelZiv.Encoders
{
    public class LzEcdEncoder : ILzEncoder
    {
        private const int _windowBufferLength = 0x400;
        private readonly int _preBufferLength;

        private byte _codeBlock;
        private int _codeBlockPosition;
        private byte[] _buffer;
        private int _bufferLength;

        public LzEcdEncoder(int preBufferLength)
        {
            _preBufferLength = preBufferLength;
        }

        public void Encode(Stream input, Stream output, LzMatch[] matches)
        {
            var originalOutputPosition = output.Position;
            output.Position += 0x10;

            _codeBlock = 0;
            _codeBlockPosition = 0;
            _buffer = new byte[8 * 2]; // each buffer can be at max 8 pairs of compressed matches; a compressed match is 2 bytes
            _bufferLength = 0;

            foreach (var match in matches)
            {
                // Write any data before the match, to the uncompressed table
                while (input.Position < match.Position - _preBufferLength)
                {
                    if (_codeBlockPosition == 8)
                        WriteAndResetBuffer(output);

                    _codeBlock |= (byte)(1 << _codeBlockPosition++);
                    _buffer[_bufferLength++] = (byte)input.ReadByte();
                }

                // Write match data to the buffer
                var bufferPosition = (match.Position - match.Displacement) % _windowBufferLength;
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

            // Create header values
            var uncompressedLength = GetBigEndian((int)input.Length);

            // Write header
            output.Position = originalOutputPosition;
            output.Write(Encoding.ASCII.GetBytes("ECD"), 0, 3);
            output.WriteByte(1);
            output.Write(GetBigEndian(0), 0, 4);
            output.Write(GetBigEndian((int)output.Length - 0x10), 0, 4);
            output.Write(GetBigEndian((int)input.Length), 0, 4);

            output.Position = outputEndPosition;
        }

        private byte[] GetBigEndian(int value)
        {
            return new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value };
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
