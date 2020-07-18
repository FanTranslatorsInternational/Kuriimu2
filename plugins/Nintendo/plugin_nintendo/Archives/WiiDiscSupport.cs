using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Attributes;
using Komponent.IO.Streams;
using Kontract;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    class WiiDiscHeader
    {
        public byte wiiDiscId;
        public short gameCode;
        public byte regionCode;
        public short makerCode;
        public byte discNumber;
        public byte discVersion;
        public bool enableAudioStreaming;
        public byte streamBufferSize;

        [FixedLength(0xE)]
        public byte[] zero0;

        public uint wiiMagicWord;       // 0x5D1C9EA3
        public uint gameCubeMagicWord;  // 0xC2339F3D

        [FixedLength(0x40)]
        public string gameTitle;

        public bool disableHashVerification;
        public bool disableDecryption;
    }

    class WiiDiscPartitionInformation
    {
        public int partitionCount1;
        public int partitionOffset1;
        public int partitionCount2;
        public int partitionOffset2;
        public int partitionCount3;
        public int partitionOffset3;
        public int partitionCount4;
        public int partitionOffset4;
    }

    class WiiDiscPartitionEntry
    {
        public int offset;
        public int type;
    }

    class WiiDiscRegionSettings
    {
        public int region;

        [FixedLength(0xC)]
        public byte[] zero0;

        public byte japanAgeRating;
        public byte usaAgeRating;

        public byte zero1;

        public byte germanAgeRating;
        public byte pegiAgeRating;
        public byte finlandAgeRating;
        public byte portugalAgeRating;
        public byte britainAgeRating;
        public byte australiaAgeRating;
        public byte koreaAgeRating;

        [FixedLength(0x6)]
        public byte[] zero2;
    }

    class WiiDiscPartitionHeader
    {
        public WiiDiscPartitionTicket ticket;
        public int tmdOffset;
        public int tmdSize;
        public int certChainOffset;
        public int certChainSize;
        public int h3Offset;
        public int dataOffset;
        public int dataSize;
        public WiiDiscPartitionTmd tmd;
    }

    class WiiDiscPartitionTicket
    {
        public int signatureType;
        [FixedLength(0x100)]
        public byte[] signature;
        [FixedLength(0x3C)]
        public byte[] padding;
        [FixedLength(0x40)]
        public string issuer;
        [FixedLength(0x3C)]
        public byte[] ecdhData;
        [FixedLength(0x3)]
        public byte[] zero0;
        [FixedLength(0x10)]
        public byte[] encryptedTitleKey;
        public byte unk0;
        [FixedLength(0x8)]
        public byte[] ticketId;
        public int consoleId;
        [FixedLength(0x8)]
        public byte[] titleId;
        public short unk1;
        public short ticketTitleVersion;
        public uint permittedTitlesMask;
        public uint permitMask;
        public bool isTitleExportAllowed;
        public byte commonKeyIndex;
        [FixedLength(0x30)]
        public byte[] unk2;
        [FixedLength(0x40)]
        public byte[] contentAccessPermissions;
        public short zero1;
        [FixedLength(0x8)]
        public WiiDiscPartitionTimeLimit[] timeLimits;
    }

    class WiiDiscPartitionTimeLimit
    {
        public int enableTimeLimit;
        public int limitSeconds;
    }

    class WiiDiscPartitionTmd
    {
        public int signatureType;
        [FixedLength(0x100)]
        public byte[] signature;
        [FixedLength(0x3C)]
        public byte[] padding;
        [FixedLength(0x40)]
        public string issuer;
        public byte version;
        public byte caCrlVersion;
        public byte signerCrlVersion;
        public bool isVWii;
        public long iosVersion;
        [FixedLength(0x8)]
        public byte[] titleId;
        public int titleType;
        public short groupId;
        public short zero0;
        public short region;
        [FixedLength(0x10)]
        public byte[] ratings;
        [FixedLength(0xC)]
        public byte[] zero1;
        [FixedLength(0xC)]
        public byte[] ipcMask;
        [FixedLength(0x12)]
        public byte[] zero2;
        public uint accessRights;
        public short titleVersion;
        public short contentCount;
        public short bootIndex;
        public short zero3;
        [VariableLength(nameof(contentCount))]
        public WiiDiscPartitionTmdContent[] contents;
    }

    class WiiDiscPartitionTmdContent
    {
        public int contentId;
        public short index;
        public short type;
        public long size;
        [FixedLength(0x14)]
        public byte[] hash;
    }

    class U8Entry
    {
        public int tmp1;
        public int offset;
        public int size;

        public bool IsDirectory
        {
            get => tmp1 >> 24 == 1;
            set => tmp1 = (tmp1 & 0xFFFFFF) | ((value ? 1 : 0) << 24);
        }

        public int NameOffset
        {
            get => tmp1 & 0xFFFFFF;
            set => tmp1 = (tmp1 & ~0xFFFFFF) | (value & 0xFFFFFF);
        }
    }

    class U8FileSystem
    {
        private BinaryReaderX _nameReader;
        private int _index;
        private int _fileOffsetStart;

        private UPath _root;

        public U8FileSystem(UPath root)
        {
            _root = root;
        }

        public IEnumerable<ArchiveFileInfo> Parse(Stream input, long fileSystemOffset, int fileSystemSize, int fileOffsetStart)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);
            br.BaseStream.Position = fileSystemOffset;

            // Get root entry
            var root = br.ReadType<U8Entry>();

            // Get name stream
            var entriesSize = root.size * 0xC;
            var nameStream = new SubStream(input, fileSystemOffset + entriesSize, fileSystemSize - entriesSize);
            _nameReader = new BinaryReaderX(nameStream);

            // Parse entries
            _fileOffsetStart = fileOffsetStart;
            br.BaseStream.Position = fileSystemOffset;
            var entries = br.ReadMultiple<U8Entry>(root.size);
            return ParseDirectory(input, entries);
        }

        private IEnumerable<ArchiveFileInfo> ParseDirectory(Stream input, IList<U8Entry> entries)
        {
            var rootEntry = entries[0];
            var endIndex = rootEntry.size;
            _index = 1;

            return ParseDirectory(input, entries, _root, endIndex);
        }

        private IEnumerable<ArchiveFileInfo> ParseDirectory(Stream input, IList<U8Entry> entries,
            UPath path, int endIndex)
        {
            while (_index < endIndex)
            {
                var entry = entries[_index++];

                _nameReader.BaseStream.Position = entry.NameOffset;
                var nodeName = _nameReader.ReadCStringASCII();

                if (entry.IsDirectory)
                {
                    foreach (var file in ParseDirectory(input, entries, path / nodeName, entry.size))
                        yield return file;
                    continue;
                }

                var subStream = new SubStream(input, (long)entry.offset<<2, entry.size);
                yield return new ArchiveFileInfo(subStream, (path / nodeName).FullName);
            }
        }
    }

    class U8TreeBuilder
    {
        private Encoding _nameEncoding;
        private BinaryWriterX _nameBw;

        public IList<(U8Entry, ArchiveFileInfo)> Entries { get; private set; }

        public Stream NameStream { get; private set; }

        public U8TreeBuilder(Encoding nameEncoding)
        {
            _nameEncoding = nameEncoding;
        }

        public void Build(IList<(string path, ArchiveFileInfo afi)> files)
        {
            // Build directory tree
            var directoryTree = BuildDirectoryTree(files);

            // Create name stream
            NameStream = new MemoryStream();
            _nameBw = new BinaryWriterX(NameStream, true);

            // Populate entries
            Entries = new List<(U8Entry, ArchiveFileInfo)>();
            PopulateEntryList(files, directoryTree, 0);
        }

        private IList<(string, int)> BuildDirectoryTree(IList<(string, ArchiveFileInfo)> files)
        {
            var distinctDirectories = files
                .OrderBy(x => GetDirectory(x.Item1))
                .Select(x => GetDirectory(x.Item1))
                .Distinct();

            var directories = new List<(string, int)> { ("/", -1) };
            foreach (var directory in distinctDirectories)
            {
                var splittedDirectory = SplitPath(directory);
                for (var i = 0; i < splittedDirectory.Length; i++)
                {
                    var parentDirectory = "/" + Combine(splittedDirectory.Take(i));
                    var currentDirectory = "/" + Combine(splittedDirectory.Take(i + 1));

                    if (directories.Any(x => x.Item1 == currentDirectory))
                        continue;

                    var index = directories.FindIndex(x => x.Item1 == parentDirectory);
                    directories.Add((currentDirectory, index));
                }
            }

            return directories;
        }

        private void PopulateEntryList(IList<(string path, ArchiveFileInfo afi)> files,
            IList<(string, int)> directories, int parentIndex)
        {
            var directoryIndex = 0;
            while (directoryIndex < directories.Count)
            {
                var currentDirectory = directories[directoryIndex];

                // Write directory name
                var directoryNameOffset = (int)_nameBw.BaseStream.Position;
                var splittedDirectoryName = SplitPath(currentDirectory.Item1);
                _nameBw.WriteString(splittedDirectoryName.Any() ? GetName(currentDirectory.Item1) : string.Empty, _nameEncoding, false);

                // Add directory entry
                var currentDirectoryIndex = Entries.Count;
                var currentDirectoryEntry = new U8Entry
                {
                    IsDirectory = true,
                    NameOffset = directoryNameOffset,
                    offset = parentIndex
                };
                Entries.Add((currentDirectoryEntry, null));

                // Add file entries
                var filesInDirectory = files.Where(x => GetDirectory(x.path) == currentDirectory.Item1);
                foreach (var file in filesInDirectory)
                {
                    // Write file name
                    var nameOffset = (int)_nameBw.BaseStream.Position;
                    _nameBw.WriteString(GetName(file.path), _nameEncoding, false);

                    // Add file entry
                    var fileEntry = new U8Entry
                    {
                        IsDirectory = false,
                        NameOffset = nameOffset
                    };
                    Entries.Add((fileEntry, file.afi));
                }

                // Add sub directories
                var subDirectories = directories
                    .Where(x => x != currentDirectory &&
                                x.Item1.StartsWith(currentDirectory.Item1))
                    .ToArray();
                PopulateEntryList(files, subDirectories, currentDirectoryIndex);

                // Edit size of directory
                currentDirectoryEntry.size = Entries.Count;

                directoryIndex += subDirectories.Length + 1;
            }
        }

        private string GetDirectory(string path)
        {
            if (path.EndsWith("/"))
                path = path.Substring(0, path.Length - 1);

            var splitted = path.Split("/");
            return string.Join("/", splitted.Take(splitted.Length - 1));
        }

        private string GetName(string path)
        {
            if (path.EndsWith("/"))
                return string.Empty;

            return path.Split("/").Last();
        }

        private string[] SplitPath(string path)
        {
            if (path.EndsWith("/"))
                path = path.Substring(0, path.Length - 1);

            return path.Split("/", StringSplitOptions.RemoveEmptyEntries);
        }

        private string Combine(IEnumerable<string> parts)
        {
            return string.Join('/', parts);
        }
    }

    class WiiDiscPartitionDataStream : Stream
    {
        private const int BlockSize_ = 0x8000;
        private const int HashBlockSize_ = 0x400;
        private const int DataBlockSize_ = 0x7C00;

        private readonly Stream _baseStream;

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _baseStream.Length / BlockSize_ * DataBlockSize_;
        public override long Position { get; set; }

        public WiiDiscPartitionDataStream(Stream baseStream)
        {
            ContractAssertions.IsNotNull(baseStream, nameof(baseStream));

            if (baseStream.Length % BlockSize_ != 0)
                throw new InvalidOperationException($"The given stream needs to be aligned to 0x{BlockSize_:X4}");

            _baseStream = baseStream;
        }

        public override void Flush()
        {
            // TODO: Flush after write
            _baseStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    return Position = offset;

                case SeekOrigin.Current:
                    return Position += offset;

                case SeekOrigin.End:
                    return Position = Length + offset;
            }

            throw new ArgumentException("Origin is invalid.");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bkPos = _baseStream.Position;

            var readBytes = 0;
            while (count > 0 && Position < Length)
                readBytes += ReadNextDataBlock(buffer, ref offset, ref count);

            _baseStream.Position = bkPos;
            return readBytes;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        private int ReadNextDataBlock(byte[] buffer, ref int offset, ref int count)
        {
            var blockPosition = Position % DataBlockSize_ + HashBlockSize_;
            var blockStart = Position / DataBlockSize_ * BlockSize_;

            var length = (int)Math.Min(BlockSize_ - blockPosition, count);
            _baseStream.Position = blockStart + blockPosition;
            var readBytes = _baseStream.Read(buffer, offset, length);

            Position += length;
            offset += length;
            count -= length;

            return readBytes;
        }
    }
}
