﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kryptography.Extensions;
using Kryptography.Nintendo.Switch.KeyStorages;
using Kryptography.Nintendo.Switch.Models;

namespace Kryptography.Nintendo.Switch.Streams
{
    class NcaStream : Stream
    {
        private readonly Stream _baseStream;

        private readonly Stream _headerStream;
        private readonly Stream[] _sectionStreams;
        private readonly List<NcaBodySection> _sections;

        public override bool CanRead => _baseStream.CanRead && _headerStream.CanRead && _sectionStreams.All(x => x.CanRead) && true;

        public override bool CanSeek => _baseStream.CanSeek && _headerStream.CanSeek && _sectionStreams.All(x => x.CanSeek) && true;

        public override bool CanWrite => _baseStream.CanWrite && _headerStream.CanWrite && _sectionStreams.All(x => x.CanWrite) && true;

        public override long Length => _baseStream.Length;

        public override long Position { get; set; }

        public NcaStream(Stream input, NcaVersion version, NcaBodySection[] sections, NcaKeyStorage keyStorage, byte[] decKeyArea, byte[] decTitleKey)
        {
            _baseStream = input;

            _headerStream = new NcaHeaderStream(input, version, keyStorage.HeaderKey);

            _sections = sections.ToList();
            _sectionStreams = new Stream[sections.Length];
            for (int i = 0; i < sections.Length; i++)
            {
                // In the writable stream, all cipher streams encapsulate the whole stream instead of sub streaming them
                var sectionIv = new byte[0x10];
                if (sections[i].SectionCrypto == NcaSectionCrypto.TitleKey || sections[i].SectionCrypto == NcaSectionCrypto.Ctr)
                    // In case of Ctr we just set the base ctr, since with setting the stream position the counter will get updated correctly already
                    Array.Copy(sections[i].BaseSectionCtr, sectionIv, 0x10);
                else
                    /* Sections encrypted with XTS start with sector id 0 at their respective section offset
                     * Since the cipher stream will still start at offset 0, the sector id gets decremented to a point that it will be 0, reaching its section offset
                     * this code can be removed if XTS sections don't start at 0 but with a value representing their section offset
                     */
                    sectionIv.Decrement(sections[i].MediaOffset, false);
                _sectionStreams[i] = new NcaBodyStream(input, sections[i].SectionCrypto, sectionIv, decKeyArea, decTitleKey);
            }
        }

        public override void Flush()
        {
            _headerStream.Flush();
            foreach (var s in _sectionStreams)
                s.Flush();

            _baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException("Can't read stream.");
            if (Position + count > Length)
                throw new EndOfStreamException("Can't read beyond stream.");

            var readBytes = 0;
            if (Position < NcaConstants.HeaderSize)
            {
                var toRead = Math.Min(NcaConstants.HeaderSize - Position, count);
                _headerStream.Position = Position;
                readBytes = _headerStream.Read(buffer, offset, (int)toRead);
            }

            while (readBytes < count)
            {
                var newPosition = Position + readBytes;
                var toRead = count - readBytes;

                var sectionToRead = _sections.FirstOrDefault(x =>
                    x != null &&
                    newPosition >= x.MediaOffset * NcaConstants.MediaSize &&
                    newPosition < (x.MediaOffset + x.MediaLength) * NcaConstants.MediaSize);
                if (sectionToRead == null)
                {
                    var nextSectionLimits = _sections.Where(x => x != null && x.MediaOffset * NcaConstants.MediaSize - newPosition >= 0).ToArray();
                    if (nextSectionLimits.Any())
                        toRead = (int)Math.Min(toRead, nextSectionLimits.Min(x => x.MediaOffset * NcaConstants.MediaSize) - newPosition);

                    var bkPos = _baseStream.Position;
                    _baseStream.Position = newPosition;
                    readBytes += _baseStream.Read(buffer, offset + readBytes, toRead);
                    _baseStream.Position = bkPos;
                }
                else
                {
                    toRead = (int)Math.Min(toRead, sectionToRead.MediaOffset * NcaConstants.MediaSize + sectionToRead.MediaLength * NcaConstants.MediaSize - newPosition);
                    var sectionNr = _sections.IndexOf(sectionToRead);
                    _sectionStreams[sectionNr].Position = newPosition;
                    readBytes += _sectionStreams[sectionNr].Read(buffer, offset + readBytes, toRead);
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
            _headerStream.SetLength(value);
            foreach (var sec in _sectionStreams)
                sec.SetLength(value);
            _baseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // Prepare write buffer
            var newPosition = Math.Min(Length, Position);
            var lenPosDiff = Length - Position;
            var writeBuffer = new byte[Math.Max(0, lenPosDiff) + count];
            Array.Copy(buffer, offset, writeBuffer, Math.Max(0, lenPosDiff), count);

            // write buffer until its end
            var writtenBytes = 0;
            var writeLength = writeBuffer.Length;
            while (writtenBytes < writeLength)
            {
                var toWrite = writeLength - writtenBytes;

                if (newPosition < NcaConstants.HeaderSize)
                {
                    toWrite = (int)Math.Min(toWrite, NcaConstants.HeaderSize - newPosition);
                    _headerStream.Position = newPosition;
                    _headerStream.Write(writeBuffer, writtenBytes, toWrite);
                }
                else
                {
                    var sectionToWrite = _sections.FirstOrDefault(x =>
                            x != null &&
                            newPosition >= x.MediaOffset * NcaConstants.MediaSize &&
                            newPosition < x.MediaOffset * NcaConstants.MediaSize + x.MediaLength * NcaConstants.MediaSize);
                    if (sectionToWrite == null)
                    {
                        var nextSections = _sections.Where(x => x != null && x.MediaOffset * NcaConstants.MediaSize - newPosition >= 0).ToList();
                        if (nextSections.Any())
                            toWrite = Math.Min(toWrite, (int)(nextSections.Min(x => x.MediaOffset * NcaConstants.MediaSize) - newPosition));

                        var bkPos = _baseStream.Position;
                        _baseStream.Position = newPosition;
                        _baseStream.Write(writeBuffer, writtenBytes, toWrite);
                        _baseStream.Position = bkPos;
                    }
                    else
                    {
                        var sectionNr = _sections.ToList().IndexOf(sectionToWrite);
                        toWrite = (int)Math.Min(toWrite, (sectionToWrite.MediaOffset * NcaConstants.MediaSize + sectionToWrite.MediaLength * NcaConstants.MediaSize) - newPosition);

                        _sectionStreams[sectionNr].Position = newPosition;
                        _sectionStreams[sectionNr].Write(writeBuffer, writtenBytes, toWrite);
                    }
                }

                writtenBytes += toWrite;
                newPosition += toWrite;

                SetLength(_baseStream.Length);
            }

            Position += writtenBytes;
        }
    }
}
