using System;
using System.IO;

namespace Kompression.LempelZiv.Encoders
{
    public class BackwardLz77Encoder : ILzEncoder
    {
        private readonly ByteOrder _byteOrder;
        private byte _codeBlock;
        private int _codeBlockPosition;
        private byte[] _buffer;
        private int _bufferLength;

        public BackwardLz77Encoder(ByteOrder byteOrder)
        {
            _byteOrder = byteOrder;
        }

        public void Encode(Stream input, Stream output, IMatch[] matches)
        {
            // Displacement goes to the end of the file relative to the match position
            // Length goes to the beginning of the file relative to the match position

            _codeBlock = 0;
            _codeBlockPosition = 8;
            // We write all data backwards into the buffer; starting from last element down to first
            _buffer = new byte[8 * 2]; // We have 8 blocks; A block can be at max 2 bytes, defining a match
            _bufferLength = 0;

            foreach (var match in matches)
            {
                var matchStart = match.Position - match.Length + 1;
                while (input.Position < matchStart)
                {
                    if (_codeBlockPosition == 0)
                        WriteAndResetBuffer(output);

                    _codeBlockPosition--;
                    _buffer[_buffer.Length - 1 - _bufferLength++] = (byte)input.ReadByte();
                }

                var byte1 = ((byte)(match.Length - 3) << 4) | (byte)((match.Displacement - 3) >> 8);
                var byte2 = match.Displacement - 3;

                if (_codeBlockPosition == 0)
                    WriteAndResetBuffer(output);

                _codeBlock |= (byte)(1 << --_codeBlockPosition);
                _buffer[_buffer.Length - 1 - _bufferLength++] = (byte)byte2;
                _buffer[_buffer.Length - 1 - _bufferLength++] = (byte)byte1;

                input.Position += match.Length;
            }

            // Write any data after last match, to the buffer
            while (input.Position < input.Length)
            {
                if (_codeBlockPosition == 0)
                    WriteAndResetBuffer(output);

                --_codeBlockPosition;
                _buffer[_buffer.Length - 1 - _bufferLength++] = (byte)input.ReadByte();
            }

            // Flush remaining buffer to stream
            WriteAndResetBuffer(output);

            WriteFooterInformation(input, output);
        }

        private void WriteFooterInformation(Stream input, Stream output)
        {
            // Remember count of padding bytes
            var padding = 0;
            if (output.Position % 4 != 0)
                padding = (int)(4 - output.Position % 4);

            // Write padding
            for (var i = 0; i < padding; i++)
                output.WriteByte(0xFF);

            // Write footer
            var compressedSize = output.Position + 8;
            var bufferTopAndBottomInt = ((8 + padding) << 24) | (int)(compressedSize & 0xFFFFFF);
            var originalBottomInt = (int)(input.Length - compressedSize);

            var bufferTopAndBottom = _byteOrder == ByteOrder.LittleEndian
                ? GetLittleEndian(bufferTopAndBottomInt)
                : GetBigEndian(bufferTopAndBottomInt);
            var originalBottom = _byteOrder == ByteOrder.LittleEndian
                ? GetLittleEndian(originalBottomInt)
                : GetBigEndian(originalBottomInt);
            output.Write(bufferTopAndBottom, 0, 4);
            output.Write(originalBottom, 0, 4);
        }

        private void WriteAndResetBuffer(Stream output)
        {
            // Write data to output
            output.Write(_buffer, _buffer.Length - _bufferLength, _bufferLength);
            output.WriteByte(_codeBlock);

            // Reset codeBlock and buffer
            _codeBlock = 0;
            _codeBlockPosition = 8;
            Array.Clear(_buffer, 0, _bufferLength);
            _bufferLength = 0;
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
