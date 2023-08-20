using System;
using System.IO;
using Kryptography.AES;
using Kryptography.Nintendo.Switch.Models;

namespace Kryptography.Nintendo.Switch.Streams
{
    class NcaBodyStream : Stream
    {
        private readonly Stream _baseStream;

        public override bool CanRead => _baseStream.CanRead && true;

        public override bool CanSeek => _baseStream.CanSeek && true;

        public override bool CanWrite => _baseStream.CanWrite && true;

        public override long Length => _baseStream.Length;

        public override long Position { get; set; }

        public NcaBodyStream(Stream input, NcaSectionCrypto sectionCryptoType, byte[] iv, byte[] decKeyArea, byte[] decTitleKey)
        {
            if (sectionCryptoType == NcaSectionCrypto.TitleKey)
            {
                if (decTitleKey == null)
                    throw new ArgumentNullException(nameof(decTitleKey));

                _baseStream = new CtrStream(input, decTitleKey, iv, false);
            }
            else
            {
                switch (sectionCryptoType)
                {
                    case NcaSectionCrypto.NoCrypto:
                        _baseStream = input;
                        break;

                    case NcaSectionCrypto.Xts:
                        var keyAreaKey = new byte[0x20];
                        Array.Copy(decKeyArea, keyAreaKey, 0x20);
                        _baseStream = new XtsStream(input, keyAreaKey, iv, true, false, 0x200);
                        break;

                    case NcaSectionCrypto.Ctr:
                        keyAreaKey = new byte[0x10];
                        Array.Copy(decKeyArea, 0x20, keyAreaKey, 0, 0x10);
                        _baseStream = new CtrStream(input, keyAreaKey, iv, false);
                        break;

                    case NcaSectionCrypto.Bktr:
                        //BKTR, some CTR
                        //stub
                        // TODO: Implement BKTR cryptography
                        throw new NotSupportedException($"This section crypto is not supported.");
                }
            }
        }

        public override void Flush() => _baseStream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException("Can't read stream.");

            var bkPos = _baseStream.Position;
            _baseStream.Position = Position;
            var readBytes = _baseStream.Read(buffer, offset, count);
            _baseStream.Position = bkPos;

            Position += count;

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

        public override void SetLength(long value) => _baseStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException("Can't write to stream.");

            var bkPos = _baseStream.Position;
            _baseStream.Position = Position;
            _baseStream.Write(buffer, offset, count);

            //_baseStream.Flush();
            _baseStream.Position = bkPos;

            Position += count;
        }
    }
}
