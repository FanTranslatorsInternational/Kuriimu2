using plugin_krypto_nintendo.Nca.KeyStorages;
using plugin_krypto_nintendo.Nca.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_krypto_nintendo.Nca.Streams
{
    internal class NcaWritableStream : Stream
    {
        private Stream _baseStream;

        private Stream _headerStream;
        private Stream[] _sectionStreams;
        private NcaBodySection[] _sections;

        public override bool CanRead => false;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _baseStream.Length;

        public override long Position { get; set; }

        public NcaWritableStream(Stream input, NcaVersion version, byte[] decKeyArea, NcaKeyStorage keyStorage, byte[] decTitleKey, NcaBodySection[] sections, bool isEncrypted)
        {
            _baseStream = input;

            _sections = sections;
            _sectionStreams = new Stream[sections.Length];
            for (int i = 0; i < sections.Length; i++)
            {
                ;
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

            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }
}
