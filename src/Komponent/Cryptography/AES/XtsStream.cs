using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Abstracts;
using Komponent.Cryptography.AES.XTS;

namespace Komponent.Cryptography.AES
{
    public class XtsStream : KryptoStream
    {
        public int SectorSize { get; }

        private Stream _stream;
        private Xts _xts;
        private long _sectorsBetweenLengthPosition = 0;
        private long _sectorPosition = 0;
        private long _bytesIntoSector = 0;

        private long _offset;
        private long _length;
        private bool _fixedLength = false;

        public override int BlockSize => 128;
        public override int BlockSizeBytes => 16;
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;

        public override List<byte[]> Keys { get; }
        public override int KeySize => Keys?[0]?.Length ?? 0;
        public override byte[] IV => throw new NotImplementedException();
        public override long Length => _length;
        public override long Position { get => _stream.Position - _offset; set => Seek(value, SeekOrigin.Begin); }
        private long TotalSectors => GetSectorCount(Length);

        private long GetSectorCount(long input) => (long)Math.Ceiling((double)input / SectorSize);
        private long GetCurrentSector(long input) => input / SectorSize;

        public XtsStream(byte[] input, long offset, long length, byte[] key1, bool nintendo_tweak = false) : this(input, offset, length, key1, null, 512, nintendo_tweak) { }

        public XtsStream(byte[] input, long offset, long length, byte[] key1, int sectorSize, bool nintendo_tweak = false) : this(input, offset, length, key1, null, sectorSize, nintendo_tweak) { }

        public XtsStream(byte[] input, long offset, long length, byte[] key1, byte[] key2, bool nintendo_tweak = false) : this(input, offset, length, key1, key2, 512, nintendo_tweak) { }

        public XtsStream(byte[] input, long offset, long length, byte[] key1, byte[] key2, int sectorSize, bool nintendo_tweak = false) : this(new MemoryStream(input), offset, length, key1, key2, sectorSize, nintendo_tweak) { }

        public XtsStream(Stream input, long offset, long length, byte[] key1, bool nintendo_tweak = false) : this(input, offset, length, key1, null, 512, nintendo_tweak) { }

        public XtsStream(Stream input, long offset, long length, byte[] key1, int sectorSize, bool nintendo_tweak = false) : this(input, offset, length, key1, null, sectorSize, nintendo_tweak) { }

        public XtsStream(Stream input, long offset, long length, byte[] key1, byte[] key2, bool nintendo_tweak = false) : this(input, offset, length, key1, key2, 512, nintendo_tweak) { }

        public XtsStream(Stream input, long offset, long length, byte[] key1, byte[] key2, int sectorSize, bool nintendo_tweak = false)
        {
            _stream = input;
            _offset = offset;
            _length = length;
            _fixedLength = true;

            SectorSize = sectorSize;

            Keys = new List<byte[]>();
            Keys.Add(key1);
            if (key2 != null)
                Keys.Add(key2);

            if (key2 != null)
            {
                if (key1.Length == 128 / 8 && key2.Length == 128 / 8)
                {
                    _xts = XtsAes128.Create(key1, key2, nintendo_tweak);
                }
                else if (key1.Length == 256 / 8 && key2.Length == 256 / 8)
                {
                    _xts = XtsAes256.Create(key1, key2, nintendo_tweak);
                }
                else
                    throw new InvalidDataException("Key1 or Key2 have invalid size.");
            }
            else
            {
                if (key1.Length == 256 / 8)
                {
                    _xts = XtsAes128.Create(key1, nintendo_tweak);
                }
                else if (key1.Length == 512 / 8)
                {
                    _xts = XtsAes256.Create(key1, nintendo_tweak);
                }
                else
                    throw new InvalidDataException("Key1 has invalid size.");
            }
        }

        public XtsStream(byte[] input, byte[] key1, bool nintendo_tweak = false) : this(input, key1, null, 512, nintendo_tweak) { }

        public XtsStream(byte[] input, byte[] key1, int sectorSize, bool nintendo_tweak = false) : this(input, key1, null, sectorSize, nintendo_tweak) { }

        public XtsStream(byte[] input, byte[] key1, byte[] key2, bool nintendo_tweak = false) : this(input, key1, key2, 512, nintendo_tweak) { }

        public XtsStream(byte[] input, byte[] key1, byte[] key2, int sectorSize, bool nintendo_tweak = false) : this(new MemoryStream(input), key1, key2, sectorSize, nintendo_tweak) { }

        public XtsStream(Stream input, byte[] key1, bool nintendo_tweak = false) : this(input, key1, null, 512, nintendo_tweak) { }

        public XtsStream(Stream input, byte[] key1, int sectorSize, bool nintendo_tweak = false) : this(input, key1, null, sectorSize, nintendo_tweak) { }

        public XtsStream(Stream input, byte[] key1, byte[] key2, bool nintendo_tweak = false) : this(input, key1, key2, 512, nintendo_tweak) { }

        public XtsStream(Stream input, byte[] key1, byte[] key2, int sectorSize, bool nintendo_tweak = false)
        {
            _stream = input;
            _offset = 0;
            _length = input.Length;
            _fixedLength = false;

            SectorSize = sectorSize;

            Keys = new List<byte[]>();
            Keys.Add(key1);
            if (key2 != null)
                Keys.Add(key2);

            if (key2 != null)
            {
                if (key1.Length == 128 / 8 && key2.Length == 128 / 8)
                {
                    _xts = XtsAes128.Create(key1, key2, nintendo_tweak);
                }
                else if (key1.Length == 256 / 8 && key2.Length == 256 / 8)
                {
                    _xts = XtsAes256.Create(key1, key2, nintendo_tweak);
                }
                else
                    throw new InvalidDataException("Key1 or Key2 have invalid size.");
            }
            else
            {
                if (key1.Length == 256 / 8)
                {
                    _xts = XtsAes128.Create(key1, nintendo_tweak);
                }
                else if (key1.Length == 512 / 8)
                {
                    _xts = XtsAes256.Create(key1, nintendo_tweak);
                }
                else
                    throw new InvalidDataException("Key1 has invalid size.");
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateRead(buffer, offset, count);

            return ReadDecrypted(buffer, offset, count);
        }

        private void ValidateRead(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException("Reading is not supported.");
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException("Offset or count can't be negative.");
            if (offset + count > buffer.Length)
                throw new InvalidDataException("Buffer too short.");
        }

        private int ReadDecrypted(byte[] buffer, int offset, int count)
        {
            var originalPosition = Position;

            count = (int)Math.Min(count, Length - Position);
            var alignedCount = (int)GetSectorCount(_bytesIntoSector + count) * SectorSize;

            if (alignedCount == 0) return alignedCount;

            var decData = Decrypt(_sectorPosition, alignedCount);

            Array.Copy(decData, _bytesIntoSector, buffer, offset, count);

            Seek(originalPosition + count, SeekOrigin.Begin);

            return count;
        }

        private byte[] Decrypt(long begin, int alignedCount)
        {
            _stream.Position = begin + _offset;
            var readData = new byte[alignedCount];
            _stream.Read(readData, 0, alignedCount);

            var decData = DecryptSectors(readData, (ulong)GetCurrentSector(begin));

            return decData;
        }

        private byte[] DecryptSectors(byte[] sectors, ulong sectorId)
        {
            if (sectors.Length % SectorSize != 0)
                throw new InvalidDataException($"{nameof(DecryptSectors)} needs sector padded amount of data.");

            byte[] result = new byte[sectors.Length];
            for (int i = 0; i < GetSectorCount(sectors.Length); i++)
                _xts.CreateDecryptor().TransformBlock(sectors, i * SectorSize, SectorSize, result, i * SectorSize, sectorId++);

            return result;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ValidateSeek(offset, origin);

            var result = _stream.Seek(offset + _offset, origin);

            UpdateSeekable();

            return result;
        }

        private void ValidateSeek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
                throw new NotSupportedException("Seek is not supported.");
            if (_fixedLength)
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        if (_offset + offset >= Length)
                            throw new InvalidDataException("Position can't be set outside set stream length.");
                        break;
                    case SeekOrigin.Current:
                        if (_offset + Position + offset >= Length)
                            throw new InvalidDataException("Position can't be set outside set stream length.");
                        break;
                    case SeekOrigin.End:
                        if (Length + offset >= Length)
                            throw new InvalidDataException("Position can't be set outside set stream length.");
                        break;
                }
        }

        private void UpdateSeekable()
        {
            _sectorsBetweenLengthPosition = GetSectorsBetween(Position);
            _sectorPosition = Position / SectorSize * SectorSize;
            _bytesIntoSector = Position % SectorSize;
        }

        private long GetSectorsBetween(long position)
        {
            if (Length <= 0)
                return GetCurrentSector(position);

            var offsetBlock = GetCurrentSector(position);
            var lengthBlock = GetCurrentSector(Length);

            if (offsetBlock == lengthBlock)
                return 0;

            var lengthRest = Length % SectorSize;
            if (lengthRest == 0 && position > Length)
                return Math.Max(offsetBlock, lengthBlock) - Math.Min(offsetBlock, lengthBlock);

            return Math.Max(offsetBlock, lengthBlock) - (Math.Min(offsetBlock, lengthBlock) + 1);
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateWrite(buffer, offset, count);

            if (_fixedLength && Position + count >= Length)
                count = (int)(Length - Position);
            if (count == 0) return;
            var readBuffer = GetInitializedReadBuffer(count, out var dataStart);

            PeakOverlappingData(readBuffer, (int)dataStart, count);

            Array.Copy(buffer, offset, readBuffer, dataStart, count);

            var originalPosition = Position;
            Position -= dataStart;

            var encBuffer = EncryptSectors(readBuffer, (ulong)GetCurrentSector(Position));
            _stream.Write(encBuffer, 0, encBuffer.Length);

            if (originalPosition + count > _length)
                _length = originalPosition + count;

            Seek(originalPosition + count, SeekOrigin.Begin);
        }

        private void ValidateWrite(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException("Write is not supported");
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException("Offset or count can't be negative.");
            if (offset + count > buffer.Length)
                throw new InvalidDataException("Buffer too short.");
        }

        private byte[] GetInitializedReadBuffer(int count, out long dataStart)
        {
            dataStart = _bytesIntoSector;

            var bufferSectors = GetSectorCount(_bytesIntoSector + count);
            if (Position >= Length)
            {
                bufferSectors += _sectorsBetweenLengthPosition;
                dataStart += _sectorsBetweenLengthPosition * BlockSizeBytes;
            }

            var bufferLength = bufferSectors * SectorSize;

            return new byte[bufferLength];
        }

        private void PeakOverlappingData(byte[] buffer, int offset, int count)
        {
            if (Position - offset < Length)
            {
                long originalPosition = Position;
                var readBuffer = Decrypt(Position - offset, (int)GetSectorCount(Math.Min(Length - (Position - offset), count)) * SectorSize);
                Array.Copy(readBuffer, 0, buffer, 0, readBuffer.Length);
                Position = originalPosition;
            }
        }

        private byte[] EncryptSectors(byte[] sectors, ulong sectorId)
        {
            if (sectors.Length % SectorSize != 0)
                throw new InvalidDataException($"{nameof(EncryptSectors)} needs sector padded amount of data.");

            byte[] result = new byte[sectors.Length];
            for (int i = 0; i < GetSectorCount(sectors.Length); i++)
                _xts.CreateEncryptor().TransformBlock(sectors, i * SectorSize, SectorSize, result, i * SectorSize, sectorId++);

            return result;
        }
    }
}
