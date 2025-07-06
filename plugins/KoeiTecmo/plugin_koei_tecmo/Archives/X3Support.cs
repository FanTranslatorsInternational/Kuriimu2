using System.Buffers.Binary;
using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Kompression.Contract;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_koei_tecmo.Archives
{
    class X3Header
    {
        public uint magic = 0x0133781D;
        public int fileCount;
        public int alignment = 0x20;
        public int zero0;
    }

    class X3FileEntry
    {
        public long offset;
        public int fileSize;
        public int decompressedFileSize;

        public bool IsCompressed => fileSize != decompressedFileSize && decompressedFileSize > 0;
    }

    class X3ArchiveFile : ArchiveFile
    {
        private readonly Stream _rawStream;
        private readonly ArchiveFileInfo _fileInfo;

        public X3FileEntry Entry { get; }

        public bool ShouldCompress { get; }

        public X3ArchiveFile(ArchiveFileInfo fileInfo, Stream rawStream, X3FileEntry entry) : base(fileInfo)
        {
            _rawStream = rawStream;
            _fileInfo = fileInfo;

            Entry = entry;
            ShouldCompress = entry.IsCompressed;
        }

        public Stream GetFinalStream()
        {
            if (!ContentChanged)
            {
                _rawStream.Position = 0;
                return _rawStream;
            }

            var result = _fileInfo.FileData;
            if (ShouldCompress && ContentChanged)
                return X3CompressedStream.Compress(_fileInfo.FileData);

            result.Position = 0;
            return result;
        }
    }

    class X3CompressedStream : Stream
    {
        private const int BlockSize_ = 0x8000;

        private static readonly ICompression ZLib = Compressions.ZLib.Build();

        private readonly byte[] _buffer = new byte[4];
        private readonly Stream _baseStream;
        private readonly Stream _decompBuffer;

        private IList<Stream> _blocks;
        private long _position;

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => false;
        public override long Length { get; }
        public override long Position { get => _position; set => Seek(value, SeekOrigin.Begin); }

        public X3CompressedStream(Stream baseStream)
        {
            _baseStream = baseStream;
            _decompBuffer = new MemoryStream();

            EnsureBlocks();

            using var br = new BinaryReaderX(baseStream, true);
            var bkPos = baseStream.Position;

            // Read decompressed length
            baseStream.Position = 0;
            Length = br.ReadInt32();

            // Read compressed blocks
            _blocks = new List<Stream>();
            while (baseStream.Position < baseStream.Length)
            {
                var blockSize = br.ReadInt32();
                if (blockSize == 0)
                    break;

                _blocks.Add(new SubStream(baseStream, baseStream.Position, blockSize));
                baseStream.Position += blockSize;
            }

            baseStream.Position = bkPos;
        }

        public override void Flush()
        {
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    return _position = offset;

                case SeekOrigin.Current:
                    return _position += offset;

                case SeekOrigin.End:
                    throw new InvalidOperationException("Cannot set position outside the stream.");

                default:
                    throw new InvalidOperationException($"Unknown origin {origin}.");
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= Length)
                return 0;

            EnsureBlocks();

            var origPosition = _position;
            count = (int)Math.Min(count, Length - _position);

            while (count > 0)
            {
                var block = (int)(_position / BlockSize_);
                var blockPosition = (int)(_position % BlockSize_);

                if (block >= _blocks.Count)
                    break;

                var length = Math.Min(Math.Min(count, BlockSize_ - blockPosition), BlockSize_);

                // Decompress block
                _blocks[block].Position = 0;
                _decompBuffer.Position = 0;

                ZLib.Decompress(_blocks[block], _decompBuffer);

                length = (int)Math.Min(length, _decompBuffer.Position);
                _decompBuffer.Position = 0;
                _decompBuffer.Read(buffer, offset, length);

                // Update values
                count -= length;
                offset += length;
                _position += length;
            }

            return (int)(_position - origPosition);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        private void EnsureBlocks()
        {
            if (_blocks != null)
                return;

            var bkPos = _baseStream.Position;
            _baseStream.Position = 4;

            // Read compressed blocks
            _blocks = new List<Stream>();
            while (_baseStream.Position < _baseStream.Length)
            {
                _baseStream.Read(_buffer);
                var blockSize = BinaryPrimitives.ReadInt32LittleEndian(_buffer);
                if (blockSize == 0)
                    break;

                _blocks.Add(new SubStream(_baseStream, _baseStream.Position, blockSize));
                _baseStream.Position += blockSize;
            }

            _baseStream.Position = bkPos;
        }

        public static Stream Compress(Stream input)
        {
            var buffer = new byte[4];
            var result = new MemoryStream();

            // Write decompressed length
            BinaryPrimitives.WriteInt32LittleEndian(buffer, (int)input.Length);
            result.Write(buffer);

            // Write compressed blocks
            input.Position = 0;
            var count = input.Length;
            while (count > 0)
            {
                var length = Math.Min(count, BlockSize_);
                var startPos = result.Position;

                result.Position += 4;
                ZLib.Compress(new SubStream(input, input.Position, length), result);

                var endPos = result.Position;
                var compSize = endPos - startPos - 4;

                result.Position = startPos;
                BinaryPrimitives.WriteInt32LittleEndian(buffer, (int)compSize);
                result.Write(buffer);

                result.Position = endPos;
                count -= length;
                input.Position += length;
            }

            BinaryPrimitives.WriteInt32LittleEndian(buffer, 0);
            result.Write(buffer);

            result.Position = 0;
            return result;
        }
    }

    class X3Support
    {
        public static string DetermineExtension(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            switch (br.ReadString(4))
            {
                case "GT1G":
                    return ".g1t";

                case "_A1G":
                    return ".g1a";

                case "SMDH":
                    return ".icn";

                default:
                    return ".bin";
            }
        }
    }
}
