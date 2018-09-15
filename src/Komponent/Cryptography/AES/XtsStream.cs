using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Komponent.Cryptography.AES.XTS;

namespace Komponent.Cryptography.AES
{
    public class XtsStream : KryptoStream
    {
        private InternalXtsStream _xtsStream;

        public XtsStream(Stream input, byte[] key1, bool nintendo_tweak = false) : this(input, key1, null, 512, nintendo_tweak) { }

        public XtsStream(Stream input, byte[] key1, int sectorSize, bool nintendo_tweak = false) : this(input, key1, null, sectorSize, nintendo_tweak) { }

        public XtsStream(Stream input, byte[] key1, byte[] key2, bool nintendo_tweak = false) : this(input, key1, key2, 512, nintendo_tweak) { }

        public XtsStream(Stream input, byte[] key1, byte[] key2, int sectorSize, bool nintendo_tweak = false)
        {
            SectorSize = sectorSize;

            Keys = new List<byte[]>();
            Keys.Add(key1);
            if (key2 != null)
                Keys.Add(key2);

            if (key2 != null)
            {
                if (key1.Length == 128 / 8 && key2.Length == 128 / 8)
                {
                    _xtsStream = new InternalXtsStream(input, XtsAes128.Create(key1, key2, nintendo_tweak), sectorSize);
                }
                else if (key1.Length == 256 / 8 && key2.Length == 256 / 8)
                {
                    _xtsStream = new InternalXtsStream(input, XtsAes256.Create(key1, key2, nintendo_tweak), sectorSize);
                }
                else
                    throw new InvalidDataException("Key1 or Key2 have invalid size.");
            }
            else
            {
                if (key1.Length == 256 / 8)
                {
                    _xtsStream = new InternalXtsStream(input, XtsAes128.Create(key1, nintendo_tweak), sectorSize);
                }
                else if (key1.Length == 512 / 8)
                {
                    _xtsStream = new InternalXtsStream(input, XtsAes256.Create(key1, nintendo_tweak), sectorSize);
                }
                else
                    throw new InvalidDataException("Key1 has invalid size.");
            }
        }

        public override int BlockSize => 128;

        public override int BlockSizeBytes => 16;

        public int SectorSize { get; }

        public override List<byte[]> Keys { get; }

        public override int KeySize => Keys?[0]?.Length ?? 0;

        public override byte[] IV => throw new NotImplementedException();

        public override bool CanRead => _xtsStream.CanRead;

        public override bool CanSeek => _xtsStream.CanSeek;

        public override bool CanWrite => _xtsStream.CanWrite;

        public override long Length => _xtsStream.Length;

        public override long Position { get => _xtsStream.Position; set => Seek(value, SeekOrigin.Begin); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _xtsStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _xtsStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _xtsStream.Write(buffer, offset, count);
            _xtsStream.Flush();
        }
    }
}
