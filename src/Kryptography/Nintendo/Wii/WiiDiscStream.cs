using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kryptography.AES;

namespace Kryptography.Nintendo.Wii
{
    public class WiiDiscStream : Stream
    {
        private const int PartitionInfoStart_ = 0x40000;
        private const int PartitionInfoEnd_ = 0x40020;

        private const int PartitionTitleKeyStart_ = 0x1BF;
        private const int PartitionTitleIdStart_ = 0x1DC;
        private const int PartitionTitleIdEnd_ = 0x1E4;
        private const int PartitionCommonKeyIndexStart_ = 0x1F1;

        private static IList<byte[]> _commonKeys = new List<byte[]>
        {
            new byte[] {0xEB, 0xE4, 0x2A, 0x22, 0x5E, 0x85, 0x93, 0xE4, 0x48, 0xD9, 0xC5, 0x45, 0x73, 0x81, 0xAA, 0xF7},
            new byte[] {0x63, 0xB8, 0x2B, 0xB4, 0xF4, 0x61, 0x4E, 0x2E, 0x13, 0xF2, 0xFE, 0xFB, 0xBA, 0x4C, 0x9B, 0x7E}
        };

        private const int PartitionEntrySize_ = 8;

        private readonly Stream _baseStream;
        private readonly IList<(long offset, Stream stream)> _partitions;

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _baseStream.Length;
        public override long Position { get; set; }

        public WiiDiscStream(Stream baseStream)
        {
            if (baseStream == null)
                throw new ArgumentNullException(nameof(baseStream));

            _baseStream = baseStream;

            var partitions = PeekPartitions(baseStream);
            if (partitions == null)
                throw new InvalidOperationException("Stream is not a WiiDisc.");

            _partitions = new List<(long, Stream)>(partitions.Count);
            for (var i = 0; i < partitions.Count; i++)
            {
                var offset = partitions[i].offset;
                var endOffset = i + 1 == partitions.Count ? baseStream.Length : partitions[i + 1].offset;

                var subStream = new SubStream(baseStream, offset, endOffset - offset);
                var partitionKey = PeekPartitionKey(subStream);
                _partitions.Add((offset, new WiiDiscPartitionStream(subStream, partitionKey)));
            }
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

            var readBytes = 0;
            var bkPos = _baseStream.Position;

            // A disc consists of unencrypted header data and encrypted partitions

            // Read unencrypted header data
            var firstPartition = _partitions.First();
            if (Position < firstPartition.offset)
            {
                var length = (int)Math.Min(firstPartition.offset - Position, count);
                _baseStream.Position = Position;
                readBytes += _baseStream.Read(buffer, offset, length);

                Position += length;
                offset += length;
                count -= length;
            }

            // Read encrypted partition data
            var partitionIndex = GetPartitionIndex();
            while (count > 0 && Position < Length)
            {
                var partition = _partitions[partitionIndex++];
                var partitionPosition = Position - partition.offset;

                partition.stream.Position = partitionPosition;
                var length = (int)Math.Min(partition.stream.Length - partitionPosition, count);
                readBytes += partition.stream.Read(buffer, offset, length);

                Position += length;
                offset += length;
                count -= length;
            }

            _baseStream.Position = bkPos;
            return readBytes;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        #region Validation Methods

        private void ValidateRead(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException("Read is not supported.");

            ValidateInput(buffer, offset, count);
        }

        private void ValidateWrite(byte[] buffer, int offset, int count)
        {
            if (!CanWrite) throw new NotSupportedException("Write is not supported");
            if (Position >= Length) throw new ArgumentOutOfRangeException("Stream has fixed length and Position was out of range.");
            if (Length - Position < count) throw new InvalidOperationException("Stream has fixed length and tries to write too much data.");

            ValidateInput(buffer, offset, count);
        }

        private void ValidateInput(byte[] buffer, int offset, int count)
        {
            if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException("Offset or count can't be negative.");
            if (offset + count > buffer.Length) throw new InvalidDataException("Buffer too short.");
        }

        #endregion

        private IList<(int offset, int type)> PeekPartitions(Stream input)
        {
            if (input.Length <= PartitionInfoEnd_)
                return null;

            var bkPos = input.Position;
            input.Position = PartitionInfoStart_;

            // Read partition infos
            var partitionInfos = new List<(int count, int offset)>();
            for (var i = 0; i < 4; i++)
                partitionInfos.Add((ReadInt32(input), ReadInt32(input) << 2));

            // If any partition entry is outside of the stream
            if (partitionInfos.Any(x => input.Length <= x.Item2 + x.Item1 * PartitionEntrySize_))
            {
                input.Position = bkPos;
                return null;
            }

            // Read partition entries
            var partitionEntries = new List<(int offset, int type)>();
            foreach (var partitionInfo in partitionInfos)
            {
                input.Position = partitionInfo.offset;

                var localPartitionEntries = new List<(int, int)>();
                for (var i = 0; i < partitionInfo.count; i++)
                    localPartitionEntries.Add((ReadInt32(input) << 2, ReadInt32(input)));

                partitionEntries.AddRange(localPartitionEntries);
            }

            // If any partition is outside of the stream
            if (partitionEntries.Any(x => input.Length <= x.offset))
            {
                input.Position = bkPos;
                return null;
            }

            input.Position = bkPos;
            return partitionEntries;
        }

        private byte[] PeekPartitionKey(Stream partitionStream)
        {
            if (partitionStream.Length <= PartitionCommonKeyIndexStart_)
                return null;

            var bkPos = partitionStream.Position;

            // Read encrypted partitionKey
            partitionStream.Position = PartitionTitleKeyStart_;
            var partitionKey = new byte[0x10];
            partitionStream.Read(partitionKey, 0, 0x10);

            // Read titleId
            partitionStream.Position = PartitionTitleIdStart_;
            var titleId = new byte[0x10];
            partitionStream.Read(titleId, 0, 8);

            // Read common key index
            partitionStream.Position = PartitionCommonKeyIndexStart_;
            var commonKeyIndex = partitionStream.ReadByte();
            if (commonKeyIndex < 0 || commonKeyIndex >= _commonKeys.Count)
            {
                partitionStream.Position = bkPos;
                return null;
            }

            // Decrypt partitionKey
            var cbcStream = new CbcStream(new MemoryStream(partitionKey), _commonKeys[commonKeyIndex], titleId);
            var decryptedPartitionKey = new byte[0x10];
            cbcStream.Read(decryptedPartitionKey, 0, 0x10);

            partitionStream.Position = bkPos;
            return decryptedPartitionKey;
        }

        private int GetPartitionIndex()
        {
            for (var i = 0; i < _partitions.Count; i++)
                if (Position >= _partitions[i].offset &&
                    Position < _partitions[i].offset + _partitions[i].stream.Length)
                    return i;

            return -1;
        }

        private int ReadInt32(Stream input)
        {
            // BigEndian
            var buffer = new byte[4];
            input.Read(buffer, 0, 4);
            return (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
        }
    }
}
