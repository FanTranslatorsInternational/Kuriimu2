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
 * the header contains of 3 MACs, each validating some part of the file or containing important information
 * the MAC at 0x80 validates range 0x00-0x80 with the fkey; the fkey is obtainable by MAC_80 itself; 
 *          the fkey is one of the 2 preset dnas keys (chosen by the initially given pgd_flags)
 * the MAC at 0x70 validates range 0x00-0x70 with the vkey; the vkey is obtainable by MAC_70 itself
 */

namespace Kryptography.Sony
{
    /// <summary>
    /// CryptoStream for PGD files found on Sony consoles. Only supports LE!
    /// </summary>
    public class PgdCryptoStream : KryptoStream
    {
        //private byte[] _dnas_key1A90 = { 0xED, 0xE2, 0x5D, 0x2D, 0xBB, 0xF8, 0x12, 0xE5, 0x3C, 0x5C, 0x59, 0x32, 0xFA, 0xE3, 0xE2, 0x43 };
        //private byte[] _dnas_key1AA0 = { 0x27, 0x74, 0xFB, 0xEB, 0xA4, 0xA0, 0x01, 0xD7, 0x02, 0x56, 0x9E, 0x33, 0x8C, 0x19, 0x57, 0x83 };

        private int _pgd_flag;
        private int _cipher_type;
        private byte[] _vkey;

        private ICryptoTransform _encryptor;
        private ICryptoTransform _decryptor;

        private Stream _stream;
        private long _offset;
        private long _length;
        private bool _fixedLength;

        public PgdCryptoStream(byte[] input) : this(new MemoryStream(input))
        {
        }

        public PgdCryptoStream(Stream input)
        {
            _stream = input;
            _length = input.Length;

            Initialize();
        }

        public PgdCryptoStream(byte[] input, long offset, long length) : this(new MemoryStream(input), offset, length)
        {
        }

        public PgdCryptoStream(Stream input, long offset, long length)
        {
            _stream = input;
            _stream.Position = Math.Max(offset, input.Position);
            _offset = offset;
            _length = length;
            _fixedLength = true;

            Initialize();
        }

        private void Initialize()
        {
            var bkPos = Position;

            using (var br = new BinaryReader(_stream, Encoding.ASCII, true))
            {
                Position = 3;
                var version = br.ReadByte();
                _pgd_flag = version == 0x40 ? 1 : version == 0x44 ? 2 : throw new InvalidDataException($"Invalid PGD version: 0x{version:X2}");

                //Set key index
                Position = 4;
                var key_index = br.ReadInt32();

                //Set types and definitions
                Position = 8;
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
                    _cipher_type = 1;
                }
                else
                {
                    mac_type = 2;
                    _cipher_type = 2;
                }

                //Get vkey for future decryption from MAC_70
                Position = 0;
                var bbmac = new BBMac(mac_type);
                _vkey = bbmac.GetKey(br.ReadBytes(0x70), br.ReadBytes(0x10));

                var context = new BBCipherContext(_vkey);
                _encryptor = context.CreateEncryptor();
                _decryptor = context.CreateDecryptor();
            }

            Position = bkPos;
        }

        public override int BlockSize => 128;

        public override int BlockSizeBytes => 16;

        public override List<byte[]> Keys { get; }

        public override int KeySize => 16;

        public override byte[] IV => throw new NotImplementedException();

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _length;

        public override long Position { get => _stream.Position - _offset; set => Seek(value, SeekOrigin.Begin); }

        private long TotalBlocks => GetBlockCount(Length);

        private long GetBlockCount(long input) => (long)Math.Ceiling((double)input / BlockSizeBytes);

        private long GetCurrentBlock(long input) => input / BlockSizeBytes;

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateRead(buffer, offset, count);

            if (_fixedLength && Position >= Length)
                return 0;

            ReadDecrypted(buffer, offset, count);

            return 0;
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
            if (_fixedLength && Position >= Length)
                return 0;

            var blockPosition = Position / BlockSizeBytes * BlockSizeBytes;
            var bytesIntoBlock = Position % BlockSizeBytes;

            var originalPosition = Position;

            count = (int)Math.Min(count, Length - Position);
            var alignedCount = (int)GetBlockCount(bytesIntoBlock + count) * BlockSizeBytes;

            if (alignedCount == 0) return alignedCount;

            var decData = Decrypt(blockPosition, alignedCount);

            Array.Copy(decData, bytesIntoBlock, buffer, offset, count);

            Seek(originalPosition + count, SeekOrigin.Begin);

            return count;
        }

        private byte[] Decrypt(long begin, int alignedCount)
        {
            _stream.Position = begin + _offset;
            var readData = new byte[alignedCount];
            _stream.Read(readData, 0, alignedCount);

            var decData = new byte[alignedCount];
            _decryptor.TransformBlock(readData, 0, readData.Length, decData, 0);

            return decData;
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
            throw new NotImplementedException();
        }
    }
}
