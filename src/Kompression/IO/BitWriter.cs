using System;
using System.IO;

namespace Kompression.IO
{
    class BitWriter : IDisposable
    {
        private readonly Stream _baseStream;
        private readonly ByteOrder _byteOrder;
        private readonly BitOrder _bitOrder;
        private readonly int _blockSize;

        private long _buffer;
        private byte _bufferBitPosition;

        public long Position => _baseStream.Position * 8 + _bufferBitPosition;

        public long Length => _baseStream.Length * 8 + _bufferBitPosition;

        public BitWriter(Stream baseStream, BitOrder bitOrder, int blockSize, ByteOrder byteOrder)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _bitOrder = bitOrder;
            _blockSize = blockSize;
            _byteOrder = byteOrder;
        }

        public void WriteBit(int value)
        {
            if (_bufferBitPosition >= _blockSize * 8)
                WriteBuffer();

            _buffer |= (long)((value & 0x1) << _bufferBitPosition++);
        }

        public void WriteByte(int value)
        {
            for (var i = 0; i < 8; i++)
                WriteBit((value >> (7 - i)) & 0x1);
        }

        public void WriteInt32(int value)
        {
            for (var i = 0; i < 32; i++)
                WriteBit((value >> (31 - i)) & 0x1);
        }

        public void Flush()
        {
            if (_bufferBitPosition > 0)
                WriteBuffer();
        }

        private void WriteBuffer()
        {
            if (_bitOrder == BitOrder.MSBFirst)
                _buffer = ReverseBits(_buffer);

            for (var i = 0; i < _blockSize; i++)
                if (_byteOrder == ByteOrder.BigEndian)
                    _baseStream.WriteByte((byte)(_buffer >> ((_blockSize - 1 - i) * 8)));
                else
                    _baseStream.WriteByte((byte)(_buffer >> (i * 8)));

            ResetBuffer();
        }

        private void ResetBuffer()
        {
            _buffer = 0;
            _bufferBitPosition = 0;
        }

        private long ReverseBits(long value)
        {
            long result = 0;

            for (var i = 0; i < _blockSize * 8; i++)
            {
                result <<= 1;
                result |= (byte)(value & 1);
                value >>= 1;
            }

            return result;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Flush();
                _baseStream?.Dispose();
            }
        }
    }
}
