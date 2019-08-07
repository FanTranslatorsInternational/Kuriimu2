using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public void Encode(Stream input, Stream output, LzMatch[] matches)
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
            // TODO: Pad output to 4 bytes and fill the padding with 0xFF
            // TODO: Write the footer
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

        public void Dispose()
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _buffer = null;
        }
    }
}
