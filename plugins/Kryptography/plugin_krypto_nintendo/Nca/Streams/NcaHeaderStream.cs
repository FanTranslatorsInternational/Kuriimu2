using Kryptography.AES;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_krypto_nintendo.Nca.Streams
{
    class NcaHeaderStream : Stream
    {
        private Stream _advancingBaseStream;
        private Stream _nonAdvancingBaseStream;
        private NcaVersion _version;
        private long _internalLength;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _internalLength;

        public override long Position { get; set; }

        public NcaHeaderStream(Stream header, NcaVersion version, byte[] headerKey, bool isEncrypted)
        {
            if (header.Length != NcaConstants.HeaderSize)
                throw new InvalidOperationException($"Nca headers can only be {NcaConstants.HeaderSize} bytes long.");

            _advancingBaseStream = !isEncrypted ? header : new XtsStream(header, headerKey, new byte[0x10], true, true, NcaConstants.MediaSize);
            _nonAdvancingBaseStream = !isEncrypted ? header : new XtsStream(header, headerKey, new byte[0x10], false, true, NcaConstants.MediaSize);
            _version = version;
            _internalLength = header.Length;
        }

        public override void Flush()
        {
            _advancingBaseStream.Flush();
            _nonAdvancingBaseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException("Can't read from stream.");
            if (Position > NcaConstants.HeaderSize || Position + count >= NcaConstants.HeaderSize)
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
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException("Can't write to stream.");

            throw new NotImplementedException(nameof(Write));
        }
    }
}
