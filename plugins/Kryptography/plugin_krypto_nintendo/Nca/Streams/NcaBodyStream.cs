using Kryptography.AES;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_krypto_nintendo.Nca.Streams
{
    internal class NcaBodyStream : Stream
    {
        private long _internalLength;
        private Stream _baseStream;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _internalLength;

        public override long Position { get; set; }

        public NcaBodyStream(Stream input, byte sectionCryptoType, byte[] iv, byte[] decKeyArea, byte[] decTitleKey)
        {
            _internalLength = input.Length;

            if (decTitleKey != null)
                _baseStream = new CtrStream(input, decTitleKey, iv, false);
            else
            {
                switch (sectionCryptoType)
                {
                    case 1:
                        // No crypto
                        _baseStream = input;
                        break;
                    case 2:
                        // XTS
                        var key_area_key = new byte[0x20];
                        Array.Copy(decKeyArea, key_area_key, 0x20);
                        _baseStream = new XtsStream(input, key_area_key, iv, true, false, 0x200);
                        break;
                    case 3:
                        //CTR
                        key_area_key = new byte[0x10];
                        Array.Copy(decKeyArea, 0x20, key_area_key, 0, 0x10);
                        _baseStream = new CtrStream(input, key_area_key, iv, false);
                        break;
                    case 4:
                        //BKTR, some CTR
                        //stub
                        // TODO: Implement BKTR cryptography
                        throw new NotSupportedException($"This section crypto is not supported yet.");
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
            // TODO: Implement write
            if (!CanWrite)
                throw new NotSupportedException("Can't write to stream.");

            throw new NotImplementedException();
        }
    }
}
