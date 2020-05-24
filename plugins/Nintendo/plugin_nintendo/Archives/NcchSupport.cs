using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Komponent.IO;
using Komponent.IO.Attributes;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{

    class NcchHeader
    {
        [FixedLength(0x100)]
        public byte[] rsa2048;
        [FixedLength(4)]
        public string magic;
        public int ncchSize;
        public ulong partitionId;
        public short makerCode;
        public short version;
        public uint seedHashVerifier;
        public ulong programID;
        [FixedLength(0x10)]
        public byte[] reserved1;
        [FixedLength(0x20)]
        public byte[] logoRegionHash;
        [FixedLength(0x10)]
        public byte[] productCode;
        [FixedLength(0x20)]
        public byte[] exHeaderHash;
        public int exHeaderSize;
        public int reserved2;
        [FixedLength(0x8)]
        public byte[] ncchFlags;
        public int plainRegionOffset;
        public int plainRegionSize;
        public int logoRegionOffset;
        public int logoRegionSize;
        public int exeFsOffset;
        public int exeFsSize;
        public int exeFsHashRegionSize;
        public int reserved3;
        public int romFsOffset;
        public int romFsSize;
        public int romFsHashRegionSize;
        public int reserved4;
        [FixedLength(0x20)]
        public byte[] exeFsSuperBlockHash;
        [FixedLength(0x20)]
        public byte[] romFsSuperBlockHash;
    }

    class NcchExeFsHeader
    {
        [FixedLength(0xA)]
        public NcchExeFsFileEntry[] fileEntries;
        [FixedLength(0x20)]
        public byte[] reserved1;
        [FixedLength(0xA)]
        public NcchExeFsFileEntryHash[] fileEntryHashes;
    }

    class NcchExeFsFileEntry
    {
        [FixedLength(8)]
        public string name;
        public int offset;
        public int size;
    }

    class NcchExeFsFileEntryHash
    {
        [FixedLength(0x20)]
        public byte[] hash;
    }

    class NcchRomFs
    {
        NcchRomFsLevelHeader lv3Header;
        long lv3Offset;

        public NcchRomFsHeader header;
        public byte[] masterHash;

        public List<FinalFileInfo> Files { get; }

        public NcchRomFs(Stream instream)
        {
            using var br = new BinaryReaderX(instream, true);

            // Read header
            var romFsOffset = br.BaseStream.Position;
            header = br.ReadType<NcchRomFsHeader>();
            masterHash = br.ReadBytes(header.masterHashSize);

            // Read Level 3
            lv3Offset = romFsOffset + 0x1000;
            br.BaseStream.Position = lv3Offset;
            lv3Header = br.ReadType<NcchRomFsLevelHeader>();

            // Resolve file and directory tree
            br.BaseStream.Position = lv3Offset + lv3Header.dirMetaTableOffset;
            Files = new List<FinalFileInfo>();
            ResolveDirectories(br);
        }

        private void ResolveDirectories(BinaryReaderX br, string currentPath = "")
        {
            var currentDirEntry = br.ReadType<NcchRomFsDirectoryMetaData>();

            // First go through all sub dirs
            if (currentDirEntry.firstChildDirOffset != -1)
            {
                br.BaseStream.Position = lv3Offset + lv3Header.dirMetaTableOffset + currentDirEntry.firstChildDirOffset;
                ResolveDirectories(br, currentPath + currentDirEntry.name + "/");
            }

            // Then get all Files of current directory
            if (currentDirEntry.firstFileOffset != -1)
            {
                var fileOffset = currentDirEntry.firstFileOffset;

                // Move through sibling Files without recursion
                NcchRomFsFileMetaData currentFileEntry;
                do
                {
                    br.BaseStream.Position = lv3Offset + lv3Header.fileMetaTableOffset + fileOffset;
                    currentFileEntry = br.ReadType<NcchRomFsFileMetaData>();

                    // Add current file
                    Files.Add(new FinalFileInfo
                    {
                        filePath = currentPath + currentDirEntry.name + "/" + currentFileEntry.name,
                        fileOffset = lv3Offset + lv3Header.fileDataOffset + currentFileEntry.fileOffset,
                        fileSize = currentFileEntry.fileSize
                    });

                    fileOffset = currentFileEntry.nextSiblingFileOffset;
                } while (currentFileEntry.nextSiblingFileOffset != -1);
            }

            // Move to next sibling directory
            if (currentDirEntry.nextSiblingDirOffset != -1)
            {
                br.BaseStream.Position = lv3Offset + lv3Header.dirMetaTableOffset + currentDirEntry.nextSiblingDirOffset;
                ResolveDirectories(br, currentPath);
            }
        }

        [DebuggerDisplay("{filePath}")]
        public class FinalFileInfo
        {
            public string filePath;
            public long fileOffset;
            public long fileSize;
        }
    }

    [Alignment(0x10)]
    class NcchRomFsHeader
    {
        [FixedLength(4)]
        public string magic = "IVFC";
        public int magicNumber = 0x10000;
        public int masterHashSize;
        public long lv1LogicalOffset;
        public long lv1HashDataSize;
        public int lv1BlockSize = 0xC;
        public int reserved1 = 0;
        public long lv2LogicalOffset;
        public long lv2HashDataSize;
        public int lv2BlockSize = 0xC;
        public int reserved2 = 0;
        public long lv3LogicalOffset;
        public long lv3HashDataSize;
        public int lv3BlockSize = 0xC;
        public int reserved3 = 0;
        public int headerLength = 0x5C;
        public int infoSize = 0;
    }

    class NcchRomFsLevelHeader
    {
        public int headerLength;
        public int dirHashTableOffset;
        public int dirHashTableSize;
        public int dirMetaTableOffset;
        public int dirMetaTableSize;
        public int fileHashTableOffset;
        public int fileHashTableSize;
        public int fileMetaTableOffset;
        public int fileMetaTableSize;
        public int fileDataOffset;
    }

    class NcchRomFsDirectoryMetaData
    {
        public int parentDirOffset;
        public int nextSiblingDirOffset;
        public int firstChildDirOffset;
        public int firstFileOffset;
        public int nextDirInSameBucketOffset;
        public int nameLength;
        [VariableLength("nameLength", StringEncoding = StringEncoding.Unicode)]
        public string name;
    }

    class NcchRomFsFileMetaData
    {
        public int containingDirOffset;
        public int nextSiblingFileOffset;
        public long fileOffset;
        public long fileSize;
        public int nextFileInSameBucketOffset;
        public int nameLength;
        [VariableLength("nameLength", StringEncoding = StringEncoding.Unicode)]
        public string name;
    }

    public static class ExeFsBuilder
    {
        private static int _exeFsHeaderSize = Tools.MeasureType(typeof(NcchExeFsHeader));
        private static int _exeFsFileEntrySize = Tools.MeasureType(typeof(NcchExeFsFileEntry));
        private static int _exeFsFileEntryHashSize = Tools.MeasureType(typeof(NcchExeFsFileEntryHash));

        private const int MediaSize_ = 0x200;
        private const int MaxFiles_ = 0xA;

        public static long Build(Stream output, IList<ArchiveFileInfo> files)
        {
            var sha256 = new Kryptography.Hash.Sha256();
            using var bw = new BinaryWriterX(output, true);

            var inOffset = output.Position;

            // Write file data
            bw.BaseStream.Position = inOffset + _exeFsHeaderSize;
            var filePosition = bw.BaseStream.Position;

            IList<NcchExeFsFileEntry> fileEntries = new List<NcchExeFsFileEntry>(MaxFiles_);
            IList<NcchExeFsFileEntryHash> fileHashes = new List<NcchExeFsFileEntryHash>(MaxFiles_);
            var fileOffset = 0;
            foreach (var file in files)
            {
                var writtenSize = file.SaveFileData(bw.BaseStream);

                bw.WriteAlignment(MediaSize_);

                fileEntries.Add(new NcchExeFsFileEntry
                {
                    name = file.FilePath.GetName(),
                    offset = fileOffset,
                    size = (int)writtenSize
                });
                fileHashes.Add(new NcchExeFsFileEntryHash
                {
                    hash = sha256.Compute(new SubStream(output, filePosition + fileOffset, writtenSize))
                });

                fileOffset = (int)(bw.BaseStream.Position - filePosition);
            }

            var finalSize = bw.BaseStream.Position - inOffset;

            // Write file entries
            bw.BaseStream.Position = inOffset;
            bw.WriteMultiple(fileEntries);
            bw.WritePadding(_exeFsFileEntrySize * (MaxFiles_ - fileEntries.Count));

            // Write reserved data
            bw.WritePadding(0x20);

            // Write file entry hashes
            bw.WritePadding(_exeFsFileEntryHashSize * (MaxFiles_ - fileEntries.Count));
            bw.WriteMultiple(fileHashes.Reverse());

            output.Position = inOffset + finalSize;
            return finalSize;
        }
    }

    public static class RomFsBuilder
    {
        private const int UnusedEntry_ = -1;
        private const int BlockSize_ = 0x1000;

        public const int RomFsHeaderSize = 0x60;

        class MetaData
        {
            public int DirMetaOffset { get; set; }
            public int FileMetaOffset { get; set; }
            public int FileOffset { get; set; }

            public List<DirEntry> Dirs = new List<DirEntry>();
            public uint[] DirHashTable;
            public List<FileEntry> Files = new List<FileEntry>();
            public uint[] FileHashTable;

            public class DirEntry
            {
                public const int Size = 6 * 0x4;

                public int MetaOffset;
                public uint Hash;

                public int ParentOffset;
                public int NextSiblingOffset;
                public int FirstChildOffset;
                public int FirstFileOffset;

                public int? NextDirInSameBucket;

                public string Name;

                public int GetSize(int alignment)
                {
                    var size = 0;
                    if (!string.IsNullOrEmpty(Name))
                        size += Encoding.Unicode.GetByteCount(Name);

                    return (size + Size + (alignment - 1)) & ~(alignment - 1);
                }

                public override string ToString()
                {
                    return Name;
                }
            }

            public class FileEntry
            {
                public const int Size = 4 * 0x4 + 2 * 0x8;

                public Stream FileData;

                public int MetaOffset;
                public uint Hash;

                public int ParentDirOffset;
                public int NextSiblingOffset;
                public long DataOffset;
                public long DataSize;

                public int? NextFileInSameBucket;

                public string Name;

                public int GetSize(int alignment)
                {
                    var size = 0;
                    if (!string.IsNullOrEmpty(Name))
                        size += Encoding.Unicode.GetByteCount(Name);

                    return (size + Size + (alignment - 1)) & ~(alignment - 1);
                }

                public override string ToString()
                {
                    return Name;
                }
            }
        }

        public class IntDirectory
        {
            private readonly List<IntDirectory> _directories = new List<IntDirectory>();
            private readonly List<ArchiveFileInfo> _filesInDirectory = new List<ArchiveFileInfo>();

            public string DirectoryName { get; set; }
            public UPath DirectoryPath { get; set; }

            public void AddFiles(params ArchiveFileInfo[] files)
            {
                _filesInDirectory.AddRange(files);
            }

            public void AddDirectory(IntDirectory dir)
            {
                _directories.Add(dir);
            }

            public IList<ArchiveFileInfo> GetFiles()
            {
                return _filesInDirectory;
            }

            public IList<IntDirectory> GetDirectories()
            {
                return _directories;
            }

            public override string ToString()
            {
                return DirectoryName;
            }
        }

        public static (long, long) Build(Stream input, IList<ArchiveFileInfo> files, UPath rootDirectory)
        {
            // Parse files into file tree
            var treeRoot = ParseFileTree(files, rootDirectory);

            // Create MetaData Tree
            var metaData = new MetaData
            {
                DirMetaOffset = treeRoot.GetDirectories().Count <= 0 ? UnusedEntry_ : 0x18
            };
            metaData.Dirs.Add(new MetaData.DirEntry
            {
                MetaOffset = 0,
                ParentOffset = 0,
                NextSiblingOffset = UnusedEntry_,
                FirstChildOffset = treeRoot.GetDirectories().Count <= 0 ? UnusedEntry_ : 0x18,
                FirstFileOffset = 0,
                NextDirInSameBucket = UnusedEntry_,
                Name = string.Empty
            });

            PopulateMetaData(metaData, treeRoot, metaData.Dirs[0]);

            // Creating directory hash buckets
            metaData.DirHashTable = Enumerable.Repeat(0xFFFFFFFF, GetHashTableEntryCount(metaData.Dirs.Count)).ToArray();
            PopulateDirHashTable(metaData.Dirs, metaData.DirHashTable);

            // Creating file hash buckets
            metaData.FileHashTable = Enumerable.Repeat(0xFFFFFFFF, GetHashTableEntryCount(metaData.Files.Count)).ToArray();
            PopulateFileHashTable(metaData.Files, metaData.FileHashTable);

            // Write RomFs
            var romFsSizes = WriteRomFs(input, metaData);

            return romFsSizes;
        }

        /// <summary>
        /// Parses all given files into a file tree.
        /// </summary>
        /// <param Name="files">The files to parse.</param>
        /// <param Name="currentPath">The path to parse.</param>
        /// <returns>The root directory of the file tree.</returns>
        private static IntDirectory ParseFileTree(IList<ArchiveFileInfo> files, UPath currentPath)
        {
            var currentAbsolutePath = currentPath.ToAbsolute();

            // Create current directory entry
            var currentDirectory = new IntDirectory
            {
                DirectoryName = currentPath.Split().LastOrDefault(),
                DirectoryPath = currentPath
            };

            // Add files of current directory
            var filesInDirectory = files.Where(x => x.FilePath.IsInDirectory(currentAbsolutePath, false)).ToArray();
            currentDirectory.AddFiles(filesInDirectory);

            // Add directories of current directories
            var directoriesInDirectory = files.Where(x => x.FilePath.IsInDirectory(currentAbsolutePath, true))
                .Except(filesInDirectory)
                .Select(x => x.FilePath.GetDirectory().GetSubDirectory(currentAbsolutePath).GetFirstDirectory(out _))
                .Distinct();
            foreach (var subDirectory in directoriesInDirectory)
            {
                var filesInSubDirectory = files.Where(x => x.FilePath.IsInDirectory(currentAbsolutePath / subDirectory, true)).ToArray();
                var parsedDirectory = ParseFileTree(filesInSubDirectory, currentPath / subDirectory);

                currentDirectory.AddDirectory(parsedDirectory);
            }

            return currentDirectory;
        }

        /// <summary>
        /// Populates the meta data tree.
        /// </summary>
        /// <param Name="metaData">The meta data to populate.</param>
        /// <param Name="dir">The directory to populate the meta data with.</param>
        /// <param Name="parentDir">The parent directory meta data.</param>
        private static void PopulateMetaData(MetaData metaData, IntDirectory dir, MetaData.DirEntry parentDir)
        {
            // Adding files
            var files = dir.GetFiles();
            for (var i = 0; i < files.Count; i++)
            {
                var newFileEntry = new MetaData.FileEntry
                {
                    MetaOffset = metaData.FileMetaOffset,
                    Hash = CalculatePathHash((uint)parentDir.MetaOffset, Encoding.Unicode.GetBytes(files[i].FilePath.GetName())),

                    FileData = files[i].GetFileData().Result,

                    ParentDirOffset = parentDir.MetaOffset,
                    DataOffset = metaData.FileOffset,
                    DataSize = (int)files[i].FileSize,
                    Name = files[i].FilePath.GetName()
                };
                metaData.FileOffset += (int)files[i].FileSize;

                metaData.FileMetaOffset += MetaData.FileEntry.Size + files[i].FilePath.GetName().Length * 2;
                if (metaData.FileMetaOffset % 4 != 0)
                    metaData.FileMetaOffset += 2;

                newFileEntry.NextSiblingOffset = i + 1 == files.Count ? UnusedEntry_ : metaData.FileMetaOffset;

                metaData.Files.Add(newFileEntry);
            }

            // Adding sub directories
            var dirs = dir.GetDirectories();
            var metaDirIndices = new List<int>();
            for (var i = 0; i < dirs.Count; i++)
            {
                var newDirEntry = new MetaData.DirEntry
                {
                    //Parent = parentDir,

                    MetaOffset = metaData.DirMetaOffset,
                    Hash = CalculatePathHash((uint)parentDir.MetaOffset, Encoding.Unicode.GetBytes(dirs[i].DirectoryName)),

                    ParentOffset = parentDir.MetaOffset,

                    Name = dirs[i].DirectoryName
                };

                metaData.DirMetaOffset += MetaData.DirEntry.Size + dirs[i].DirectoryName.Length * 2;
                if (metaData.DirMetaOffset % 4 != 0)
                    metaData.DirMetaOffset += 2;

                newDirEntry.NextSiblingOffset = i + 1 < dirs.Count ? metaData.DirMetaOffset : UnusedEntry_;

                metaData.Dirs.Add(newDirEntry);
                metaDirIndices.Add(metaData.Dirs.Count - 1);
            }

            // Adding children of sub directories
            for (var i = 0; i < dirs.Count; i++)
            {
                metaData.Dirs[metaDirIndices[i]].FirstChildOffset = dirs[i].GetDirectories().Count > 0 ? metaData.DirMetaOffset : UnusedEntry_;
                metaData.Dirs[metaDirIndices[i]].FirstFileOffset = dirs[i].GetFiles().Count > 0 ? metaData.FileMetaOffset : UnusedEntry_;

                PopulateMetaData(metaData, dirs[i], metaData.Dirs[metaDirIndices[i]]);
            }
        }

        /// <summary>
        /// Write the RomFs structure.
        /// </summary>
        /// <param name="output">The stream to write to.</param>
        /// <param name="metaData">The meta data for the RomFs.</param>
        /// <returns>The size of the RomFs partition and its header size.</returns>
        private static (long, long) WriteRomFs(Stream output, MetaData metaData)
        {
            var romFsPosition = output.Position;
            var masterHashPosition = romFsPosition + 0x60;
            var metaDataPosition = romFsPosition + BlockSize_;

            // Write meta data
            output.Position = metaDataPosition;
            var metaDataSize = WriteMetaData(output, metaData);

            // Write IVFC levels
            var levelData = WriteIvfcLevels(output, metaDataPosition, metaDataSize, masterHashPosition, 3);

            // Write RomFs header
            using var bw = new BinaryWriterX(output, true);

            bw.BaseStream.Position = romFsPosition;
            bw.WriteType(new NcchRomFsHeader
            {
                masterHashSize = (int)levelData[2].Item2,

                lv1LogicalOffset = 0,
                lv1HashDataSize = levelData[1].Item2,

                lv2LogicalOffset = levelData[1].Item3,
                lv2HashDataSize = levelData[0].Item2,

                lv3LogicalOffset = levelData[1].Item3 + levelData[0].Item3,
                lv3HashDataSize = metaDataSize
            });

            var romFsSize = levelData[0].Item1 + levelData[0].Item3 - romFsPosition;
            var romFsHeaderSize = 0x60 + levelData[2].Item2;

            return (romFsSize, romFsHeaderSize);
        }

        /// <summary>
        /// Write meta data tree.
        /// </summary>
        /// <param name="output">The stream to write to.</param>
        /// <param name="metaData">The meta data tree to write.</param>
        /// <returns>The size of the written meta data.</returns>
        private static long WriteMetaData(Stream output, MetaData metaData)
        {
            var startPosition = output.Position;
            using var bw = new BinaryWriterX(output, true);

            var header = new NcchRomFsLevelHeader
            {
                headerLength = 0x28,
                dirHashTableOffset = 0x28,
                dirHashTableSize = metaData.DirHashTable.Length * sizeof(uint),
                dirMetaTableSize = metaData.Dirs.Sum(dir => dir.GetSize(4)),
                fileHashTableSize = metaData.FileHashTable.Length * sizeof(uint),
                fileMetaTableSize = metaData.Files.Sum(file => file.GetSize(4)),
            };
            bw.BaseStream.Position = startPosition + 0x28;

            // Write directory hash table
            foreach (var hash in metaData.DirHashTable)
                bw.Write(hash);

            header.dirMetaTableOffset = (int)(bw.BaseStream.Position - startPosition);

            // Write directory directoryEntries
            foreach (var dir in metaData.Dirs)
            {
                bw.Write(dir.ParentOffset);
                bw.Write(dir.NextSiblingOffset);
                bw.Write(dir.FirstChildOffset);
                bw.Write(dir.FirstFileOffset);
                bw.Write(dir.NextDirInSameBucket ?? UnusedEntry_);
                bw.Write(Encoding.Unicode.GetByteCount(dir.Name));
                bw.Write(Encoding.Unicode.GetBytes(dir.Name));

                bw.WriteAlignment(4);
            }

            header.fileHashTableOffset = (int)(bw.BaseStream.Position - startPosition);

            // Write file hash table
            foreach (var hash in metaData.FileHashTable)
                bw.Write(hash);

            header.fileMetaTableOffset = (int)(bw.BaseStream.Position - startPosition);

            // Write file directoryEntries
            foreach (var file in metaData.Files)
            {
                bw.Write(file.ParentDirOffset);
                bw.Write(file.NextSiblingOffset);
                bw.Write(file.DataOffset);
                bw.Write(file.DataSize);
                bw.Write(file.NextFileInSameBucket ?? UnusedEntry_);
                bw.Write(Encoding.Unicode.GetByteCount(file.Name));
                bw.Write(Encoding.Unicode.GetBytes(file.Name));

                bw.WriteAlignment(4);
            }

            header.fileDataOffset = (int)(bw.BaseStream.Position - startPosition);

            // Write files
            foreach (var file in metaData.Files)
                file.FileData.CopyTo(bw.BaseStream);

            var level3Size = bw.BaseStream.Position;

            bw.WriteAlignment(BlockSize_);

            bw.BaseStream.Position = startPosition;
            bw.WriteType(header);

            return level3Size;
        }

        /// <summary>
        /// Write IVFC hash levels.
        /// </summary>
        /// <param name="output">The stream to write to.</param>
        /// <param name="metaDataPosition">The position of the initial data to hash.</param>
        /// <param name="metaDataSize">The position at which to start writing.</param>
        /// <param name="masterHashPosition">The separate position at which the master hash level is written.</param>
        /// <param name="levels">Number of levels to write.</param>
        /// <returns>Position and size of each written level.</returns>
        private static IList<(long, long, long)> WriteIvfcLevels(Stream output, long metaDataPosition, long metaDataSize,
            long masterHashPosition, int levels)
        {
            var result = new List<(long, long, long)>();
            using var bw = new BinaryWriterX(output, true);

            // Pre-calculate hash level offsets
            var hashLevelPositions = new long[levels - 1];

            var levelPosition = metaDataPosition;
            var levelSize = metaDataSize;
            for (var i = levels - 2; i >= 0; i--)
            {
                hashLevelPositions[i] = (levelPosition + levelSize + BlockSize_ - 1) & ~(BlockSize_ - 1);

                levelPosition = hashLevelPositions[i];
                levelSize = levelSize / BlockSize_ * 0x20;
            }

            // Write hash levels
            var dataPosition = metaDataPosition;
            var writePosition = hashLevelPositions[0];
            var dataSize = writePosition - dataPosition;

            var sha256 = new Kryptography.Hash.Sha256();
            for (var level = 0; level < levels; level++)
            {
                bw.BaseStream.Position = writePosition;

                var dataEnd = dataPosition + dataSize;
                while (dataPosition < dataEnd)
                {
                    var blockSize = Math.Min(BlockSize_, dataEnd - dataPosition);
                    var hash = sha256.Compute(new SubStream(output, dataPosition, blockSize));
                    bw.Write(hash);

                    dataPosition += BlockSize_;
                }

                dataPosition = writePosition;
                dataSize = bw.BaseStream.Position - writePosition;

                writePosition = level + 1 >= levels - 1 ? masterHashPosition : hashLevelPositions[level + 1];

                // Pad hash level to next block
                // Do not pad master hash level
                // TODO: Make general padding code that also works with unaligned master hash position
                var alignSize = 0L;
                if(level + 1 < levels - 1)
                {
                    alignSize = ((dataSize + BlockSize_ - 1) & ~(BlockSize_ - 1)) - dataSize;
                    bw.WritePadding((int) alignSize);
                }

                result.Add((dataPosition, dataSize, dataSize + alignSize));
            }

            return result;
        }

        private static void PopulateDirHashTable(List<MetaData.DirEntry> directoryEntries, IList<uint> buckets)
        {
            foreach (var directoryEntry in directoryEntries)
            {
                if (directoryEntry.NextDirInSameBucket != null)
                    continue;

                // Get all fileEntries with same bucket
                var bucketId = GetBucketId(directoryEntry.Hash, buckets.Count);
                var siblings = directoryEntries.Where(e => GetBucketId(e.Hash, buckets.Count) == bucketId).ToArray();

                // Set head entry offset (the latest entry in the list)
                buckets[bucketId] = (uint)siblings.Last().MetaOffset;

                // Set NextDirInSameBucket in each of those fileEntries
                for (var i = 0; i < siblings.Length; i++)
                {
                    siblings[i].NextDirInSameBucket = i != 0 ?
                        siblings[i - 1].MetaOffset :
                        UnusedEntry_;
                }
            }
        }

        private static void PopulateFileHashTable(List<MetaData.FileEntry> fileEntries, IList<uint> buckets)
        {
            foreach (var fileEntry in fileEntries)
            {
                if (fileEntry.NextFileInSameBucket != null)
                    continue;

                // Get all entries with same bucket
                var bucketId = GetBucketId(fileEntry.Hash, buckets.Count);
                var siblings = fileEntries.Where(e => GetBucketId(e.Hash, buckets.Count) == bucketId).ToArray();

                // Set head entry offset (the latest entry in the list)
                buckets[bucketId] = (uint)siblings.Last().MetaOffset;

                //set NextFileInSameBucket in each of those directoryEntries
                for (var i = 0; i < siblings.Length; i++)
                {
                    siblings[i].NextFileInSameBucket = i != 0 ?
                        siblings[i - 1].MetaOffset :
                        UnusedEntry_;
                }
            }
        }

        #region Hash methods

        private static uint CalculatePathHash(uint parentOffset, byte[] nameBytes, int start = 0)
        {
            var hash = parentOffset ^ 123456789;
            for (var i = 0; i < nameBytes.Length; i += 2)
            {
                hash = (hash >> 5) | (hash << 27);
                hash ^= (ushort)(nameBytes[start + i] | (nameBytes[start + i + 1] << 8));
            }

            return hash;
        }

        private static int GetHashTableEntryCount(int entryCount)
        {
            var count = entryCount;
            if (entryCount < 3)
                return 3;

            if (count < 19)
                return entryCount | 1;

            while (count % 2 == 0 || count % 3 == 0 || count % 5 == 0 || count % 7 == 0 || count % 11 == 0 || count % 13 == 0 || count % 17 == 0)
                count++;

            return count;
        }

        private static int GetBucketId(uint hash, int entryCount) => (int)(hash % entryCount);

        #endregion
    }
}
