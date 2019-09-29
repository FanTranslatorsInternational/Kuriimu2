using System;
using System.IO;
using System.Text;
using Kompression.Configuration;
using Kompression.Interfaces;
using Kompression.IO;

namespace Kompression.Implementations.Encoders
{
    public class Yaz0Encoder : IEncoder
    {
        private readonly ByteOrder _byteOrder;
        private IMatchParser _matchParser;

        private byte _codeBlock;
        private int _codeBlockPosition;
        private byte[] _buffer;
        private int _bufferLength;

        public Yaz0Encoder(ByteOrder byteOrder, IMatchParser matchParser)
        {
            _byteOrder = byteOrder;
            _matchParser = matchParser;
        }

        public void Encode(Stream input, Stream output)
        {
            var originalOutputPosition = output.Position;
            output.Position += 0x10;

            _codeBlock = 0;
            _codeBlockPosition = 8;
            _buffer = new byte[8 * 3]; // each buffer can be at max 8 pairs of compressed matches; a compressed match can be at max 3 bytes
            _bufferLength = 0;

            var matches = _matchParser.ParseMatches(input);
            foreach (var match in matches)
            {
                // Write any data before the match, to the buffer
                while (input.Position < match.Position)
                {
                    if (_codeBlockPosition == 0)
                        WriteAndResetBuffer(output);

                    _codeBlock |= (byte)(1 << --_codeBlockPosition);
                    _buffer[_bufferLength++] = (byte)input.ReadByte();
                }

                // Write match data to the buffer
                var firstByte = (byte)((match.Displacement - 1) >> 8);
                var secondByte = (byte)(match.Displacement - 1);

                if (match.Length < 0x12)
                    // Since minimum _length should be 3 for Yay0, we get a minimum matchLength of 1 in this case
                    firstByte |= (byte)((match.Length - 2) << 4);

                if (_codeBlockPosition == 0)
                    WriteAndResetBuffer(output);

                _codeBlockPosition--; // Since a match is flagged with a 0 bit, we don't need a bit shift and just decrease the position
                _buffer[_bufferLength++] = firstByte;
                _buffer[_bufferLength++] = secondByte;
                if (match.Length >= 0x12)
                    _buffer[_bufferLength++] = (byte)(match.Length - 0x12);

                input.Position += match.Length;
            }

            // Write any data after last match, to the buffer
            while (input.Position < input.Length)
            {
                if (_codeBlockPosition == 0)
                    WriteAndResetBuffer(output);

                _codeBlock |= (byte)(1 << --_codeBlockPosition);
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
            _codeBlockPosition = 8;
            Array.Clear(_buffer, 0, _bufferLength);
            _bufferLength = 0;
        }

        private void WriteHeaderData(Stream input, Stream output, long originalOutputPosition)
        {
            var outputEndPosition = output.Position;

            // Create header values
            var uncompressedLength = _byteOrder == ByteOrder.LittleEndian
                ? GetLittleEndian((int)input.Length)
                : GetBigEndian((int)input.Length);

            // Write header
            output.Position = originalOutputPosition;
            output.Write(Encoding.ASCII.GetBytes("Yaz0"), 0, 4);
            output.Write(uncompressedLength, 0, 4);
            output.Write(new byte[8], 0, 8);
            output.Position = outputEndPosition;
        }

        private byte[] GetLittleEndian(int value)
        {
            return new[] { (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24) };
        }

        private byte[] GetBigEndian(int value)
        {
            return new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value };
        }

        public void Dispose()
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _buffer = null;
        }
    }
}
