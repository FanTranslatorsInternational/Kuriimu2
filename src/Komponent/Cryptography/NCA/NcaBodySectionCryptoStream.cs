using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Kontract.Abstracts;
using Komponent.Cryptography.AES;

namespace Komponent.Cryptography.NCA
{
    public class NcaBodySectionCryptoStream : KryptoStream
    {
        private const long _ncaHeaderLength = 0xC00;

        private int _cryptoType;
        private byte[] _keyArea;
        private KeyStorage _keyStorage;
        private KryptoStream _kryptoStream;
        private long _length;
        private long _offset;
        private Stream _stream;
        public NcaBodySectionCryptoStream(Stream input, long offset, long length, int cryptoType, byte[] keyArea, KeyStorage keyStorage)
            : this(input, offset, length, cryptoType, keyArea, keyStorage, null) { }

        public NcaBodySectionCryptoStream(Stream input, long offset, long length, int cryptoType, byte[] keyArea, KeyStorage keyStorage, byte[] section_ctr)
        {
            _stream = input;
            _offset = offset;
            _length = length;

            if (cryptoType < 1 || cryptoType > 4)
                throw new InvalidDataException($"SectionCrypto {cryptoType} is invalid.");
            _cryptoType = cryptoType;

            if (keyArea.Length != 0x40)
                throw new InvalidDataException("KeyArea must be 0x40 bytes.");
            _keyArea = keyArea;

            _keyStorage = keyStorage;

            switch (cryptoType)
            {
                case 2:
                    _kryptoStream = new XtsStream(input, offset, length, GetKeyAreaKey(0), true);
                    break;
                case 3:
                    _kryptoStream = new CtrStream(input, offset, length, GetKeyAreaKey(1), GenerateCTR(section_ctr, offset));
                    break;
            }
        }

        public override int BlockSize => throw new NotImplementedException();
        public override int BlockSizeBytes => throw new NotImplementedException();
        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override byte[] IV => throw new NotImplementedException();
        public override List<byte[]> Keys => throw new NotImplementedException();
        public override int KeySize => throw new NotImplementedException();
        public override long Length => _length;

        public override long Position { get => _stream.Position - _offset; set => Seek(value, SeekOrigin.Begin); }
        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Position > Length)
                throw new InvalidDataException($"NCA Section is only 0x{Length:X8} bytes. Position was 0x{Position:X8}.");
            if (Position + count > Length)
                throw new InvalidDataException($"NCA Section is only 0x{Length:X8} bytes. Position was 0x{Position:X8}. It was tried to read 0x{count:X8} bytes");

            switch (_cryptoType)
            {
                case 1:
                    return _stream.Read(buffer, offset, count);
                case 2: //XTS
                case 3: //CTR
                    return _kryptoStream.Read(buffer, offset, count);
                case 4: //BKTR
                    throw new InvalidOperationException("BKTR Sections are not supported yet.");
                default:
                    throw new InvalidDataException($"SectionCrypto {_cryptoType} is invalid.");
            }
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
            if (Position > Length)
                throw new InvalidDataException($"NCA Section is only 0x{Length:X8} bytes. Position was 0x{Position:X8}.");
            if (Position + count > Length)
                throw new InvalidDataException($"NCA Section is only 0x{Length:X8} bytes. Position was 0x{Position:X8}. It was tried to read 0x{count:X8} bytes");

            switch (_cryptoType)
            {
                case 1:
                    _stream.Write(buffer, offset, count);
                    break;
                case 2: //XTS
                case 3: //CTR
                    _kryptoStream.Write(buffer, offset, count);
                    break;
                case 4: //BKTR
                    throw new InvalidOperationException("BKTR Sections are not supported yet.");
                default:
                    throw new InvalidDataException($"SectionCrypto {_cryptoType} is invalid.");
            }
        }

        private static byte[] GenerateCTR(byte[] section_ctr, long offset)
        {
            int ctr = 0;
            for (int i = 0; i < 4; i++)
                ctr |= section_ctr[i] << ((3 - i) * 8);

            return GenerateCTR(ctr, offset);
        }

        private static byte[] GenerateCTR(int section_ctr, long offset)
        {
            offset >>= 4;
            byte[] ctr = new byte[0x10];
            for (int i = 0; i < 4; i++)
            {
                ctr[0x4 - i - 1] = (byte)(section_ctr & 0xFF);
                section_ctr >>= 8;
            }
            for (int i = 0; i < 8; i++)
            {
                ctr[0x10 - i - 1] = (byte)(offset & 0xFF);
                offset >>= 8;
            }
            return ctr;
        }
        private byte[] GetKeyAreaKey(int count)
        {
            switch (count)
            {
                case 0:
                    return _keyArea.Take(0x20).ToArray();
                case 1:
                    return _keyArea.Skip(0x20).Take(0x10).ToArray();
                case 2:
                    return _keyArea.Skip(0x30).Take(0x10).ToArray();
                default:
                    throw new InvalidDataException($"KeyArea Key {count} doesn't exist.");
            }
        }
    }
}
