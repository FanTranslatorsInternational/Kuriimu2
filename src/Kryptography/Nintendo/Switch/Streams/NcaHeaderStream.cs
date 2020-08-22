using System;
using System.IO;
using Kryptography.AES;
using Kryptography.Nintendo.Switch.Models;

namespace Kryptography.Nintendo.Switch.Streams
{
    class NcaHeaderStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly Stream _advancingBaseStream;
        private readonly Stream _nonAdvancingBaseStream;
        private readonly NcaVersion _version;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _baseStream.Length;

        public override long Position { get; set; }

        public NcaHeaderStream(Stream header, NcaVersion version, byte[] headerKey)
        {
            _baseStream = header;
            _advancingBaseStream = new XtsStream(header, headerKey, new byte[0x10], true, false, NcaConstants.MediaSize);
            _nonAdvancingBaseStream = new XtsStream(header, headerKey, new byte[0x10], false, false, NcaConstants.MediaSize);
            _version = version;
        }

        public override void Flush()
        {
            _advancingBaseStream.Flush();
            _nonAdvancingBaseStream.Flush();
            _baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException("Can't read from stream.");
            if (Position > NcaConstants.HeaderSize || Position + count > NcaConstants.HeaderSize)
                throw new EndOfStreamException();

            var bkPosNonAdvance = _nonAdvancingBaseStream.Position;
            var bkPosAdvance = _advancingBaseStream.Position;

            int readBytes = 0;
            switch (_version)
            {
                case NcaVersion.NCA2:
                    int toRead;
                    if (Position < NcaConstants.HeaderWithoutSectionsSize)
                    {
                        _advancingBaseStream.Position = Position;
                        toRead = (int)Math.Min(count, NcaConstants.HeaderWithoutSectionsSize - Position);
                        readBytes = _advancingBaseStream.Read(buffer, offset, toRead);
                    }

                    toRead = count - readBytes;
                    _nonAdvancingBaseStream.Position = Position + readBytes;
                    readBytes += _nonAdvancingBaseStream.Read(buffer, offset + readBytes, toRead);
                    break;
                case NcaVersion.NCA3:
                    _advancingBaseStream.Position = Position;
                    readBytes = _advancingBaseStream.Read(buffer, offset, count);
                    break;
            }

            Position += readBytes;

            _nonAdvancingBaseStream.Position = bkPosNonAdvance;
            _advancingBaseStream.Position = bkPosAdvance;

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
            _nonAdvancingBaseStream.SetLength(value);
            _advancingBaseStream.SetLength(value);
            _baseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException("Can't write to stream.");

            var bkPosNonAdvance = _nonAdvancingBaseStream.Position;
            var bkPosAdvance = _advancingBaseStream.Position;

            var writtenBytes = 0;
            var newPosition = Position;
            while (writtenBytes < count)
            {
                var toWrite = count - writtenBytes;
                if (newPosition < NcaConstants.HeaderWithoutSectionsSize)
                {
                    toWrite = (int)Math.Min(toWrite, NcaConstants.HeaderWithoutSectionsSize - newPosition);
                    _advancingBaseStream.Position = newPosition;
                    _advancingBaseStream.Write(buffer, offset + writtenBytes, toWrite);
                }
                else
                {
                    switch (_version)
                    {
                        case NcaVersion.NCA2:
                            _nonAdvancingBaseStream.Position = newPosition;
                            _nonAdvancingBaseStream.Write(buffer, offset + writtenBytes, toWrite);
                            break;
                        case NcaVersion.NCA3:
                            _advancingBaseStream.Position = newPosition;
                            _advancingBaseStream.Write(buffer, offset + writtenBytes, toWrite);
                            break;
                    }
                }

                writtenBytes += toWrite;
                newPosition += toWrite;

                SetLength(_baseStream.Length);
            }

            Position += writtenBytes;
            
            _nonAdvancingBaseStream.Position = bkPosNonAdvance;
            _advancingBaseStream.Position = bkPosAdvance;
        }
    }
}
