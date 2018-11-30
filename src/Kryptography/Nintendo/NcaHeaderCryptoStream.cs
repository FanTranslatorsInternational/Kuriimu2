using Kryptography.AES;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Kryptography.Nintendo
{
    public class NcaHeaderCryptoStream : Stream
    {
        private NcaKeyStorage _ncaKeyStorage;
        private Stream _baseStream;

        public NCAVersion NCAVersion { get; private set; }
        public bool IsHeaderEncrypted { get; private set; }
        public bool HasRightsId
        {
            get
            {
                var origPos = Position;
                Position = 0x230;

                byte[] rightsId = new byte[0x10];
                Read(rightsId, 0, 0x10);

                bool hasRightsID = false;
                for (int i = 0; i < 0x10; i++)
                    if (rightsId[i] != 0)
                    {
                        hasRightsID = true;
                        break;
                    }

                Position = origPos;

                return hasRightsID;
            }
        }

        public NcaHeaderCryptoStream(Stream input, long offset, long length, NcaKeyStorage keyStorage, NCAVersion ncaVersion = NCAVersion.None)
        {
            _baseStream = new SubStream(input, offset, length);
            Initialize(keyStorage, ncaVersion);
        }

        public NcaHeaderCryptoStream(byte[] input, long offset, long length, NcaKeyStorage keyStorage, NCAVersion ncaVersion = NCAVersion.None) :
            this(new MemoryStream(input), offset, length, keyStorage, ncaVersion)
        {
        }

        private void Initialize(NcaKeyStorage keyStorage, NCAVersion ncaVersion)
        {
            _ncaKeyStorage = keyStorage;
            NCAVersion = ncaVersion;
        }

        //public NcaHeaderCryptoStream(Stream input, NcaKeyStorage keyStorage, NCAVersion ncaVersion, bool shouldEncrypt)
        //{
        //    _stream = input;
        //    _ncaKeyStorage = keyStorage;

        //    NCAVersion = ncaVersion;
        //    IsHeaderEncrypted = shouldEncrypt;
        //}

        public NcaHeaderCryptoStream(Stream input, NcaKeyStorage keyStorage)
        {
            _baseStream = input;
            _ncaKeyStorage = keyStorage;

            SetHeaderInformation();
        }

        private void SetHeaderInformation()
        {
            var originalPos = _baseStream.Position;
            _baseStream.Position = 0x200;

            var magic = new byte[4];
            _baseStream.Read(magic, 0, magic.Length);

            if (Encoding.ASCII.GetString(magic) == "NCA2" || Encoding.ASCII.GetString(magic) == "NCA3")
                IsHeaderEncrypted = false;
            else
            {
                IsHeaderEncrypted = true;

                NCAVersion = NCAVersion.NCA3;
                Position = 0x200;
                Read(magic, 0, 4);
                if (!Enum.TryParse<NCAVersion>(Encoding.ASCII.GetString(magic), out var version))
                    throw new InvalidDataException("NCA Version couldn't be determined. Are keys correct?");
                NCAVersion = version;
            }

            _baseStream.Position = originalPos;
        }

        #region Overrides
        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => _baseStream.CanWrite;

        public override long Length => Common.NcaHeaderSize;
        public override long Position { get; set; }

        public override void Flush() => _baseStream.Flush();

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin: return Position = offset;
                case SeekOrigin.Current: return Position += offset;
                case SeekOrigin.End: return Position = Length + offset;
            }
            return _baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Position >= Common.NcaHeaderSize)
                throw new EndOfStreamException($"NCA Header is only 0x{Common.NcaHeaderSize:X4} bytes long but Position was 0x{Position:X4}.");
            if (Position + count > Common.NcaHeaderSize)
                throw new EndOfStreamException($"NCA Header is only 0x{Common.NcaHeaderSize:X4} bytes long but it was tried to read 0x{count:X4} bytes from Position 0x{Position:X4}.");

            if (!IsHeaderEncrypted)
            {
                return _baseStream.Read(buffer, offset, count);
            }
            else
            {
                var read = 0;

                var kryptoStream = new XtsStream(_baseStream, _ncaKeyStorage["header_key"], new byte[0x10], false, 512);
                if (Position < 0x400)
                {
                    read = kryptoStream.Read(buffer, offset, (int)Math.Min(count, 0x400 - Position));
                    if (read >= count)
                        return read;
                }
                switch (NCAVersion)
                {
                    case NCAVersion.NCA2:
                        var index = 0;
                        while (count - read > 0)
                        {
                            kryptoStream = new XtsStream(_baseStream, 0x400 + index * 0x200, 0x200, _ncaKeyStorage["header_key"], new byte[0x10], false, 512);
                            var read2 = kryptoStream.Read(buffer, offset + read, Math.Min(count - read, 0x200));
                            index++;
                            read += read2;
                        }
                        return read;

                    case NCAVersion.NCA3:
                        read += kryptoStream.Read(buffer, offset + read, count - read);
                        return read;

                    default:
                        throw new NotSupportedException($"Unsupported NCA Version {NCAVersion}");
                }
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (Position >= Common.NcaHeaderSize)
                throw new EndOfStreamException($"NCA Header is only 0x{Common.NcaHeaderSize:X4} bytes long but Position was 0x{Position:X4}.");
            if (Position + count > Common.NcaHeaderSize)
                throw new EndOfStreamException($"NCA Header is only 0x{Common.NcaHeaderSize:X4} bytes long but it was tried to write 0x{count:X4} bytes on Position 0x{Position:X4}.");

            if (!IsHeaderEncrypted)
            {
                _baseStream.Write(buffer, offset, count);
            }
            else
            {
                var nonSectionBytes = (int)Math.Min(count, 0x400 - Position);

                var kryptoStream = new XtsStream(_baseStream, _ncaKeyStorage["header_key"], new byte[16], false, 512);
                if (Position < 0x400)
                {
                    kryptoStream.Write(buffer.Take(nonSectionBytes).ToArray(), 0, nonSectionBytes);
                    if (nonSectionBytes == count)
                        return;
                }

                switch (NCAVersion)
                {
                    case NCAVersion.NCA2:
                        var index = 0;
                        var sectionBytesWritten = 0;
                        while (count - nonSectionBytes - sectionBytesWritten > 0)
                        {
                            var toWrite = Math.Min(count - nonSectionBytes - sectionBytesWritten, 0x200);
                            kryptoStream = new XtsStream(_baseStream, 0x400 + index * 0x200, 0x200, _ncaKeyStorage["header_key"], new byte[16], false, 512);
                            kryptoStream.Write(buffer, nonSectionBytes + sectionBytesWritten, toWrite);
                            index++;
                            sectionBytesWritten += toWrite;
                        }
                        break;

                    case NCAVersion.NCA3:
                        kryptoStream.Write(buffer, nonSectionBytes, count - nonSectionBytes);
                        break;

                    default:
                        throw new NotSupportedException($"Unsupported NCA Version {NCAVersion}");
                }
            }
        }
        #endregion

        #region Peeks
        public int PeekMasterKeyRev()
        {
            var origPos = Position;
            Position = 0x206;

            var cryptoType1 = new byte[1];
            Read(cryptoType1, 0, 1);

            Position = 0x220;
            var cryptoType2 = new byte[1];
            Read(cryptoType2, 0, 1);

            var cryptoType = (cryptoType2[0] > cryptoType1[0]) ? cryptoType2[0] : cryptoType1[0];
            if (cryptoType >= 1) cryptoType--;

            Position = origPos;

            return cryptoType;
        }

        public byte[] PeekDecryptedKeyArea()
        {
            var origPos = Position;
            Position = 0x207;

            var keyIndex = new byte[1];
            Read(keyIndex, 0, 1);

            if (keyIndex[0] < 0 || keyIndex[0] > 2)
                throw new InvalidDataException($"NCA KeyIndex must be 0-2. Found KeyIndex: {keyIndex[0]}");

            Position = 0x300;
            var keyArea = new byte[0x40];
            Read(keyArea, 0, 0x40);

            var decryptedKeyArea = new byte[0x40];
            switch (keyIndex[0])
            {
                case 0:
                    new EcbStream(keyArea, _ncaKeyStorage.KEKApplication[PeekMasterKeyRev()]).Read(decryptedKeyArea, 0, decryptedKeyArea.Length);
                    break;

                case 1:
                    new EcbStream(keyArea, _ncaKeyStorage.KEKOcean[PeekMasterKeyRev()]).Read(decryptedKeyArea, 0, decryptedKeyArea.Length);
                    break;

                case 2:
                    new EcbStream(keyArea, _ncaKeyStorage.KEKSystem[PeekMasterKeyRev()]).Read(decryptedKeyArea, 0, decryptedKeyArea.Length);
                    break;
            }

            Position = origPos;

            return decryptedKeyArea;
        }

        public int PeekSectionCryptoType(int section)
        {
            if (section < 0 || section > 3)
                throw new InvalidDataException($"NCAHeader only contains sections 0-3. Section {section} was given.");

            var origPos = Position;
            Position = 0x400 + section * 0x200 + 0x4;

            var cryptoType = new byte[1];
            Read(cryptoType, 0, 1);

            Position = origPos;

            return cryptoType[0];
        }

        public byte[] PeekSectionCtr(int section)
        {
            if (section < 0 || section > 3)
                throw new InvalidDataException($"NCAHeader only contains sections 0-3. Section {section} was given.");

            var origPos = Position;
            Position = 0x400 + section * 0x200 + 0x140;

            var ctr = new byte[8];
            Read(ctr, 0, 8);

            Position = origPos;

            return ctr;
        }

        public List<SectionEntry> PeekSections()
        {
            var origPos = Position;
            Position = 0x240;

            List<SectionEntry> result = new List<SectionEntry>();
            using (var br = new BinaryReader(this, Encoding.ASCII, true))
                for (int i = 0; i < 4; i++)
                    result.Add(new SectionEntry
                    {
                        mediaOffset = br.ReadInt32(),
                        endMediaOffset = br.ReadInt32(),
                        unk1 = br.ReadInt32(),
                        unk2 = br.ReadInt32()
                    });

            Position = origPos;
            return result;
        }
        #endregion

        public void WriteKeyArea(byte[] plainKeyArea, int cryptoType, int keyIndex)
        {
            if (keyIndex < 0 || keyIndex > 2)
                throw new InvalidDataException($"KeyIndex must be 0-2. Given KeyIndex: {keyIndex}");
            if (cryptoType < 0 || cryptoType > 0x1F)
                throw new InvalidDataException($"CryptoType must be 0-31. Given CryptoType: {cryptoType}");

            byte[] key = null;
            switch (keyIndex)
            {
                case 0:
                    key = _ncaKeyStorage.KEKApplication[cryptoType];
                    break;

                case 1:
                    key = _ncaKeyStorage.KEKOcean[cryptoType];
                    break;

                case 2:
                    key = _ncaKeyStorage.KEKSystem[cryptoType];
                    break;
            }

            var kryptoStream = new EcbStream(_baseStream, key);
            kryptoStream.Position = 0x300;
            kryptoStream.Write(plainKeyArea, 0, plainKeyArea.Length);
        }
    }
}