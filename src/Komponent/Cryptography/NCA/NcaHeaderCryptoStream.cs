using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Komponent.Cryptography.AES;
using Komponent.IO;
using Kontract.Abstracts;

namespace Komponent.Cryptography.NCA
{
    public class NcaHeaderCryptoStream : KryptoStream
    {
        private const long _maxLength = 0xC00;
        private KeyStorage _keyStorage;
        private Stream _stream;
        public NcaHeaderCryptoStream(Stream input, KeyStorage keyStorage, NCAVersion ncaVersion, bool shouldEncrypt)
        {
            _stream = input;
            _keyStorage = keyStorage;

            NCAVersion = ncaVersion;
            IsHeaderEncrypted = shouldEncrypt;
        }

        public NcaHeaderCryptoStream(Stream input, KeyStorage keyStorage)
        {
            _stream = input;
            _keyStorage = keyStorage;

            SetHeaderInformation();
        }

        public override int BlockSize => throw new NotImplementedException();
        public override int BlockSizeBytes => throw new NotImplementedException();
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public bool IsHeaderEncrypted { get; private set; }
        public override byte[] IV => throw new NotImplementedException();
        public override List<byte[]> Keys => throw new NotImplementedException();
        public override int KeySize => throw new NotImplementedException();
        public override long Length => _maxLength;
        public NCAVersion NCAVersion { get; private set; }
        public override long Position { get => _stream.Position; set => Seek(value, SeekOrigin.Begin); }
        public override void Flush()
        {
        }

        public int PeekCryptoType()
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
                    new EcbStream(keyArea, _keyStorage[$"key_area_key_application_{PeekCryptoType():00}"]).Read(decryptedKeyArea, 0, decryptedKeyArea.Length);
                    break;
                case 1:
                    new EcbStream(keyArea, _keyStorage[$"key_area_key_ocean_{PeekCryptoType():00}"]).Read(decryptedKeyArea, 0, decryptedKeyArea.Length);
                    break;
                case 2:
                    new EcbStream(keyArea, _keyStorage[$"key_area_key_system_{PeekCryptoType():00}"]).Read(decryptedKeyArea, 0, decryptedKeyArea.Length);
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

            List<SectionEntry> result;
            using (var br = new BinaryReaderX(this, true))
                result = br.ReadMultiple<SectionEntry>(4);

            Position = origPos;
            return result;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Position >= _maxLength)
                throw new EndOfStreamException($"NCA Header is only 0x{_maxLength:X4} bytes long but Position was 0x{Position:X4}.");
            if (Position + count > _maxLength)
                throw new EndOfStreamException($"NCA Header is only 0x{_maxLength:X4} bytes long but it was tried to read 0x{count:X4} bytes from Position 0x{Position:X4}.");

            if (!IsHeaderEncrypted)
            {
                return _stream.Read(buffer, offset, count);
            }
            else
            {
                var read = 0;

                var xtsStream = new XtsStream(_stream, _keyStorage["header_key"], true);
                if (Position < 0x400)
                {
                    read = xtsStream.Read(buffer, offset, (int)Math.Min(count, 0x400 - Position));
                    if (read >= count)
                        return read;
                }
                switch (NCAVersion)
                {
                    case NCAVersion.NCA2:
                        var index = 0;
                        while (count - read > 0)
                        {
                            xtsStream = new XtsStream(_stream, 0x400 + index * 0x200, 0x200, _keyStorage["header_key"], true);
                            var read2 = xtsStream.Read(buffer, offset + read, Math.Min(count - read, 0x200));
                            index++;
                            read += read2;
                        }
                        return read;
                    case NCAVersion.NCA3:
                        read += xtsStream.Read(buffer, offset + read, count - read);
                        return read;
                    default:
                        throw new NotSupportedException("Unsupported NCA Version.");
                }
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (Position >= _maxLength)
                throw new EndOfStreamException($"NCA Header is only 0x{_maxLength:X4} bytes long but Position was 0x{Position:X4}.");
            if (Position + count > _maxLength)
                throw new EndOfStreamException($"NCA Header is only 0x{_maxLength:X4} bytes long but it was tried to write 0x{count:X4} bytes on Position 0x{Position:X4}.");

            if (!IsHeaderEncrypted)
            {
                _stream.Write(buffer, offset, count);
            }
            else
            {
                var nonSectionBytes = (int)Math.Min(count, 0x400 - Position);

                var xtsStream = new XtsStream(_stream, _keyStorage["header_key"], true);
                if (Position < 0x400)
                {
                    xtsStream.Write(buffer.Take(nonSectionBytes).ToArray(), 0, nonSectionBytes);
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
                            xtsStream = new XtsStream(new SubStream(_stream, 0x400 + index * 0x200, 0x200), _keyStorage["header_key"], true);
                            xtsStream.Write(buffer, nonSectionBytes + sectionBytesWritten, toWrite);
                            index++;
                            sectionBytesWritten += toWrite;
                        }
                        break;
                    case NCAVersion.NCA3:
                        xtsStream.Write(buffer, nonSectionBytes, count - nonSectionBytes);
                        break;
                    default:
                        throw new NotSupportedException("Unsupported NCA Version.");
                }
            }
        }

        public void WriteKeyArea(byte[] plainKeyArea, int cryptoType, int keyIndex)
        {
            if (keyIndex < 0 || keyIndex > 2)
                throw new InvalidDataException($"KeyIndex must be 0-2. Given KeyIndex: {keyIndex}");
            if (cryptoType < 0 || cryptoType > 0x1F)
                throw new InvalidDataException($"CryptoType must be 0-31. Given CryptoType: {cryptoType}");

            var origPos = Position;
            Position = 0x300;

            switch (keyIndex)
            {
                case 0:
                    new EcbStream(_stream, _keyStorage[$"key_area_key_application_{cryptoType:00}"]).Write(plainKeyArea, 0, plainKeyArea.Length);
                    break;
                case 1:
                    new EcbStream(_stream, _keyStorage[$"key_area_key_ocean_{cryptoType:00}"]).Write(plainKeyArea, 0, plainKeyArea.Length);
                    break;
                case 2:
                    new EcbStream(_stream, _keyStorage[$"key_area_key_system_{cryptoType:00}"]).Write(plainKeyArea, 0, plainKeyArea.Length);
                    break;
            }

            Position = origPos;
        }

        private void SetHeaderInformation()
        {
            var originalPos = Position;
            Position = 0x200;

            var magic = new byte[4];
            _stream.Read(magic, 0, magic.Length);

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

            Position = originalPos;
        }
    }
}
