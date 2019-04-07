using Komponent.IO;
using plugin_krypto_nintendo.Nca.KeyStorages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_krypto_nintendo.Nca.Streams
{
    internal class SectionLimits
    {
        public int Index { get; }
        public long StartOffset { get; }
        public long Length { get; }

        public SectionLimits(int index, long startOffset, long length)
        {
            Index = index;
            StartOffset = startOffset;
            Length = length;
        }
    }

    internal class NcaReadableStream : Stream
    {
        private Stream _baseStream;
        private NcaHeaderStream _header;
        private NcaBodyStream[] _sections;
        private SectionLimits[] _sectionLimits;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _baseStream.Length;

        public override long Position { get; set; }

        public NcaReadableStream(Stream input, NcaVersion version, byte[] decKeyArea, NcaKeyStorage keyStorage, byte[] decTitleKey, bool isEncrypted)
        {
            _baseStream = input;
            _header = new NcaHeaderStream(new SubStream(input, 0, NcaConstants.HeaderSize), version, keyStorage.HeaderKey, isEncrypted);

            _header.Position = 0x240;
            var sections = new byte[0x40];
            _header.Read(sections, 0, 0x40);
            _sections = new NcaBodyStream[4];
            _sectionLimits = new SectionLimits[4];
            for (int i = 0; i < 4; i++)
            {
                var sectionOffset = NcaConstants.HeaderWithoutSectionsSize + i * NcaConstants.MediaSize;
                long offset = BitConverter.ToInt32(sections, i * 0x10) * NcaConstants.MediaSize;
                long length = BitConverter.ToInt32(sections, i * 0x10 + 4) * NcaConstants.MediaSize - offset;

                if (offset == 0 || length == 0)
                    continue;

                var sectionCryptoType = new byte[1];
                _header.Position = sectionOffset + 4;
                _header.Read(sectionCryptoType, 0, 1);
                if (sectionCryptoType[0] < 1 || sectionCryptoType[0] > 4)
                    throw new InvalidOperationException($"CryptoType for section {i} must be 1-4. Found CryptoType: {sectionCryptoType[0]}");

                var sectionCtr = new byte[8];
                _header.Position = sectionOffset + 0x140;
                _header.Read(sectionCtr, 0, 8);
                sectionCtr = sectionCtr.Reverse().ToArray();

                var subStream = new SubStream(input, offset, length);
                _sections[i] = new NcaBodyStream(subStream, sectionCryptoType[0], GenerateCTR(sectionCtr, offset), decKeyArea, decTitleKey);
                _sectionLimits[i] = new SectionLimits(i, offset, length);
            }
        }

        public override void Flush()
        {
            _header.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException("Can't read stream.");
            if (Position + count > Length)
                throw new EndOfStreamException("Can't read beyond stream.");

            int readBytes = 0;
            if (Position < NcaConstants.HeaderSize)
            {
                var toRead = Math.Min(NcaConstants.HeaderSize - Position, count);
                _header.Position = Position;
                readBytes = _header.Read(buffer, offset, (int)toRead);
                Position += readBytes;
            }

            while (readBytes < count)
            {
                var newPosition = Position + readBytes;
                var toRead = count - readBytes;

                var sectionToRead = _sectionLimits.FirstOrDefault(x => x != null && x.StartOffset >= newPosition && x.StartOffset + x.Length < newPosition);
                if (sectionToRead == null)
                {
                    var nextSectionLimits = _sectionLimits.Where(x => x != null && x.StartOffset - newPosition >= 0);
                    if (nextSectionLimits.Any())
                        toRead = (int)Math.Min(toRead, nextSectionLimits.Min(x => x.StartOffset) - newPosition);

                    var bkPos = _baseStream.Position;
                    _baseStream.Position = newPosition;
                    readBytes += _baseStream.Read(buffer, offset + readBytes, toRead);
                    _baseStream.Position = bkPos;
                }
                else
                {
                    // TODO: Calculate length
                    toRead = (int)Math.Min(toRead, sectionToRead.StartOffset + sectionToRead.Length - newPosition);
                    _sections[sectionToRead.Index].Position = newPosition;
                    readBytes += _sections[sectionToRead.Index].Read(buffer, offset + readBytes, toRead);
                }
            }

            Position += readBytes;
            return readBytes;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
                throw new NotSupportedException("Can't seek stream.");

            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length + offset;
                    break;
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException("Can't write to stream.");

            throw new NotImplementedException();
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
    }
}
