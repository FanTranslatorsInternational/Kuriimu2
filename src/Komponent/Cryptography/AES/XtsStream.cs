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

        public int CurrentSector => CalculateCurrentSector((int)Position);

        public override int BlockSize => 128;

        public override int BlockSizeBytes => BlockSize / 8;

        public override List<byte[]> Keys { get; }

        public override int KeySize => Keys?[0]?.Length ?? 0;

        public override byte[] IV => throw new NotImplementedException();

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        private long _offset = 0;
        private long _length = 0;
        public override long Length => _length;

        public override long Position { get => _stream.Position - _offset; set => Seek(value, SeekOrigin.Begin); }

        public int TotalSectors { get => CalculateSectorCount((int)Length); }
        private int CalculateSectorCount(int input) => (int)Math.Ceiling((double)input / SectorSize);
        private int CalculateCurrentSector(int input) => (int)Math.Floor((double)input / SectorSize);

        private Stream _stream;
        private Xts _xtsContext;
        private byte[] _finalSector;

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
            _finalSector = new byte[sectorSize];
            _length = length;
            _offset = offset;

            SectorSize = sectorSize;

            Keys = new List<byte[]>();
            Keys.Add(key1);
            if (key2 != null)
                Keys.Add(key2);

            if (key2 != null)
            {
                if (key1.Length == 128 / 8 && key2.Length == 128 / 8)
                {
                    _xtsContext = XtsAes128.Create(key1, key2, nintendo_tweak);
                }
                else if (key1.Length == 256 / 8 && key2.Length == 256 / 8)
                {
                    _xtsContext = XtsAes256.Create(key1, key2, nintendo_tweak);
                }
                else
                    throw new InvalidDataException("Key1 or Key2 have invalid size.");
            }
            else
            {
                if (key1.Length == 256 / 8)
                {
                    _xtsContext = XtsAes128.Create(key1, nintendo_tweak);
                }
                else if (key1.Length == 512 / 8)
                {
                    _xtsContext = XtsAes256.Create(key1, nintendo_tweak);
                }
                else
                    throw new InvalidDataException("Key1 has invalid size.");
            }

            Position = input.Position;
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
            _finalSector = new byte[sectorSize];
            _length = _stream.Length;

            SectorSize = sectorSize;

            Keys = new List<byte[]>();
            Keys.Add(key1);
            if (key2 != null)
                Keys.Add(key2);

            if (key2 != null)
            {
                if (key1.Length == 128 / 8 && key2.Length == 128 / 8)
                {
                    _xtsContext = XtsAes128.Create(key1, key2, nintendo_tweak);
                }
                else if (key1.Length == 256 / 8 && key2.Length == 256 / 8)
                {
                    _xtsContext = XtsAes256.Create(key1, key2, nintendo_tweak);
                }
                else
                    throw new InvalidDataException("Key1 or Key2 have invalid size.");
            }
            else
            {
                if (key1.Length == 256 / 8)
                {
                    _xtsContext = XtsAes128.Create(key1, nintendo_tweak);
                }
                else if (key1.Length == 512 / 8)
                {
                    _xtsContext = XtsAes256.Create(key1, nintendo_tweak);
                }
                else
                    throw new InvalidDataException("Key1 has invalid size.");
            }

            Position = input.Position;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateInput(buffer, offset, count);

            var decrypted = ReadDecrypted(count);

            long offsetIntoSector = 0;
            if (Position % SectorSize > 0)
                offsetIntoSector = Position % SectorSize;
            Array.Copy(decrypted.Skip((int)offsetIntoSector).Take(count).ToArray(), 0, buffer, offset, count);
            Position += count;

            return count;
        }

        private void ValidateInput(byte[] buffer, int offset, int count)
        {
            if (offset < 0 || count < 0)
                throw new InvalidDataException("Offset and count can't be negative.");
            if (offset + count > buffer.Length)
                throw new InvalidDataException("Buffer too short.");
            if (count > Length - Position)
                throw new InvalidDataException($"Can't read {count} bytes from position {Position} in stream with length {Length}.");
        }

        private byte[] ReadDecrypted(int count)
        {
            long offsetIntoSector = 0;
            if (Position % SectorSize > 0)
                offsetIntoSector = Position % SectorSize;

            var sectorsToRead = CalculateSectorCount((int)offsetIntoSector + count);
            var sectorPaddedCount = sectorsToRead * SectorSize;

            if (Length <= 0 || count <= 0)
                return new byte[sectorPaddedCount];

            var originalPosition = Position;
            Position -= offsetIntoSector;

            var minimalDecryptableSize = Math.Min(CalculateSectorCount((int)Length) * SectorSize, sectorPaddedCount);
            var bytesRead = new byte[minimalDecryptableSize];
            _stream.Read(bytesRead, 0, minimalDecryptableSize);
            Position = originalPosition;

            if (CalculateSectorCount((int)Position + count) >= TotalSectors)
                Array.Copy(_finalSector, 0, bytesRead, bytesRead.Length - BlockSizeBytes, _finalSector.Length);

            var decrypted = DecryptSectors(bytesRead, (ulong)CurrentSector);
            var result = new byte[sectorPaddedCount];
            Array.Copy(decrypted, 0, result, 0, minimalDecryptableSize);

            return result;
        }

        private byte[] DecryptSectors(byte[] sectors, ulong sectorId)
        {
            if (sectors.Length % SectorSize != 0)
                throw new InvalidDataException($"{nameof(DecryptSectors)} needs sector padded amount of data.");

            byte[] result = new byte[sectors.Length];
            for (int i = 0; i < CalculateSectorCount(sectors.Length); i++)
                _xtsContext.CreateDecryptor().TransformBlock(sectors, i * SectorSize, SectorSize, result, i * SectorSize, sectorId++);

            return result;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset + _offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            long offsetIntoSector = 0;
            if (Position % SectorSize > 0)
                offsetIntoSector = Position % SectorSize;

            var sectorToWrite = CalculateSectorCount((int)offsetIntoSector + count);
            var sectorPaddedCount = sectorToWrite * SectorSize;

            byte[] decrypted = ReadDecrypted(count);
            Array.Copy(buffer, offset, decrypted, offsetIntoSector, count);

            if (CalculateSectorCount((int)Length) < CalculateSectorCount((int)Position) - 1)
            {
                var betweenSectors = CalculateSectorCount((int)Position) - CalculateSectorCount((int)Length) - 1;
                var newDecrypted = new byte[betweenSectors * SectorSize + decrypted.Length];
                Array.Copy(decrypted, 0, newDecrypted, betweenSectors * SectorSize, decrypted.Length);
                decrypted = newDecrypted;
            }

            var originalPosition = Position;
            if (CalculateSectorCount((int)Length) < CalculateSectorCount((int)Position) - 1)
                Position -= (CalculateSectorCount((int)Position) - CalculateSectorCount((int)Length) - 1) * SectorSize;
            Position -= offsetIntoSector;

            var encrypted = EncryptSectors(decrypted, (ulong)CalculateCurrentSector((int)Position));

            if (CalculateSectorCount((int)originalPosition + count) >= TotalSectors)
                Array.Copy(encrypted.Skip(encrypted.Length - SectorSize).Take(SectorSize).ToArray(), _finalSector, SectorSize);

            _stream.Write(encrypted, 0, encrypted.Length);

            _length = Math.Max(_length, Position + count);
            Position = originalPosition + count;
        }

        private byte[] EncryptSectors(byte[] sectors, ulong sectorId)
        {
            if (sectors.Length % SectorSize != 0)
                throw new InvalidDataException($"{nameof(EncryptSectors)} needs sector padded amount of data.");

            byte[] result = new byte[sectors.Length];
            for (int i = 0; i < CalculateSectorCount(sectors.Length); i++)
                _xtsContext.CreateEncryptor().TransformBlock(sectors, i * SectorSize, SectorSize, result, i * SectorSize, sectorId++);

            return result;
        }
    }
}
