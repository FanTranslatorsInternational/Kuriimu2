using Komponent.Streams;
using Kompression;
using Kompression.Contract;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;
using Kryptography.Checksum.Crc;
using System.IO;

namespace plugin_bandai_namco.Archives
{
    class L7cHeader
    {
        public string magic = "L7CA";
        public int unk = 0x00010000; // Version? Must be 0x00010000
        public int archiveSize;
        public int fileInfoOffset;
        public int fileInfoSize;
        public int unk2 = 0x00010000; // Chunk max size?
        public int fileInfoCount;
        public int directoryCount;
        public int fileCount;
        public int chunkCount;
        public int stringTableSize;
        public int unk4;
    }

    class L7cFileInfoEntry
    {
        public int id;
        public uint hash; // Hash of filename
        public int folderNameOffset;
        public int fileNameOffset;
        public long timestamp;

        public bool IsFile => id >= 0;
    }

    class L7cFileEntry
    {
        public int compSize;
        public int decompSize;
        public int chunkIndex;
        public int chunkCount;
        public int offset;
        public uint crc32;  // Hash of file data
    }

    class L7cChunkEntry
    {
        public int chunkSize;
        public ushort unk = 0;
        public ushort chunkId;
    }

    class L7cArchiveFile : ArchiveFile
    {
        private static readonly Crc32 Crc32 = Crc32.Crc32B;

        private readonly ArchiveFileInfo _fileInfo;
        private readonly Stream _origStream;
        private readonly bool _usesCompression;

        public IList<L7cChunkEntry> Chunks { get; private set; }

        public L7cFileEntry Entry { get; }

        public L7cArchiveFile(ArchiveFileInfo fileInfo, IList<L7cChunkEntry> chunks, L7cFileEntry entry) : base(fileInfo)
        {
            Chunks = chunks;
            Entry = entry;

            _fileInfo = fileInfo;
            _origStream = fileInfo.FileData;

            var chunkStream = new ChunkStream(_origStream, entry.decompSize, ChunkInfo.ParseEntries(chunks));

            _fileInfo.FileData = chunkStream;
            _usesCompression = chunkStream.UsesCompression;
        }

        public long WriteFileData(Stream output)
        {
            // Write unchanged file
            if (!ContentChanged)
            {
                _origStream.Position = 0;
                _origStream.CopyTo(output);

                _fileInfo.ContentChanged = false;
                return _origStream.Length;
            }

            // Write newly chunked file
            _fileInfo.FileData.Position = 0;
            var chunkedStreamData = ChunkStream.CreateChunked(_fileInfo.FileData, _usesCompression, out var newChunks);
            chunkedStreamData.CopyTo(output);

            Chunks = newChunks;

            _fileInfo.FileData.Position = 0;
            Entry.crc32 = Crc32.ComputeValue(_fileInfo.FileData);

            _fileInfo.ContentChanged = false;
            return chunkedStreamData.Length;
        }
    }

    class ChunkStream : Stream
    {
        private const int BlockSize_ = 0x10000;

        private static readonly ICompression TaikoLz = Compressions.TaikoLz80.Build();

        private Stream _baseStream;

        private IList<ChunkInfo> _chunks;
        private Stream[] _decodedChunks;

        private long _length;
        private long _position;
        private int _currentChunk;

        public bool UsesCompression => _chunks.Any(x => x.Compression != null);

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public ChunkStream(Stream baseStream, int length, IList<ChunkInfo> chunks)
        {
            _baseStream = baseStream;

            foreach (var chunk in chunks)
            {
                if (chunk.Offset < 0 || chunk.Offset >= _baseStream.Length)
                    throw new InvalidOperationException("One chunk doesn't fit into the baseStream.");
                if (chunk.Offset + chunk.Length > _baseStream.Length)
                    throw new InvalidOperationException("One chunk doesn't fit into the baseStream.");
            }

            _chunks = chunks;
            _decodedChunks = new Stream[chunks.Count];
            _length = length;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _position = offset;
                    break;
                case SeekOrigin.Current:
                    _position += offset;
                    break;
                case SeekOrigin.End:
                    _position = _length + offset;
                    break;
            }

            if (_position >= _length)
                _currentChunk = -1;
            else
                _currentChunk = GetChunkByPosition(_position);

            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            count = (int)Math.Min(count, _length - _position);
            if (_currentChunk < 0 || _currentChunk >= _chunks.Count)
                return 0;

            var originalPosition = _baseStream.Position;
            var readBytes = 0;

            DecodeCurrentChunk();

            // Read either complete chunks or until the end of a chunk
            var relativePosition = GetChunkRelativePosition();
            while (_currentChunk < _chunks.Count && relativePosition + count >= _chunks[_currentChunk].Length)
            {
                var length = (int)(_chunks[_currentChunk].Length - relativePosition);
                ReadBufferFromChunk(buffer, offset, length, _currentChunk, relativePosition);

                _position += length;
                count -= length;
                offset += length;
                readBytes += length;
                relativePosition = 0;
                _currentChunk++;

                DecodeCurrentChunk();
            }

            // Read the remaining data not concluding with a chunk
            ReadBufferFromChunk(buffer, offset, count, _currentChunk, relativePosition);

            Seek(_position + count, SeekOrigin.Begin);
            readBytes += count;

            _baseStream.Position = originalPosition;

            return readBytes;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        private void DecodeCurrentChunk()
        {
            if (_currentChunk < 0 || _currentChunk >= _chunks.Count)
                return;

            if (_decodedChunks[_currentChunk] == null && _chunks[_currentChunk].Compression != null)
            {
                // Decompress chunk when read the first time only
                _decodedChunks[_currentChunk] = new MemoryStream();
                _chunks[_currentChunk].Compression.Decompress(
                    new SubStream(_baseStream, _chunks[_currentChunk].Offset, _chunks[_currentChunk].Length),
                    _decodedChunks[_currentChunk]);

                _chunks[_currentChunk].Length = _decodedChunks[_currentChunk].Length;
            }
        }

        private void ReadBufferFromChunk(byte[] buffer, int offset, int length, int chunk, long relativePosition)
        {
            if (_currentChunk < 0 || _currentChunk >= _chunks.Count)
                return;

            if (_decodedChunks[chunk] != null)
            {
                _decodedChunks[chunk].Position = relativePosition;
                _decodedChunks[chunk].Read(buffer, offset, length);
            }
            else
            {
                _baseStream.Position = GetAbsolutePositionByChunk(chunk);
                _baseStream.Read(buffer, offset, length);
            }
        }

        private int GetChunkByPosition(long position)
        {
            var chunkId = 0;
            while (position >= _chunks[chunkId].Length)
                position -= _chunks[chunkId++].Length;

            return chunkId;
        }

        private long GetAbsolutePositionByChunk(int chunk)
        {
            if (_currentChunk < 0 || _currentChunk >= _chunks.Count)
                return -1;

            var summedLength = 0L;
            for (var i = 0; i < chunk; i++)
                summedLength += _chunks[i].Length;

            return _chunks[chunk].Offset + (_position - summedLength);
        }

        private long GetChunkRelativePosition()
        {
            var summedLength = 0L;
            for (int i = 0; i < _currentChunk; i++)
                summedLength += _chunks[i].Length;

            return _position - summedLength;
        }

        public static Stream CreateChunked(Stream input, bool compress, out IList<L7cChunkEntry> chunks)
        {
            var result = new MemoryStream();
            chunks = new List<L7cChunkEntry>();
            var flag = compress ? 0x80000000 : 0;

            // Write compressed blocks
            input.Position = 0;
            var count = input.Length;

            var chunkId = 0;
            while (count > 0)
            {
                var length = Math.Min(count, BlockSize_);
                var startPos = result.Position;

                var block = new SubStream(input, input.Position, length);

                if (compress)
                {
                    // TODO: Do not rely on buffer stream. Compression should be position agnostic
                    var buffer = new MemoryStream();
                    TaikoLz.Compress(block, buffer);

                    buffer.Position = 0;
                    buffer.CopyTo(result);
                }
                else
                    block.CopyTo(result);

                chunks.Add(new L7cChunkEntry
                {
                    chunkSize = (int)(flag | (result.Length - startPos)),
                    chunkId = (ushort)chunkId++
                });

                count -= length;
                input.Position += length;
            }

            result.Position = 0;
            return result;
        }
    }

    class ChunkInfo
    {
        public long Offset { get; }
        public long Length { get; set; }
        public ICompression Compression { get; }

        private ChunkInfo(long offset, L7cChunkEntry entry)
        {
            Offset = offset;
            Length = entry.chunkSize & 0xFFFFFF;

            switch ((entry.chunkSize >> 24) & 0xFF)
            {
                case 0x80:
                    Compression = Compressions.TaikoLz80.Build();
                    break;

                case 0x81:
                    Compression = Compressions.TaikoLz81.Build();
                    break;
            }
        }

        public static IList<ChunkInfo> ParseEntries(IList<L7cChunkEntry> entries)
        {
            var result = new List<ChunkInfo>();

            var offset = 0L;
            foreach (var entry in entries)
            {
                var chunkInfo = new ChunkInfo(offset, entry);
                result.Add(chunkInfo);

                offset += chunkInfo.Length;
            }

            return result;
        }
    }
}
