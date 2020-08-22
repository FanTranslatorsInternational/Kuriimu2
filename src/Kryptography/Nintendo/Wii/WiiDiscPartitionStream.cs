using System;
using System.IO;
using Kryptography.AES;

namespace Kryptography.Nintendo.Wii
{
    class WiiDiscPartitionStream : Stream
    {
        private const int BlockSize_ = 0x8000;
        private const int DataOffsetStart_ = 0x2B8;
        private const int DataSizeEnd_ = 0x2C0;

        private const int BlockHashSize_ = 0x400;
        private const int BlockDataSize_ = 0x7C00;
        private const int BlockDataIvStart_ = 0x3D0;

        private readonly Stream _baseStream;
        private readonly byte[] _partitionKey;

        private readonly int _dataOffset;

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _baseStream.Length;
        public override long Position { get; set; }

        public WiiDiscPartitionStream(Stream baseStream, byte[] partitionKey)
        {
            if (baseStream == null)
                throw new ArgumentNullException(nameof(baseStream));
            if (partitionKey.Length != 0x10)
                throw new ArgumentException("Partition key has to be 16 bytes.");
            if (baseStream.Length % BlockSize_ != 0)
                throw new InvalidOperationException($"WiiDisc partition has to be aligned to 0x{BlockSize_:X4} bytes.");

            _baseStream = baseStream;
            _partitionKey = partitionKey;

            _dataOffset = PeekDataOffset(baseStream);
            if (_dataOffset < 0)
                throw new InvalidOperationException("Partition has invalid data information.");
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    return Position = offset;

                case SeekOrigin.Current:
                    return Position += offset;

                case SeekOrigin.End:
                    return Position = Length + offset;
            }

            throw new ArgumentException("Origin is invalid.");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateRead(buffer, offset, count);

            // A partition consists of an unencrypted partition header and encrypted user data
            // User data consists of blocks of size 0x8000
            // Each block has 0x400 bytes encrypted SHA-1 and 0x7C00 bytes encrypted user data

            var readBytes = 0;
            var bkPos = _baseStream.Position;

            // Read unencrypted header
            if (Position < _dataOffset)
            {
                var length = (int)Math.Min(_dataOffset - Position, count);
                _baseStream.Position = Position;
                readBytes += _baseStream.Read(buffer, offset, length);

                Position += length;
                offset += length;
                count -= length;
            }

            // Read encrypted blocks
            while (count > 0 && Position < Length)
                readBytes += ReadNextBlock(buffer, ref offset, ref count);

            _baseStream.Position = bkPos;
            return readBytes;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        #region Validate Methods

        private void ValidateRead(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException("Read is not supported.");

            ValidateInput(buffer, offset, count);
        }

        private void ValidateInput(byte[] buffer, int offset, int count)
        {
            if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException("Offset or count can't be negative.");
            if (offset + count > buffer.Length) throw new InvalidDataException("Buffer too short.");
        }

        #endregion

        private int PeekDataOffset(Stream input)
        {
            if (input.Length < DataSizeEnd_)
                return -1;

            // Read data offset and size
            var bkPos = input.Position;
            input.Position = DataOffsetStart_;
            var dataOffset = ReadInt32(input) << 2;
            var dataSize = ReadInt32(input) << 2;

            // If data portion is not inside stream
            if (input.Length < dataOffset + dataSize)
            {
                input.Position = bkPos;
                return -1;
            }

            input.Position = bkPos;
            return dataOffset;
        }

        private int ReadInt32(Stream input)
        {
            // BigEndian
            var buffer = new byte[4];
            input.Read(buffer, 0, 4);
            return (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
        }

        private int ReadNextBlock(byte[] buffer, ref int offset, ref int count)
        {
            int length;
            var readBytes = 0;
            var blockPosition = Position % BlockSize_;
            var blockStart = Position - blockPosition;

            if (count <= 0 || Position >= Length)
                return readBytes;

            // Read and decrypt SHA1 portion of the block
            var hashPartStream = new SubStream(_baseStream, blockStart, BlockHashSize_);
            if (blockPosition < BlockHashSize_)
            {
                var cbcHashPartStream = new CbcStream(hashPartStream, _partitionKey, new byte[0x10]);

                length = (int)Math.Min(BlockHashSize_ - blockPosition, count);
                cbcHashPartStream.Position = blockPosition;
                readBytes += cbcHashPartStream.Read(buffer, offset, length);

                Position += length;
                offset += length;
                count -= length;

                blockPosition = Position % BlockSize_;
            }

            if (count <= 0 || Position >= Length)
                return readBytes;

            // Read and decrypt user data
            hashPartStream.Position = BlockDataIvStart_;
            var dataIv = new byte[0x10];
            hashPartStream.Read(dataIv, 0, 0x10);

            var dataPartStream = new SubStream(_baseStream, blockStart + BlockHashSize_, BlockDataSize_);
            var cbcDataPartStream = new CbcStream(dataPartStream, _partitionKey, dataIv);

            length = (int)Math.Min(BlockSize_ - blockPosition, count);
            cbcDataPartStream.Position = blockPosition - BlockHashSize_;
            readBytes += cbcDataPartStream.Read(buffer, offset, length);

            Position += length;
            offset += length;
            count -= length;

            return readBytes;
        }
    }
}
