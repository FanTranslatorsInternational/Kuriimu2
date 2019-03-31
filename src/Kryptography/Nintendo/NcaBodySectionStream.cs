//using Kryptography.AES;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;

//namespace Kryptography.Nintendo
//{
//    public class NcaBodySectionStream : Stream
//    {
//        private const long _ncaHeaderLength = 0xC00;

//        private int _cryptoType;
//        private byte[] _keyArea;
//        private NcaKeyStorage _keyStorage;
//        private Stream _baseStream;

//        private bool _hasRightsId;

//        public NcaBodySectionStream(Stream input, long offset, long length, bool hasRightsId, int masterKeyRev, int cryptoType, byte[] keyArea, NcaKeyStorage keyStorage)
//            : this(input, offset, length, hasRightsId, masterKeyRev, cryptoType, keyArea, keyStorage, null) { }

//        public NcaBodySectionStream(Stream input, long offset, long length, bool hasRightsId, int masterKeyRev, int cryptoType, byte[] keyArea, NcaKeyStorage keyStorage, byte[] section_ctr)
//        {
//            _hasRightsId = hasRightsId;

//            if (hasRightsId)
//            {
//                if (keyStorage.TitleKey == null)
//                    throw new InvalidDataException("Title Key is needed.");

//                var ecb = new EcbStream(keyStorage.TitleKey, keyStorage.TitleKEK[masterKeyRev]);
//                var dec_title_key = new byte[0x10];
//                ecb.Read(dec_title_key, 0, 0x10);

//                _baseStream = new CtrStream(input, offset, length, dec_title_key, GenerateCTR(section_ctr, offset), false);
//            }
//            else
//            {

//                if (cryptoType < 1 || cryptoType > 4)
//                    throw new InvalidDataException($"SectionCrypto {cryptoType} is invalid.");
//                _cryptoType = cryptoType;

//                if (keyArea.Length != 0x40)
//                    throw new InvalidDataException("KeyArea must be 0x40 bytes.");
//                _keyArea = keyArea;

//                _keyStorage = keyStorage;

//                switch (cryptoType)
//                {
//                    case 1:
//                        _baseStream = new SubStream(input, offset, length);
//                        break;

//                    case 2:
//                        _baseStream = new XtsStream(input, offset, length, GetKeyAreaKey(0), new byte[0x10], false, 512);
//                        break;

//                    case 3:
//                        _baseStream = new CtrStream(input, offset, length, GetKeyAreaKey(1), GenerateCTR(section_ctr, offset), false);
//                        break;
//                }
//            }
//        }

//        public override bool CanRead => _baseStream.CanRead;

//        public override bool CanSeek => _baseStream.CanSeek;

//        public override bool CanWrite => _baseStream.CanWrite;

//        public override long Length => _baseStream.Length;

//        public override long Position { get; set; }

//        public override void Flush()
//        {
//        }

//        public override int Read(byte[] buffer, int offset, int count)
//        {
//            if (Position > Length)
//                throw new InvalidDataException($"NCA Section is only 0x{Length:X8} bytes. Position was 0x{Position:X8}.");
//            if (Position + count > Length)
//                throw new InvalidDataException($"NCA Section is only 0x{Length:X8} bytes. Position was 0x{Position:X8}. It was tried to read 0x{count:X8} bytes");

//            var origPos = _baseStream.Position;
//            _baseStream.Position = Position;

//            if (_hasRightsId)
//            {
//                return _baseStream.Read(buffer, offset, count);
//            }
//            else
//            {
//                switch (_cryptoType)
//                {
//                    case 1: //No Crypto
//                    case 2: //XTS
//                    case 3: //CTR
//                        var read = _baseStream.Read(buffer, offset, count);

//                        Position += read;
//                        _baseStream.Position = origPos;

//                        return read;

//                    case 4: //BKTR
//                        throw new InvalidOperationException("BKTR Sections are not supported yet.");

//                    default:
//                        throw new InvalidDataException($"SectionCrypto {_cryptoType} is invalid.");
//                }
//            }
//        }

//        public override long Seek(long offset, SeekOrigin origin)
//        {
//            switch (origin)
//            {
//                case SeekOrigin.Begin: return Position = offset;
//                case SeekOrigin.Current: return Position += offset;
//                case SeekOrigin.End: return Position = Length + offset;
//            }
//            return _baseStream.Seek(offset, origin);
//        }

//        public override void SetLength(long value)
//        {
//            throw new NotImplementedException();
//        }

//        public override void Write(byte[] buffer, int offset, int count)
//        {
//            if (Position > Length)
//                throw new InvalidDataException($"NCA Section is only 0x{Length:X8} bytes. Position was 0x{Position:X8}.");
//            if (Position + count > Length)
//                throw new InvalidDataException($"NCA Section is only 0x{Length:X8} bytes. Position was 0x{Position:X8}. It was tried to read 0x{count:X8} bytes");

//            var origPos = _baseStream.Position;
//            _baseStream.Position = Position;

//            if (_hasRightsId)
//            {
//                _baseStream.Read(buffer, offset, count);
//            }
//            else
//            {
//                switch (_cryptoType)
//                {
//                    case 1: //No Crypto
//                    case 2: //XTS
//                    case 3: //CTR
//                        _baseStream.Write(buffer, offset, count);

//                        Position += count;
//                        _baseStream.Position = origPos;

//                        break;

//                    case 4: //BKTR
//                        throw new InvalidOperationException("BKTR Sections are not supported yet.");
//                    default:
//                        throw new InvalidDataException($"SectionCrypto {_cryptoType} is invalid.");
//                }
//            }
//        }

//        private static byte[] GenerateCTR(byte[] section_ctr, long offset)
//        {
//            int ctr = 0;
//            for (int i = 0; i < 4; i++)
//                ctr |= section_ctr[i] << ((3 - i) * 8);

//            return GenerateCTR(ctr, offset);
//        }

//        private static byte[] GenerateCTR(int section_ctr, long offset)
//        {
//            offset >>= 4;
//            byte[] ctr = new byte[0x10];
//            for (int i = 0; i < 4; i++)
//            {
//                ctr[0x4 - i - 1] = (byte)(section_ctr & 0xFF);
//                section_ctr >>= 8;
//            }
//            for (int i = 0; i < 8; i++)
//            {
//                ctr[0x10 - i - 1] = (byte)(offset & 0xFF);
//                offset >>= 8;
//            }
//            return ctr;
//        }

//        private byte[] GetKeyAreaKey(int areaId)
//        {
//            switch (areaId)
//            {
//                case 0:
//                    return _keyArea.Take(0x20).ToArray();

//                case 1:
//                    return _keyArea.Skip(0x20).Take(0x10).ToArray();

//                case 2:
//                    return _keyArea.Skip(0x30).Take(0x10).ToArray();

//                default:
//                    throw new InvalidDataException($"KeyArea Key {areaId} doesn't exist.");
//            }
//        }
//    }
//}