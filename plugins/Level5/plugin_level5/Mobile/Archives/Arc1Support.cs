using System.IO;
using Komponent.IO.Attributes;
using Kontract.Models.Archive;

namespace plugin_level5.Mobile.Archives
{
    class Arc1Header
    {
        [FixedLength(4)]
        public string magic = "ARC1";
        public int fileSize;
        public int entryOffset;
        public int entrySize;
        public int unk1;
    }

    class Arc1FileEntry
    {
        public int nameOffset;
        public int offset;
        public int size;
    }

    class Arc1ArchiveFileInfo : ArchiveFileInfo
    {
        public Stream OriginalFileData { get; }

        public Arc1ArchiveFileInfo(Stream fileData, string filePath, bool decrypt, uint seed) : base(GetInternalStream(fileData, decrypt, seed), filePath)
        {
            OriginalFileData = fileData;
        }

        public Stream GetFinalStream()
        {
            return !ContentChanged ? OriginalFileData : base.GetFinalStream();
        }

        private static Stream GetInternalStream(Stream fileData, bool decrypt, uint seed)
        {
            return decrypt ? new Arc1CryptoStream(fileData, seed) : fileData;
        }
    }

    class Arc1CryptoStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly uint _initSeed;

        private uint _positionSeed;

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => _baseStream.CanWrite;
        public override long Length => _baseStream.Length;
        public override long Position
        {
            get => _baseStream.Position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public Arc1CryptoStream(Stream baseStream, uint seed)
        {
            _baseStream = baseStream;
            _initSeed = GetNextSeed(seed + 0x45243);

            _positionSeed = GetPositionSeed(baseStream.Position);
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var oldPosition = Position;
            var newPosition = _baseStream.Seek(offset, origin);

            if (newPosition < oldPosition)
                _positionSeed = GetPositionSeed(newPosition);
            else
                for (var i = oldPosition; i < newPosition; i++)
                    _positionSeed = GetNextSeed(_positionSeed);

            return newPosition;
        }

        public override void SetLength(long value)
        {
            _baseStream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            count = _baseStream.Read(buffer, offset, count);
            ApplyCipher(buffer, offset, count);

            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ApplyCipher(buffer, offset, count);
            _baseStream.Write(buffer, offset, count);
        }

        private void ApplyCipher(byte[] buffer, int offset, int count)
        {
            for (var i = 0; i < count; i++)
            {
                buffer[offset + i] ^= (byte)(_positionSeed >> 0x18);
                _positionSeed = GetNextSeed(_positionSeed);
            }
        }

        private uint GetPositionSeed(long position)
        {
            var s = _initSeed;
            for (var i = 0; i < position; i++)
                s = GetNextSeed(s);

            return s;
        }

        private uint GetNextSeed(uint seed)
        {
            seed *= 0x41C64E6D;
            seed += 0x3039;

            return seed;
        }
    }
}
