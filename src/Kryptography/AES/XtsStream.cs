using Kryptography.AES.XTS;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kryptography.AES
{
    public class XtsStream : KryptoStream
    {
        public int SectorSize { get; }

        private Stream _stream;
        private XtsCryptoTransform _encryptor;
        private XtsCryptoTransform _decryptor;

        private long _offset;
        private long _length;
        private bool _fixedLength = false;
        private bool _littleEndianId;

        public override int BlockSize => 128;
        public override int BlockSizeBytes => 16;
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;

        public override List<byte[]> Keys { get; }
        public override int KeySize => Keys?[0]?.Length ?? 0;
        public override byte[] IV { get; }
        public override long Length => _length;
        public override long Position { get => _stream.Position - _offset; set => Seek(value, SeekOrigin.Begin); }
        private long TotalSectors => GetSectorCount(Length);

        private long GetSectorCount(long input) => (long)Math.Ceiling((double)input / SectorSize);

        private long GetCurrentSector(long input) => input / SectorSize;

        private byte[] GetCurrentSectorId(long input)
        {
            var id = new byte[16];
            Array.Copy(IV, id, 16);

            Increment(id, (int)GetCurrentSector(input));

            return id;
        }

        private void Increment(byte[] ctr, int count)
        {
            for (int i = ctr.Length - 1; i >= 0; i--)
            {
                if (count == 0)
                    break;

                var check = ctr[i];
                ctr[i] += (byte)count;
                count >>= 8;

                int off = 0;
                while (i - off - 1 >= 0 && ctr[i - off] < check)
                {
                    check = ctr[i - off - 1];
                    ctr[i - off - 1]++;
                    off++;
                }
            }
        }

        public XtsStream(byte[] input, long offset, long length, byte[] key, byte[] sectorId, bool littleEndianId = false) : this(new MemoryStream(input), offset, length, key, 512, sectorId, littleEndianId)
        {
        }

        public XtsStream(byte[] input, long offset, long length, byte[] key, int sectorSize, byte[] sectorId, bool littleEndianId = false) : this(new MemoryStream(input), offset, length, key, sectorSize, sectorId, littleEndianId)
        {
        }

        public XtsStream(Stream input, long offset, long length, byte[] key, byte[] sectorId, bool littleEndianId = false) : this(input, offset, length, key, 512, sectorId, littleEndianId)
        {
        }

        public XtsStream(Stream input, long offset, long length, byte[] key, int sectorSize, byte[] sectorId, bool littleEndianId = false)
        {
            _stream = input;
            _offset = offset;
            _length = length;
            _fixedLength = true;

            SectorSize = sectorSize;
            IV = new byte[16];
            Array.Copy(sectorId, IV, 16);

            Keys = new List<byte[]>();
            Keys.Add(key);

            var xts = XtsContext.Create(key, sectorId, littleEndianId, sectorSize);

            _encryptor = (XtsCryptoTransform)xts.CreateEncryptor();
            _decryptor = (XtsCryptoTransform)xts.CreateDecryptor();
        }

        new public void Dispose()
        {
            _stream.Dispose();
            _encryptor.Dispose();
            _decryptor.Dispose();
        }

        public override void Flush()
        {
            throw new NotImplementedException();
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
            var sectorPosition = Position / SectorSize * SectorSize;
            var bytesIntoSector = Position % SectorSize;

            var originalPosition = Position;

            count = (int)Math.Min(count, Length - Position);
            var alignedCount = (int)GetSectorCount(bytesIntoSector + count) * SectorSize;

            if (alignedCount == 0) return alignedCount;

            var decData = Decrypt(sectorPosition, alignedCount);

            Array.Copy(decData, bytesIntoSector, buffer, offset, count);

            Seek(originalPosition + count, SeekOrigin.Begin);

            return count;
        }

        private byte[] Decrypt(long begin, int alignedCount)
        {
            _stream.Position = begin + _offset;
            var readData = new byte[alignedCount];
            _stream.Read(readData, 0, alignedCount);

            var decData = DecryptSectors(readData, GetCurrentSectorId(begin));

            return decData;
        }

        private byte[] DecryptSectors(byte[] sectors, byte[] sectorId)
        {
            if (sectors.Length % SectorSize != 0)
                throw new InvalidDataException($"{nameof(DecryptSectors)} needs sector padded amount of data.");

            byte[] result = new byte[sectors.Length];
            _decryptor.SectorId = sectorId;
            _decryptor.TransformBlock(sectors, 0, sectors.Length, result, 0);

            return result;
        }

        private byte[] NumericToArray(long value, bool le)
        {
            var res = new byte[0x10];

            if (le)
                for (int i = 15; i >= 8; i--)
                {
                    res[i] = (byte)(value & 0xFF);
                    value >>= 8;
                }
            else
                for (int i = 0; i < 8; i++)
                {
                    res[i] = (byte)(value & 0xFF);
                    value >>= 8;
                }

            return res;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ValidateSeek(offset, origin);

            return _stream.Seek(offset + _offset, origin);
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

            var encBuffer = EncryptSectors(readBuffer, GetCurrentSectorId(Position));

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
            var sectorsBetweenLengthPosition = GetSectorsBetween(Position);
            var bytesIntoSector = Position % SectorSize;

            dataStart = bytesIntoSector;

            var bufferSectors = GetSectorCount(bytesIntoSector + count);
            if (Position >= Length)
            {
                bufferSectors += sectorsBetweenLengthPosition;
                dataStart += sectorsBetweenLengthPosition * BlockSizeBytes;
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

        private byte[] EncryptSectors(byte[] sectors, byte[] sectorId)
        {
            if (sectors.Length % SectorSize != 0)
                throw new InvalidDataException($"{nameof(EncryptSectors)} needs sector padded amount of data.");

            byte[] result = new byte[sectors.Length];
            _encryptor.SectorId = sectorId;
            _encryptor.TransformBlock(sectors, 0, sectors.Length, result, 0);

            return result;
        }
    }
}