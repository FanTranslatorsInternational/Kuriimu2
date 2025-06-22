using System.Diagnostics;
using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Kryptography.Checksum;

namespace plugin_nintendo.Archives
{
    class NcchHeader
    {
        public byte[] rsa2048;
        public string magic;
        public int ncchSize;
        public ulong partitionId;
        public short makerCode;
        public short version;
        public uint seedHashVerifier;
        public ulong programID;
        public byte[] reserved1;
        public byte[] logoRegionHash;
        public byte[] productCode;
        public byte[] exHeaderHash;
        public int exHeaderSize;
        public int reserved2;
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
        public byte[] exeFsSuperBlockHash;
        public byte[] romFsSuperBlockHash;
    }

    class NcchExeFsHeader
    {
        public NcchExeFsFileEntry[] fileEntries;
        public byte[] reserved1;
        public NcchExeFsFileEntryHash[] fileEntryHashes;
    }

    class NcchExeFsFileEntry
    {
        public string name;
        public int offset;
        public int size;
    }

    class NcchExeFsFileEntryHash
    {
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
            header = ReadHeader(br);

            br.SeekAlignment();
            masterHash = br.ReadBytes(header.masterHashSize);

            // Read Level 3
            lv3Offset = romFsOffset + 0x1000;
            br.BaseStream.Position = lv3Offset;
            lv3Header = ReadLevelHeader(br);

            // Resolve file and directory tree
            br.BaseStream.Position = lv3Offset + lv3Header.dirMetaTableOffset;
            Files = new List<FinalFileInfo>();
            ResolveDirectories(br);
        }

        private NcchRomFsHeader ReadHeader(BinaryReaderX reader)
        {
            return new NcchRomFsHeader
            {
                magic = reader.ReadString(4),
                magicNumber = reader.ReadInt32(),
                masterHashSize = reader.ReadInt32(),
                lv1LogicalOffset = reader.ReadInt64(),
                lv1HashDataSize = reader.ReadInt64(),
                lv1BlockSize = reader.ReadInt32(),
                reserved1 = reader.ReadInt32(),
                lv2LogicalOffset = reader.ReadInt64(),
                lv2HashDataSize = reader.ReadInt64(),
                lv2BlockSize = reader.ReadInt32(),
                reserved2 = reader.ReadInt32(),
                lv3LogicalOffset = reader.ReadInt64(),
                lv3HashDataSize = reader.ReadInt64(),
                lv3BlockSize = reader.ReadInt32(),
                reserved3 = reader.ReadInt32(),
                headerLength = reader.ReadInt32(),
                infoSize = reader.ReadInt32()
            };
        }

        private NcchRomFsLevelHeader ReadLevelHeader(BinaryReaderX reader)
        {
            return new NcchRomFsLevelHeader
            {
                headerLength = reader.ReadInt32(),
                dirHashTableOffset = reader.ReadInt32(),
                dirHashTableSize = reader.ReadInt32(),
                dirMetaTableOffset = reader.ReadInt32(),
                dirMetaTableSize = reader.ReadInt32(),
                fileHashTableOffset = reader.ReadInt32(),
                fileHashTableSize = reader.ReadInt32(),
                fileMetaTableOffset = reader.ReadInt32(),
                fileMetaTableSize = reader.ReadInt32(),
                fileDataOffset = reader.ReadInt32()
            };
        }

        private void ResolveDirectories(BinaryReaderX br, string currentPath = "")
        {
            var currentDirEntry = ReadDirectoryMetaData(br);

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
                    currentFileEntry = ReadFileMetaData(br);

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

        private NcchRomFsDirectoryMetaData ReadDirectoryMetaData(BinaryReaderX reader)
        {
            var metaData = new NcchRomFsDirectoryMetaData
            {
                parentDirOffset = reader.ReadInt32(),
                nextSiblingDirOffset = reader.ReadInt32(),
                firstChildDirOffset = reader.ReadInt32(),
                firstFileOffset = reader.ReadInt32(),
                nextDirInSameBucketOffset = reader.ReadInt32(),
                nameLength = reader.ReadInt32()
            };

            metaData.name = reader.ReadString(metaData.nameLength, Encoding.Unicode);

            return metaData;
        }

        private NcchRomFsFileMetaData ReadFileMetaData(BinaryReaderX reader)
        {
            var metaData = new NcchRomFsFileMetaData
            {
                containingDirOffset = reader.ReadInt32(),
                nextSiblingFileOffset = reader.ReadInt32(),
                fileOffset = reader.ReadInt64(),
                fileSize = reader.ReadInt64(),
                nextFileInSameBucketOffset = reader.ReadInt32(),
                nameLength = reader.ReadInt32()
            };

            metaData.name = reader.ReadString(metaData.nameLength, Encoding.Unicode);

            return metaData;
        }

        [DebuggerDisplay("{filePath}")]
        public class FinalFileInfo
        {
            public string filePath;
            public long fileOffset;
            public long fileSize;
        }
    }

    class NcchRomFsHeader
    {
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
        public string name;
    }

    public static class ExeFsBuilder
    {
        private const int ExeFsHeaderSize_ = 0x200;
        private const int ExeFsFileEntrySize_ = 0x10;
        private const int ExeFsFileEntryHashSize_ = 0x20;

        private const int MediaSize_ = 0x200;
        private const int MaxFiles_ = 0xA;

        public static long Build(Stream output, IList<IArchiveFile> files)
        {
            var hash = new Sha256();

            using var bw = new BinaryWriterX(output, true);

            var inOffset = output.Position;

            // Write file data
            bw.BaseStream.Position = inOffset + ExeFsHeaderSize_;
            var filePosition = bw.BaseStream.Position;

            IList<NcchExeFsFileEntry> fileEntries = new List<NcchExeFsFileEntry>(MaxFiles_);
            IList<NcchExeFsFileEntryHash> fileHashes = new List<NcchExeFsFileEntryHash>(MaxFiles_);
            var fileOffset = 0;
            foreach (var file in files)
            {
                var writtenSize = file.WriteFileData(bw.BaseStream);

                bw.WriteAlignment(MediaSize_);

                fileEntries.Add(new NcchExeFsFileEntry
                {
                    name = file.FilePath.GetName().PadRight(8, '\0'),
                    offset = fileOffset,
                    size = (int)writtenSize
                });
                fileHashes.Add(new NcchExeFsFileEntryHash
                {
                    hash = hash.Compute(new SubStream(output, filePosition + fileOffset, writtenSize))
                });

                fileOffset = (int)(bw.BaseStream.Position - filePosition);
            }

            var finalSize = bw.BaseStream.Position - inOffset;

            // Write file entries
            bw.BaseStream.Position = inOffset;
            WriteEntries(fileEntries, bw);
            bw.WritePadding(ExeFsFileEntrySize_ * (MaxFiles_ - fileEntries.Count));

            // Write reserved data
            bw.WritePadding(0x20);

            // Write file entry hashes
            bw.WritePadding(ExeFsFileEntryHashSize_ * (MaxFiles_ - fileEntries.Count));
            WriteHashes([.. fileHashes.Reverse()], bw);

            output.Position = inOffset + finalSize;
            return finalSize;
        }

        private static void WriteEntries(IList<NcchExeFsFileEntry> entries, BinaryWriterX writer)
        {
            foreach (NcchExeFsFileEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private static void WriteEntry(NcchExeFsFileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.name);
            writer.Write(entry.offset);
            writer.Write(entry.size);
        }

        private static void WriteHashes(IList<NcchExeFsFileEntryHash> hashes, BinaryWriterX writer)
        {
            foreach (NcchExeFsFileEntryHash hash in hashes)
                WriteHash(hash, writer);
        }

        private static void WriteHash(NcchExeFsFileEntryHash hash, BinaryWriterX writer)
        {
            writer.Write(hash.hash);
        }
    }

    class RomFsDirectoryNode
    {
        private RomFsDirectoryNode _parent;

        public string Name { get; private set; }

        public UPath FullPath => _parent == null ? Name : _parent.FullPath / Name;

        public int SizeInBytes => 6 * 4 + ((Encoding.Unicode.GetByteCount(Name) + 3) & ~3) + Directories.Sum(x => x.SizeInBytes);

        public int DirectoryCount => 1 + Directories.Sum(x => x.DirectoryCount);

        public IList<RomFsDirectoryNode> Directories { get; private set; }

        public IList<IArchiveFile> Files { get; private set; }

        public static RomFsDirectoryNode Parse(IList<IArchiveFile> files, UPath rootDirectory)
        {
            var root = new RomFsDirectoryNode
            {
                Name = "/",
                Directories = new List<RomFsDirectoryNode>(),
                Files = new List<IArchiveFile>()
            };

            var directories = files.Select(x => x.FilePath.GetSubDirectory(rootDirectory.ToAbsolute()).GetDirectory()).Distinct().OrderBy(x => x).ToArray();
            foreach (var directory in directories)
            {
                var currentNode = root;
                foreach (var part in directory.Split())
                {
                    var newNode = currentNode.Directories.FirstOrDefault(x => x.Name == part);
                    if (newNode != null)
                    {
                        currentNode = newNode;
                        continue;
                    }

                    newNode = new RomFsDirectoryNode
                    {
                        _parent = currentNode,

                        Name = part,
                        Directories = new List<RomFsDirectoryNode>(),
                        Files = new List<IArchiveFile>()
                    };
                    currentNode.Directories.Add(newNode);

                    currentNode = newNode;
                }

                foreach (var file in files.Where(x => x.FilePath.GetDirectory().ToRelative() == rootDirectory / directory.ToRelative()))
                    currentNode.Files.Add(file);
            }

            return root;
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
            public long FileOffset { get; set; }

            public List<DirEntry> Dirs = new();
            public uint[] DirHashTable;
            public List<FileEntry> Files = new();
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

        public static long CalculateRomFsSize(IList<IArchiveFile> files, UPath rootDirectory)
        {
            var rootNode = RomFsDirectoryNode.Parse(files, rootDirectory);

            long totalSize = BlockSize_;

            // Calculate meta size
            totalSize += GetHashTableEntryCount(rootNode.DirectoryCount) * 4;
            totalSize += GetHashTableEntryCount(files.Count) * 4;
            totalSize += rootNode.SizeInBytes;
            totalSize += files.Sum(x => MetaData.FileEntry.Size + ((Encoding.Unicode.GetByteCount(x.FilePath.GetName()) + 3) & ~3));

            // Calculate file size
            totalSize = (totalSize + 0xF) & ~0xF;
            totalSize += files.Sum(x => (x.FileSize + 0xF) & ~0xF);
            totalSize = (totalSize + BlockSize_ - 1) & ~(BlockSize_ - 1);

            // Calculate hash level sizes
            var levelSize = totalSize - BlockSize_;
            for (var i = 0; i < 2; i++)
            {
                var currentLevelSize = levelSize / BlockSize_ * 0x20;
                currentLevelSize = (currentLevelSize + BlockSize_ - 1) & ~(BlockSize_ - 1);

                totalSize += currentLevelSize;

                levelSize = currentLevelSize;
            }

            return totalSize;
        }

        public static (long, long) Build(Stream input, IList<IArchiveFile> files, UPath rootDirectory)
        {
            // Parse files into file tree
            var rootNode = RomFsDirectoryNode.Parse(files, rootDirectory);

            // Create MetaData Tree
            var metaData = new MetaData
            {
                DirMetaOffset = rootNode.Directories.Count <= 0 ? UnusedEntry_ : 0x18
            };
            metaData.Dirs.Add(new MetaData.DirEntry
            {
                MetaOffset = 0,
                ParentOffset = 0,
                NextSiblingOffset = UnusedEntry_,
                FirstChildOffset = rootNode.Directories.Count <= 0 ? UnusedEntry_ : 0x18,
                FirstFileOffset = 0,
                NextDirInSameBucket = UnusedEntry_,
                Hash = CalculatePathHash(0, Encoding.Unicode.GetBytes(string.Empty)),
                Name = string.Empty
            });

            PopulateMetaData(metaData, rootNode, metaData.Dirs[0]);

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
        /// Populates the meta data tree.
        /// </summary>
        /// <param name="metaData">The meta data to populate.</param>
        /// <param name="dir">The directory to populate the meta data with.</param>
        /// <param name="parentDir">The parent directory meta data.</param>
        private static void PopulateMetaData(MetaData metaData, RomFsDirectoryNode dir, MetaData.DirEntry parentDir)
        {
            // Adding files
            var files = dir.Files;
            for (var i = 0; i < files.Count; i++)
            {
                var newFileEntry = new MetaData.FileEntry
                {
                    MetaOffset = metaData.FileMetaOffset,
                    Hash = CalculatePathHash((uint)parentDir.MetaOffset, Encoding.Unicode.GetBytes(files[i].FilePath.GetName())),

                    FileData = files[i].GetFileData().Result,

                    ParentDirOffset = parentDir.MetaOffset,
                    DataOffset = metaData.FileOffset,
                    DataSize = files[i].FileSize,
                    Name = files[i].FilePath.GetName()
                };
                metaData.FileOffset = (metaData.FileOffset + files[i].FileSize + 0xF) & ~0xF;

                metaData.FileMetaOffset += MetaData.FileEntry.Size + files[i].FilePath.GetName().Length * 2;
                if (metaData.FileMetaOffset % 4 != 0)
                    metaData.FileMetaOffset += 2;

                newFileEntry.NextSiblingOffset = i + 1 == files.Count ? UnusedEntry_ : metaData.FileMetaOffset;

                metaData.Files.Add(newFileEntry);
            }

            // Adding sub directories
            var dirs = dir.Directories;
            var metaDirIndices = new List<int>();
            for (var i = 0; i < dirs.Count; i++)
            {
                var newDirEntry = new MetaData.DirEntry
                {
                    //Parent = parentDir,

                    MetaOffset = metaData.DirMetaOffset,
                    Hash = CalculatePathHash((uint)parentDir.MetaOffset, Encoding.Unicode.GetBytes(dirs[i].Name)),

                    ParentOffset = parentDir.MetaOffset,

                    Name = dirs[i].Name
                };

                metaData.DirMetaOffset += MetaData.DirEntry.Size + dirs[i].Name.Length * 2;
                if (metaData.DirMetaOffset % 4 != 0)
                    metaData.DirMetaOffset += 2;

                newDirEntry.NextSiblingOffset = i + 1 < dirs.Count ? metaData.DirMetaOffset : UnusedEntry_;

                metaData.Dirs.Add(newDirEntry);
                metaDirIndices.Add(metaData.Dirs.Count - 1);
            }

            // Adding children of sub directories
            for (var i = 0; i < dirs.Count; i++)
            {
                metaData.Dirs[metaDirIndices[i]].FirstChildOffset = dirs[i].Directories.Count > 0 ? metaData.DirMetaOffset : UnusedEntry_;
                metaData.Dirs[metaDirIndices[i]].FirstFileOffset = dirs[i].Files.Count > 0 ? metaData.FileMetaOffset : UnusedEntry_;

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
            var masterHashPosition = 0x60;
            var metaDataPosition = BlockSize_;

            // Write meta data
            output.Position = metaDataPosition;
            var metaDataSize = WriteMetaData(output, metaData);

            // Write IVFC levels
            var levelData = WriteIvfcLevels(output, metaDataPosition, metaDataSize, masterHashPosition, 3);

            // Write RomFs header
            using var bw = new BinaryWriterX(output, true);

            var header = new NcchRomFsHeader
            {
                masterHashSize = (int)levelData[2].Item2,

                lv1LogicalOffset = 0,
                lv1HashDataSize = levelData[1].Item2,

                lv2LogicalOffset = levelData[1].Item3,
                lv2HashDataSize = levelData[0].Item2,

                lv3LogicalOffset = levelData[1].Item3 + levelData[0].Item3,
                lv3HashDataSize = metaDataSize
            };

            bw.BaseStream.Position = 0;
            WriteHeader(header, bw);

            var romFsSize = levelData[0].Item1 + levelData[0].Item3;
            var romFsHeaderSize = 0x60 + levelData[2].Item2;

            return (romFsSize, romFsHeaderSize);
        }

        private static void WriteHeader(NcchRomFsHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.magicNumber);
            writer.Write(header.masterHashSize);
            writer.Write(header.lv1LogicalOffset);
            writer.Write(header.lv1HashDataSize);
            writer.Write(header.lv1BlockSize);
            writer.Write(header.reserved1);
            writer.Write(header.lv2LogicalOffset);
            writer.Write(header.lv2HashDataSize);
            writer.Write(header.lv2BlockSize);
            writer.Write(header.reserved2);
            writer.Write(header.lv3LogicalOffset);
            writer.Write(header.lv3HashDataSize);
            writer.Write(header.lv3BlockSize);
            writer.Write(header.reserved3);
            writer.Write(header.headerLength);
            writer.Write(header.infoSize);
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

            // Write directory entries
            header.dirMetaTableOffset = (int)(bw.BaseStream.Position - startPosition);
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

            // Write file hash table
            header.fileHashTableOffset = (int)(bw.BaseStream.Position - startPosition);
            foreach (var hash in metaData.FileHashTable)
                bw.Write(hash);

            // Write file entries
            header.fileMetaTableOffset = (int)(bw.BaseStream.Position - startPosition);
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

            // Write files
            header.fileDataOffset = (int)((bw.BaseStream.Position - startPosition + 0xF) & ~0xF);
            foreach (var file in metaData.Files)
            {
                bw.WriteAlignment(16);
                file.FileData.CopyTo(bw.BaseStream);
            }

            var level3Size = bw.BaseStream.Position - startPosition;

            bw.WriteAlignment(BlockSize_);

            bw.BaseStream.Position = startPosition;
            WriteHeader(header, bw);

            return level3Size;
        }

        private static void WriteHeader(NcchRomFsLevelHeader header, BinaryWriterX writer)
        {
            writer.Write(header.headerLength);
            writer.Write(header.dirHashTableOffset);
            writer.Write(header.dirHashTableSize);
            writer.Write(header.dirMetaTableOffset);
            writer.Write(header.dirMetaTableSize);
            writer.Write(header.fileHashTableOffset);
            writer.Write(header.fileHashTableSize);
            writer.Write(header.fileMetaTableOffset);
            writer.Write(header.fileMetaTableSize);
            writer.Write(header.fileDataOffset);
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
            // Pre-calculate hash level sizes
            var hashLevelSizes = new long[levels];

            var alignedMetaDataSize = (metaDataSize + BlockSize_ - 1) & ~(BlockSize_ - 1);
            for (var level = 0; level < levels - 1; level++)
            {
                var previousSize = level == 0 ? alignedMetaDataSize : hashLevelSizes[level - 1];
                var levelSize = previousSize / BlockSize_ * 0x20;
                var alignedLevelSize = (levelSize + BlockSize_ - 1) & ~(BlockSize_ - 1);

                hashLevelSizes[level] = alignedLevelSize;
            }

            // Pre-calculate hash level position
            var hashLevelPositions = new long[levels];

            var alignedMetaDataPosition = (metaDataPosition + BlockSize_ - 1) & ~(BlockSize_ - 1);
            for (var level = 0; level < levels - 1; level++)
            {
                var levelPosition = alignedMetaDataPosition + alignedMetaDataSize + hashLevelSizes.Skip(level + 1).Take(levels - level - 2).Sum(x => x);

                hashLevelPositions[level] = levelPosition;
            }

            // Add master hash position
            hashLevelSizes[levels - 1] = BlockSize_;
            hashLevelPositions[levels - 1] = masterHashPosition;

            // Write hash levels
            var result = new List<(long, long, long)>();
            var sha256 = new Sha256();

            var previousLevelPosition = alignedMetaDataPosition;
            var previousLevelSize = alignedMetaDataSize;
            for (var level = 0; level < levels; level++)
            {
                var previousLevelStream = new SubStream(output, previousLevelPosition, previousLevelSize);
                var levelStream = new SubStream(output, hashLevelPositions[level], hashLevelSizes[level]);

                var block = new byte[BlockSize_];
                while (previousLevelStream.Position < previousLevelStream.Length)
                {
                    previousLevelStream.Read(block, 0, BlockSize_);
                    var hash = sha256.Compute(block);
                    levelStream.Write(hash);
                }

                result.Add((hashLevelPositions[level], levelStream.Position, hashLevelSizes[level]));

                previousLevelPosition = hashLevelPositions[level];
                previousLevelSize = hashLevelSizes[level];
            }

            return result;
        }

        private static void PopulateDirHashTable(IList<MetaData.DirEntry> directoryEntries, IList<uint> buckets)
        {
            foreach (var directoryEntry in directoryEntries)
            {
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
