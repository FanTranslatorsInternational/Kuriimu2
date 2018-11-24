//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Security.Cryptography;
//using System.Text;
//using System.Threading.Tasks;

///* MAC validation has to happen outside of the stream
// * Stream only de-/encrypts 0x30-0x60 and 0x90-FileSize, while the rest is read without crypto
// * The stream assumes a PGD file and doesn't do any additional validation (is magic/version correct; are MACs valid);
// * The stream only throws on initialization if the given version or crypto types are unknown/out of range (kinda the only validation that's happening)
// * 
// * Crypto explanation:
// * the header contains of 3 MACs, each validating some part of the file or containing important information
// * the MAC at 0x80 validates range 0x00-0x80 with the fkey; the fkey is obtainable by MAC_80 itself; 
// *          the fkey is one of the 2 preset dnas keys (chosen by the initially given pgd_flags)
// * the MAC at 0x70 validates range 0x00-0x70 with the vkey; the vkey is obtainable by MAC_70 itself
// */

//namespace Kryptography.Sony
//{
//    /// <summary>
//    /// CryptoStream for PGD files found on Sony consoles. Only supports LE!
//    /// </summary>
//    public class PgdCryptoStream : KryptoStream
//    {
//        //private byte[] _dnas_key1A90 = { 0xED, 0xE2, 0x5D, 0x2D, 0xBB, 0xF8, 0x12, 0xE5, 0x3C, 0x5C, 0x59, 0x32, 0xFA, 0xE3, 0xE2, 0x43 };
//        //private byte[] _dnas_key1AA0 = { 0x27, 0x74, 0xFB, 0xEB, 0xA4, 0xA0, 0x01, 0xD7, 0x02, 0x56, 0x9E, 0x33, 0x8C, 0x19, 0x57, 0x83 };

//        private int _pgd_flag;
//        private int _cipher_type;
//        private byte[] _vkey;

//        private BBCipherTransform _headDecryptor;
//        private BBCipherTransform _headEncryptor;

//        private BBCipherTransform _bodyDecryptor;
//        private BBCipherTransform _bodyEncryptor;

//        public PgdCryptoStream(byte[] input) : base(input)
//        {
//            Initialize();
//        }

//        public PgdCryptoStream(Stream input) : base(input)
//        {
//            Initialize();
//        }

//        public PgdCryptoStream(byte[] input, long offset, long length) : base(input, offset, length)
//        {
//            Initialize();
//        }

//        public PgdCryptoStream(Stream input, long offset, long length) : base(input, offset, length)
//        {
//            Initialize();
//        }

//        private void Initialize()
//        {
//            var bkPos = Position;

//            using (var br = new BinaryReader(_stream, Encoding.ASCII, true))
//            {
//                Position = 3;
//                var version = br.ReadByte();
//                _pgd_flag = version == 0x40 ? 1 : version == 0x44 ? 2 : throw new InvalidDataException($"Invalid PGD version: 0x{version:X2}");

//                //Set key index
//                Position = 4;
//                var key_index = br.ReadInt32();

//                //Set types and definitions
//                Position = 8;
//                var drm_type = br.ReadInt32();

//                int mac_type = 0;
//                if (drm_type == 1)
//                {
//                    mac_type = 1;
//                    _pgd_flag |= 4;
//                    if (key_index > 1)
//                    {
//                        mac_type = 3;
//                        _pgd_flag |= 8;
//                    }
//                    _cipher_type = 1;
//                }
//                else
//                {
//                    mac_type = 2;
//                    _cipher_type = 2;
//                }

//                SetVersionKey(br, mac_type);

//                byte[] headerKey = new byte[0x10];
//                GetHeaderKey(br, headerKey);

//                byte[] bodyKey = new byte[0x10];
//                GetBodyKey(br, headerKey, bodyKey);

//                var headContext = new BBCipherContext(headerKey, _vkey, 0, _cipher_type);

//                _headDecryptor = (BBCipherTransform)headContext.CreateDecryptor();
//                _headEncryptor = (BBCipherTransform)headContext.CreateEncryptor();

//                var bodyContext = new BBCipherContext(bodyKey, _vkey, 0, _cipher_type);

//                _bodyDecryptor = (BBCipherTransform)bodyContext.CreateDecryptor();
//                _bodyEncryptor = (BBCipherTransform)bodyContext.CreateEncryptor();
//            }

//            Position = bkPos;
//        }

//        private void SetVersionKey(BinaryReader br, int mac_type)
//        {
//            br.BaseStream.Position = 0;

//            var bbmac = new BBMac(mac_type);
//            _vkey = bbmac.GetKey(br.ReadBytes(0x70), br.ReadBytes(0x10));
//        }

//        private void GetHeaderKey(BinaryReader br, byte[] headerKey)
//        {
//            br.BaseStream.Position = 0x10;

//            Array.Copy(br.ReadBytes(0x10), headerKey, 0x10);
//        }

//        private void GetBodyKey(BinaryReader br, byte[] headerKey, byte[] bodyKey)
//        {
//            var context = new BBCipherContext(headerKey, _vkey, 0, _cipher_type);
//            var dec = context.CreateDecryptor();

//            br.BaseStream.Position = 0x30;
//            var decryptedBuffer = dec.TransformFinalBlock(br.ReadBytes(0x30), 0, 0x30);
//            Array.Copy(decryptedBuffer, bodyKey, 0x10);
//        }

//        public override int BlockSize => 128;

//        public override int BlockSizeBytes => 16;

//        protected override int BlockAlign => BlockSizeBytes;

//        public override List<byte[]> Keys { get; }

//        public override int KeySize => 16;

//        public override byte[] IV => throw new NotImplementedException();

//        public override bool CanRead => true;

//        public override bool CanSeek => true;

//        public override bool CanWrite => true;

//        public override long Length => _length;

//        public override long Position { get => _stream.Position - _offset; set => Seek(value, SeekOrigin.Begin); }

//        private long TotalBlocks => GetBlockCount(Length);

//        private long GetBlockCount(long input) => (long)Math.Ceiling((double)input / BlockSizeBytes);

//        private long GetCurrentBlock(long input) => input / BlockSizeBytes;

//        public override void Flush()
//        {
//            throw new NotImplementedException();
//        }

//        protected override void Decrypt(long alignedPosition, int alignedCount, byte[] decryptedData)
//        {
//            var absolutePos = alignedPosition + _offset;
//            _stream.Position = absolutePos;

//            var read = 0;
//            if (alignedPosition + read < 0x30)
//            {
//                var size = (int)Math.Min(0x30 - (alignedPosition + read), alignedCount - read);
//                _stream.Read(decryptedData, read, size);

//                if ((read += size) >= alignedCount)
//                    return;
//            }
//            if (alignedPosition + read < 0x60)
//            {
//                var size = (int)Math.Min(0x60 - (alignedPosition + read), alignedCount - read);

//                if (alignedPosition + read > 0x30)
//                {
//                    var diff = alignedPosition + read - 0x30;

//                    _stream.Position = _offset + 0x30;
//                    var tmp = new byte[0x30];
//                    _stream.Read(tmp, 0, 0x30);

//                    _headDecryptor.TransformBlock(tmp, 0, 0x30, tmp, 0);

//                    Array.Copy(tmp, diff, decryptedData, read, size);
//                    _stream.Position = absolutePos + diff + size;
//                }
//                else
//                {
//                    _stream.Read(decryptedData, read, size);
//                    _headDecryptor.TransformBlock(decryptedData, read, size, decryptedData, read);
//                }

//                if ((read += size) >= alignedCount)
//                    return;
//            }
//            if (alignedPosition + read < 0x90)
//            {
//                var size = (int)Math.Min(0x90 - (alignedPosition + read), alignedCount - read);
//                _stream.Read(decryptedData, read, size);

//                if ((read += size) >= alignedCount)
//                    return;
//            }

//            if ((alignedPosition + read - 0x90) % 0x800 > 0)
//            {
//                var diff = (alignedPosition + read - 0x90) % 0x800;
//                var size = (int)Math.Min(0x800 - diff, alignedCount - read);
//                _stream.Position -= diff;

//                var tmp = new byte[0x800];
//                _stream.Read(tmp, 0, 0x800);

//                _bodyDecryptor.Seed = (int)(alignedPosition + read - 0x90 - diff) / 0x10 + 1;

//                _bodyDecryptor.TransformBlock(tmp, 0, 0x800, tmp, 0);
//                Array.Copy(tmp, diff, decryptedData, read, size);

//                _stream.Position -= 0x800 - (diff + size);

//                if ((read += size) >= alignedCount)
//                    return;
//            }

//            _bodyDecryptor.Seed = (int)(alignedPosition + read - 0x90) / 0x10 + 1;

//            _stream.Read(decryptedData, read, alignedCount - read);
//            _bodyDecryptor.TransformBlock(decryptedData, read, alignedCount - read, decryptedData, read);
//        }

//        //public override void Write(byte[] buffer, int offset, int count)
//        //{
//        //    ValidateWrite(buffer, offset, count);

//        //    if (count == 0) return;
//        //    var readBuffer = GetInitializedReadBuffer(count, out var dataStart);

//        //    PeakOverlappingData(readBuffer, (int)dataStart, count);

//        //    Array.Copy(buffer, offset, readBuffer, dataStart, count);

//        //    var originalPosition = Position;

//        //    Encrypt((int)(_stream.Position - dataStart), readBuffer);

//        //    if (originalPosition + count > _length)
//        //        _length = originalPosition + count;

//        //    Seek(originalPosition + count, SeekOrigin.Begin);
//        //}

//        //private void Encrypt(int begin, byte[] readBuffer)
//        //{
//        //    var encBuffer = new byte[readBuffer.Length];

//        //    _encryptor.TransformBlock(readBuffer, 0, readBuffer.Length, encBuffer, 0);

//        //    var originalPosition = Position;
//        //    _stream.Position -= dataStart;
//        //    _stream.Write(encBuffer, 0, encBuffer.Length);
//        //}

//        //private void ValidateWrite(byte[] buffer, int offset, int count)
//        //{
//        //    if (!CanWrite)
//        //        throw new NotSupportedException("Write is not supported");
//        //    if (offset < 0 || count < 0)
//        //        throw new ArgumentOutOfRangeException("Offset or count can't be negative.");
//        //    if (offset + count > buffer.Length)
//        //        throw new InvalidDataException("Buffer too short.");
//        //}

//        //private long GetBlocksBetween(long position)
//        //{
//        //    var offsetBlock = GetCurrentBlock(position);
//        //    var lengthBlock = GetCurrentBlock(Length);
//        //    if (Math.Max(offsetBlock, lengthBlock) - Math.Min(offsetBlock, lengthBlock) > 1)
//        //        return Math.Max(offsetBlock, lengthBlock) - Math.Min(offsetBlock, lengthBlock) - 1;
//        //    else
//        //        return 0;
//        //}

//        //private byte[] GetInitializedReadBuffer(int count, out long dataStart)
//        //{
//        //    var blocksBetweenLengthPosition = GetBlocksBetween(Position);
//        //    var bytesIntoBlock = Position % BlockSizeBytes;

//        //    dataStart = bytesIntoBlock;

//        //    var bufferBlocks = GetBlockCount(bytesIntoBlock + count);
//        //    if (Position >= Length)
//        //    {
//        //        bufferBlocks += blocksBetweenLengthPosition;
//        //        dataStart += blocksBetweenLengthPosition * BlockSizeBytes;
//        //    }

//        //    var bufferLength = bufferBlocks * BlockSizeBytes;

//        //    return new byte[bufferLength];
//        //}

//        //private void PeakOverlappingData(byte[] buffer, int offset, int count)
//        //{
//        //    if (Position - offset < Length)
//        //    {
//        //        long originalPosition = Position;
//        //        var readBuffer = Decrypt(Position - offset, (int)GetBlockCount(Math.Min(Length - (Position - offset), count)) * BlockSizeBytes);
//        //        Array.Copy(readBuffer, 0, buffer, 0, readBuffer.Length);
//        //        Position = originalPosition;
//        //    }
//        //}
//    }
//}
