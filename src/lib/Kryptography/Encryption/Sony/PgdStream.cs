using System.Text;
using Komponent.Streams;

/* MAC validation has to happen outside of the stream
 * Stream only de-/encrypts 0x30-0x60 and 0x90-FileSize, while the rest is read without crypto
 * The stream assumes a PGD file and doesn't do any additional validation (is magic/version correct; are MACs valid);
 * The stream only throws on initialization if the given version or crypto types are unknown/out of range (kinda the only validation that's happening)
 *
 * Crypto explanation:
 * the header consists of 3 MACs, each validating some part of the file or containing important information
 * the MAC at 0x80 validates range 0x00-0x80 with the fkey; the fkey is obtainable by MAC_80 itself;
 *          the fkey is one of the 2 preset dnas keys (chosen by the initially given pgd_flags)
 * the MAC at 0x70 validates range 0x00-0x70 with the vkey; the vkey is obtainable by MAC_70 itself
 */

namespace Kryptography.Encryption.Sony
{
    /// <summary>
    /// CryptoStream for PGD files found on Sony consoles. Only supports LE!
    /// </summary>
    public class PgdStream : Stream
    {
        //private byte[] _dnas_key1A90 = { 0xED, 0xE2, 0x5D, 0x2D, 0xBB, 0xF8, 0x12, 0xE5, 0x3C, 0x5C, 0x59, 0x32, 0xFA, 0xE3, 0xE2, 0x43 };
        //private byte[] _dnas_key1AA0 = { 0x27, 0x74, 0xFB, 0xEB, 0xA4, 0xA0, 0x01, 0xD7, 0x02, 0x56, 0x9E, 0x33, 0x8C, 0x19, 0x57, 0x83 };

        private int _pgdFlag;
        //private int _cipher_type;
        //private byte[] _vkey;

        private readonly Stream _baseStream;

        private Stream? _headerBaseStream;
        private KryptoStream? _headerStream;
        private Stream? _bodyBaseStream;
        private KryptoStream? _bodyStream;

        public byte[] VersionKey { get; private set; } = [];
        public byte[] HeaderKey { get; private set; } = [];
        public byte[] BodyKey { get; private set; } = [];
        public int CipherType { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _baseStream.Length;

        public override long Position { get; set; }

        public PgdStream(byte[] input, byte[]? vkey, byte[]? headerKey, byte[]? bodyKey, int cipherType)
            : this(new MemoryStream(input), vkey, headerKey, bodyKey, cipherType)
        {
        }

        public PgdStream(Stream input, byte[]? vkey, byte[]? headerKey, byte[]? bodyKey, int cipherType)
        {
            _baseStream = input;

            if (vkey != null && headerKey != null && bodyKey != null && cipherType != -1)
            {
                InitializeWithPresetKeys(vkey, headerKey, bodyKey, cipherType);
            }
            else
            {
                Initialize();
            }
        }

        public PgdStream(byte[] input, long offset, long length, byte[]? vkey, byte[]? headerKey, byte[]? bodyKey, int cipherType)
            : this(new MemoryStream(input), offset, length, vkey, headerKey, bodyKey, cipherType)
        {
        }

        public PgdStream(Stream input, long offset, long length, byte[]? vkey, byte[]? headerKey, byte[]? bodyKey, int cipherType)
        {
            _baseStream = new SubStream(input, offset, length)
            {
                Position = Math.Min(Math.Max(input.Position, offset) - offset, length)
            };

            if (vkey != null && headerKey != null && bodyKey != null && cipherType != -1)
            {
                InitializeWithPresetKeys(vkey, headerKey, bodyKey, cipherType);
            }
            else
            {
                Initialize();
            }
        }

        private void InitializeWithPresetKeys(byte[] vkey, byte[] headerKey, byte[] bodyKey, int cipherType)
        {
            VersionKey = vkey;
            HeaderKey = headerKey;
            BodyKey = bodyKey;
            CipherType = cipherType;

            _headerBaseStream = new MemoryStream();
            _bodyBaseStream = new MemoryStream();

            _headerStream = new BbCipherStream(_headerBaseStream, HeaderKey, VersionKey, 0, cipherType);
            _bodyStream = new BbCipherStream(_bodyBaseStream, BodyKey, VersionKey, 0, cipherType);
        }

        private void Initialize()
        {
            using var br = new BinaryReader(_baseStream, Encoding.ASCII, true);

            br.BaseStream.Position = 3;
            var version = br.ReadByte();
            _pgdFlag = version switch
            {
                0x40 => 1,
                0x44 => 2,
                _ => throw new InvalidDataException($"Invalid PGD version: 0x{version:X2}")
            };

            //Set key index
            br.BaseStream.Position = 4;
            var keyIndex = br.ReadInt32();

            //Set types and definitions
            br.BaseStream.Position = 8;
            var drmType = br.ReadInt32();

            int macType;
            if (drmType == 1)
            {
                macType = 1;
                _pgdFlag |= 4;
                if (keyIndex > 1)
                {
                    macType = 3;
                    _pgdFlag |= 8;
                }
                CipherType = 1;
            }
            else
            {
                macType = 2;
                CipherType = 2;
            }

            GetVersionKey(macType);

            GetHeaderKey();
            _headerStream = new BbCipherStream(_baseStream, 0x30, 0x30, HeaderKey, VersionKey, 0, CipherType);

            GetBodyKey();
            _bodyStream = new BbCipherStream(_baseStream, 0x90, _baseStream.Length - 0x90, BodyKey, VersionKey, 0, CipherType);
        }

        private void GetVersionKey(int macType)
        {
            _baseStream.Position = 0;

            var dataRange = new byte[0x70];
            var mac = new byte[0x10];
            _ = _baseStream.Read(dataRange, 0, 0x70);
            _ = _baseStream.Read(mac, 0, 0x10);

            var bbmac = new BbMac(macType);
            VersionKey = bbmac.GetKey(dataRange, mac) ?? throw new InvalidOperationException("Could not determine BBMAC key.");
        }

        private void GetHeaderKey()
        {
            HeaderKey = new byte[0x10];

            _baseStream.Position = 0x10;
            _ = _baseStream.Read(HeaderKey, 0, 0x10);
        }

        private void GetBodyKey()
        {
            ArgumentNullException.ThrowIfNull(_headerStream);

            BodyKey = new byte[0x10];

            _headerStream.Position = 0;
            _ = _headerStream.Read(BodyKey, 0, 0x10);
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = 0;

            //Unencrypted header from 0 to 0x30; Contains headerkey and magic
            if (Position < 0x30)
            {
                _baseStream.Position = Position;

                var size = Math.Min(count, 0x30 - (int)Position);
                read += _baseStream.Read(buffer, offset, size);

                Position += size;
            }

            //Encrypted header from 0x30 to 0x60; Contains bodykey and meta information like size
            //Gets decrypted with BBCipher, headerkey and vkey
            if (Position < 0x60 && read < count)
            {
                ArgumentNullException.ThrowIfNull(_headerStream);

                _headerStream.Position = Position - 0x30;

                var size = Math.Min(count - read, 0x60 - (int)Position);
                read += _headerStream.Read(buffer, read + offset, size);

                Position += size;
            }

            //Unencrypted MAC data from 0x60 to 0x90; Contains 3 MACs, each validating data portion from 0 to {position of MAC}
            //One MAC is 0x10 bytes long
            if (Position < 0x90 && read < count)
            {
                _baseStream.Position = Position;

                var size = Math.Min(count - read, 0x90 - (int)Position);
                read += _baseStream.Read(buffer, read + offset, size);

                Position += size;
            }

            //Encrypted file data from 0x90 to file end; Contains all file data
            //Gets decrypted with BBCipher, bodykey and vkey
            if (read < count)
            {
                ArgumentNullException.ThrowIfNull(_bodyStream);

                _bodyStream.Position = Position - 0x90;

                var size = Math.Min(count - read, _bodyStream.Length - Position);
                read += _bodyStream.Read(buffer, read + offset, (int)size);

                Position += size;
            }

            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var write = 0;

            if (Position < 0x30)
            {
                _baseStream.Position = Position;

                var size = Math.Min(count, 0x30 - (int)Position);
                _baseStream.Write(buffer, offset, size);

                Position += size;
                write += size;
            }

            if (Position < 0x60 && write < count)
            {
                ArgumentNullException.ThrowIfNull(_headerStream);

                _headerStream.Position = Position - 0x30;

                var size = Math.Min(count - write, 0x60 - (int)Position);
                _headerStream.Write(buffer, write + offset, size);

                Position += size;
                write += size;
            }

            if (Position < 0x90 && write < count)
            {
                _baseStream.Position = Position;

                var size = Math.Min(count - write, 0x90 - (int)Position);
                _baseStream.Write(buffer, write + offset, size);

                Position += size;
                write += size;
            }

            if (write < count)
            {
                ArgumentNullException.ThrowIfNull(_bodyStream);

                _bodyStream.Position = Position - 0x90;

                var size = Math.Min(count - write, _baseStream.Length - Position);
                _bodyStream.Write(buffer, write + offset, (int)size);

                Position += size;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return origin switch
            {
                SeekOrigin.Begin => Position = offset,
                SeekOrigin.Current => Position += offset,
                SeekOrigin.End => Position = Length + offset,
                _ => throw new ArgumentException(origin.ToString())
            };
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
    }
}