using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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

namespace Kryptography.Sony
{
    /// <summary>
    /// CryptoStream for PGD files found on Sony consoles. Only supports LE!
    /// </summary>
    public class PgdStream : Stream
    {
        //private byte[] _dnas_key1A90 = { 0xED, 0xE2, 0x5D, 0x2D, 0xBB, 0xF8, 0x12, 0xE5, 0x3C, 0x5C, 0x59, 0x32, 0xFA, 0xE3, 0xE2, 0x43 };
        //private byte[] _dnas_key1AA0 = { 0x27, 0x74, 0xFB, 0xEB, 0xA4, 0xA0, 0x01, 0xD7, 0x02, 0x56, 0x9E, 0x33, 0x8C, 0x19, 0x57, 0x83 };

        private int _pgd_flag;
        //private int _cipher_type;
        //private byte[] _vkey;

        private Stream _headerBaseStream;
        private KryptoStream _headerStream;
        private Stream _bodyBaseStream;
        private KryptoStream _bodyStream;
        private Stream _baseStream;

        public PgdStream(byte[] input, byte[] vkey, byte[] header_key, byte[] body_key, int cipher_type) : this(new MemoryStream(input), vkey, header_key, body_key, cipher_type)
        {
        }

        public PgdStream(Stream input, byte[] vkey, byte[] header_key, byte[] body_key, int cipher_type)
        {
            _baseStream = input;

            if (vkey != null && header_key != null && body_key != null && cipher_type != -1)
            {
                InitializeWithPresetKeys(vkey, header_key, body_key, cipher_type);
            }
            else
            {
                Initialize();
            }
        }

        public PgdStream(byte[] input, long offset, long length, byte[] vkey, byte[] header_key, byte[] body_key, int cipher_type) : this(new MemoryStream(input), offset, length, vkey, header_key, body_key, cipher_type)
        {
        }

        public PgdStream(Stream input, long offset, long length, byte[] vkey, byte[] header_key, byte[] body_key, int cipher_type)
        {
            _baseStream = new SubStream(input, offset, length);

            if (vkey != null && header_key != null && body_key != null && cipher_type != -1)
            {
                InitializeWithPresetKeys(vkey, header_key, body_key, cipher_type);
            }
            else
            {
                Initialize();
            }
        }

        private void InitializeWithPresetKeys(byte[] vkey, byte[] header_key, byte[] body_key, int cipher_type)
        {
            VersionKey = vkey;
            HeaderKey = header_key;
            BodyKey = body_key;
            CipherType = cipher_type;

            _headerBaseStream = new MemoryStream();
            _bodyBaseStream = new MemoryStream();

            _headerStream = new BBCipherStream(_headerBaseStream, HeaderKey, VersionKey, 0, cipher_type);
            _bodyStream = new BBCipherStream(_bodyBaseStream, BodyKey, VersionKey, 0, cipher_type);
        }

        private void Initialize()
        {
            using (var br = new BinaryReader(_baseStream, Encoding.ASCII, true))
            {
                br.BaseStream.Position = 3;
                var version = br.ReadByte();
                _pgd_flag = version == 0x40 ? 1 : version == 0x44 ? 2 : throw new InvalidDataException($"Invalid PGD version: 0x{version:X2}");

                //Set key index
                br.BaseStream.Position = 4;
                var key_index = br.ReadInt32();

                //Set types and definitions
                br.BaseStream.Position = 8;
                var drm_type = br.ReadInt32();

                int mac_type = 0;
                if (drm_type == 1)
                {
                    mac_type = 1;
                    _pgd_flag |= 4;
                    if (key_index > 1)
                    {
                        mac_type = 3;
                        _pgd_flag |= 8;
                    }
                    CipherType = 1;
                }
                else
                {
                    mac_type = 2;
                    CipherType = 2;
                }

                GetVersionKey(mac_type);

                GetHeaderKey();
                _headerStream = new BBCipherStream(_baseStream, 0x30, 0x30, HeaderKey, VersionKey, 0, CipherType);

                GetBodyKey();
                _bodyStream = new BBCipherStream(_baseStream, 0x90, _baseStream.Length - 0x90, BodyKey, VersionKey, 0, CipherType);
            }
        }

        private void GetVersionKey(int mac_type)
        {
            _baseStream.Position = 0;

            var dataRange = new byte[0x70];
            var mac = new byte[0x10];
            _baseStream.Read(dataRange, 0, 0x70);
            _baseStream.Read(mac, 0, 0x10);

            var bbmac = new BBMac(mac_type);
            VersionKey = bbmac.GetKey(dataRange, mac);
        }

        private void GetHeaderKey()
        {
            HeaderKey = new byte[0x10];

            _baseStream.Position = 0x10;
            _baseStream.Read(HeaderKey, 0, 0x10);
        }

        private void GetBodyKey()
        {
            BodyKey = new byte[0x10];

            _headerStream.Position = 0;
            _headerStream.Read(BodyKey, 0, 0x10);
        }

        public byte[] VersionKey { get; private set; }
        public byte[] HeaderKey { get; private set; }
        public byte[] BodyKey { get; private set; }
        public int CipherType { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _baseStream.Length;

        public override long Position { get; set; }

        public override void Flush()
        {
            ;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = 0;

            if (Position < 0x30)
            {
                _baseStream.Position = Position;

                var size = Math.Min(count, 0x30 - (int)Position);
                read += _baseStream.Read(buffer, read, size);

                Position += size;
            }

            if (Position < 0x60 && read < count)
            {
                _headerStream.Position = Position - 0x30;

                var size = Math.Min(count - read, 0x60 - (int)Position);
                read += _headerStream.Read(buffer, read, size);

                Position += size;
            }

            if (Position < 0x90 && read < count)
            {
                _baseStream.Position = Position;

                var size = Math.Min(count - read, 0x90 - (int)Position);
                read += _baseStream.Read(buffer, read, size);

                Position += size;
            }

            if (read < count)
            {
                _bodyStream.Position = Position - 0x90;

                var size = Math.Min(count - read, _baseStream.Length - Position);
                read += _bodyStream.Read(buffer, read, (int)size);

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
                _baseStream.Write(buffer, write, size);

                Position += size;
                write += size;
            }

            if (Position < 0x60 && write < count)
            {
                _headerStream.Position = Position - 0x30;

                var size = Math.Min(count - write, 0x60 - (int)Position);
                _headerStream.Write(buffer, write, size);

                Position += size;
                write += size;
            }

            if (Position < 0x90 && write < count)
            {
                _baseStream.Position = Position;

                var size = Math.Min(count - write, 0x90 - (int)Position);
                _baseStream.Write(buffer, write, size);

                Position += size;
                write += size;
            }

            if (write < count)
            {
                _bodyStream.Position = Position - 0x90;

                var size = Math.Min(count - write, _baseStream.Length - Position);
                _bodyStream.Write(buffer, write, (int)size);

                Position += size;
                write += (int)size;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin: return Position = offset;
                case SeekOrigin.Current: return Position += offset;
                case SeekOrigin.End: return Position = Length + offset;
            }
            throw new ArgumentException(origin.ToString());
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
    }
}