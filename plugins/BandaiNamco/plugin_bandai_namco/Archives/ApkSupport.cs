using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.DataClasses.FileSystem;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_bandai_namco.Archives
{
    class ApkSectionHeader
    {
        public string magic;
        public int sectionSize;
        public int zero1;
    }

    class ApkPackHeader
    {
        public int unk1 = 0x00010000;
        public int stringIndex;
        public int dataOffset;
        public int unk2 = 1;
        public byte[] headerIdent;
    }

    class ApkToc
    {
        public ApkTocHeader header;
        public ApkTocEntry[] entries;
    }

    class ApkTocHeader
    {
        public int entrySize = 0x28;
        public int entryCount;
        public int entryOffset = 0x10;
        public int zero0;
    }

    class ApkTocEntry
    {
        public int flags;
        public int stringIndex;
        public int headerIndex;
        public int zero0;
        public uint offset;
        public int count;
        public uint decompSize;
        public int zero1;
        public uint compSize;
        public int zero2;
    }

    class ApkPackFilesHeader
    {
        public int zero0;
        public int entrySize = 0x28;
        public int sectionSize = 0x10;
        public int zero1;
    }

    class ApkStringHeader
    {
        public int stringCount;
        public int tableOffset = 0x10;
        public int dataOffset;
        public int sectionSize;
    }

    class ApkSection
    {
        public const string StartSection = "ENDILTLE";
        public const string PackHeader = "PACKHEDR";
        public const string PackToc = "PACKTOC ";
        public const string PackFiles = "PACKFSLS";
        public const string StringTable = "GENESTRT";
        public const string EndSection = "GENEEOF ";

        private ApkSectionHeader _header;

        public string Type => _header.magic;
        public Stream Data { get; set; }

        private ApkSection() { }

        public static IList<ApkSection> ReadAll(Stream input)
        {
            var sections = new List<ApkSection>();
            while (sections.Count == 0 || sections.Last().Type != EndSection)
                sections.Add(Read(input));

            return sections;
        }

        public static ApkSection Read(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = ApkSupport.ReadSectionHeader(br);

            // Prepare section data
            var section = new SubStream(br.BaseStream, br.BaseStream.Position, header.sectionSize);
            br.BaseStream.Position += (header.sectionSize + 0xF) & ~0xF;

            return new ApkSection
            {
                _header = header,
                Data = section
            };
        }

        public ApkPackHeader ReadPackHeader()
        {
            using var br = new BinaryReaderX(Data, true);

            return new ApkPackHeader
            {
                unk1 = br.ReadInt32(),
                stringIndex = br.ReadInt32(),
                dataOffset = br.ReadInt32(),
                unk2 = br.ReadInt32(),
                headerIdent = br.ReadBytes(0x10)
            };
        }

        public ApkToc ReadToc()
        {
            using var br = new BinaryReaderX(Data, true);

            var header = new ApkTocHeader
            {
                entrySize = br.ReadInt32(),
                entryCount = br.ReadInt32(),
                entryOffset = br.ReadInt32(),
                zero0 = br.ReadInt32()
            };

            return new ApkToc
            {
                header = header,
                entries = ReadTocEntries(br, header.entryCount)
            };
        }

        private ApkTocEntry[] ReadTocEntries(BinaryReaderX reader, int count)
        {
            var result = new ApkTocEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadTocEntry(reader);

            return result;
        }

        private ApkTocEntry ReadTocEntry(BinaryReaderX reader)
        {
            return new ApkTocEntry
            {
                flags = reader.ReadInt32(),
                stringIndex = reader.ReadInt32(),
                headerIndex = reader.ReadInt32(),
                zero0 = reader.ReadInt32(),
                offset = reader.ReadUInt32(),
                count = reader.ReadInt32(),
                decompSize = reader.ReadUInt32(),
                zero1 = reader.ReadInt32(),
                compSize = reader.ReadUInt32(),
                zero2 = reader.ReadInt32()
            };
        }
    }

    class ApkArchiveFile : ArchiveFile
    {
        public int HeaderIndex { get; }

        public ApkArchiveFile(ArchiveFileInfo fileInfo, int headerIndex) : base(fileInfo)
        {
            HeaderIndex = headerIndex;
        }
    }

    class ApkSupport
    {
        private static long _largestFileOffset;
        private static string _largestFileName;

        public static IEnumerable<IArchiveFile> EnumerateFiles(IList<Stream> streams, ApkTocEntry entry, UPath path, IList<ApkPackHeader> apkHeaders, IList<string> strings, IList<ApkTocEntry> entries)
        {
            var headerName = strings[apkHeaders[entry.headerIndex].stringIndex];
            var name = strings[entry.stringIndex];

            var isDirectory = (entry.flags & 0x1) > 0;
            var isCompressed = (entry.flags & 0x300) > 0;

            if (isDirectory)
            {
                foreach (var subEntry in entries.Skip((int)entry.offset).Take(entry.count))
                    foreach (var file in EnumerateFiles(streams, subEntry, path / name, apkHeaders, strings, entries))
                        yield return file;
            }
            else
            {
                var stream = streams[entry.headerIndex];
                if (stream == null)
                    yield break;

                if (isCompressed && _largestFileOffset < entry.offset)
                {
                    _largestFileOffset = entry.offset;
                    _largestFileName = (headerName / path.ToRelative() / name).FullName;
                }

                ArchiveFileInfo fileInfo;
                if ((entry.flags & 0x200) > 0)
                {
                    fileInfo = new CompressedArchiveFileInfo
                    {
                        FilePath = (headerName / path.ToRelative() / name).FullName,
                        FileData = new SubStream(stream, entry.offset, entry.compSize),
                        Compression = Compressions.ZLib.Build(),
                        DecompressedSize = (int)entry.decompSize
                    };
                }
                else if ((entry.flags & 0x300) > 0)
                {
                    fileInfo = new CompressedArchiveFileInfo
                    {
                        FilePath = (headerName / path.ToRelative() / name).FullName,
                        FileData = new SubStream(stream, entry.offset, entry.compSize),
                        Compression = Compressions.Lzma.Build(),
                        DecompressedSize = (int)entry.decompSize
                    };
                }
                else
                {
                    fileInfo = new ArchiveFileInfo
                    {
                        FilePath = (headerName / path.ToRelative() / name).FullName,
                        FileData = new SubStream(stream, entry.offset, entry.decompSize)
                    };
                }

                yield return new ApkArchiveFile(fileInfo, entry.headerIndex);
            }
        }

        public static void Save(Stream output, IList<IArchiveFile> files, string name, byte[] headerIdent)
        {
            using var bw = new BinaryWriterX(output, true);

            var fileTree = files.ToTree();
            foreach (var part in name.Split('/'))
                fileTree = fileTree?.Directories.FirstOrDefault(x => x.Name == part);

            // Calculate offsets
            var packHeaderOffset = 0x10;
            var entryOffset = packHeaderOffset + 0x30;
            var packFslsOffset = (entryOffset + 0x20 + CountEntries(fileTree) * 0x28 + 0xF) & ~0xF;
            var stringOffset = packFslsOffset + 0x20;

            // Distinct strings
            var strings = new List<string> { name, "" };
            strings.AddRange(files.SelectMany(x => x.FilePath.GetSubDirectory(((UPath)name).ToAbsolute()).ToRelative().Split()).Distinct());

            // Write strings
            output.Position = stringOffset;
            WriteStringTable(output, strings);

            var endSectionOffset = output.Length;
            var dataOffset = (endSectionOffset + 0x10 + 0x7FF) & ~0x7FF;

            // Write end section
            WriteSectionHeader(new ApkSectionHeader { magic = ApkSection.EndSection }, bw);

            // Write pack file section
            output.Position = packFslsOffset;
            WriteSectionHeader(new ApkSectionHeader { magic = ApkSection.PackFiles, sectionSize = 0x10 }, bw);
            WritePackFilesHeader(new ApkPackFilesHeader(), bw);

            // Write entries
            output.Position = entryOffset;
            WriteEntrySection(output, fileTree, strings, new List<long> { dataOffset });

            // Write start section
            output.Position = 0;
            WriteSectionHeader(new ApkSectionHeader { magic = ApkSection.StartSection }, bw);

            // Write pack header
            WriteSectionHeader(new ApkSectionHeader { magic = ApkSection.PackHeader, sectionSize = 0x20 }, bw);
            WritePackHeader(new ApkPackHeader { dataOffset = (int)dataOffset, headerIdent = headerIdent }, bw);
        }

        public static int CountEntries(DirectoryEntry entry) => 1 + entry.Files.Count + entry.Directories.Sum(CountEntries);

        public static void WriteStringTable(Stream input, IList<string> strings)
        {
            using var bw = new BinaryWriterX(input, true);

            var position = input.Position;

            // Write strings
            var stringPosition = (position + 0x20 + strings.Count * 4 + 0xF) & ~0xF;
            input.Position = stringPosition;

            var offsets = new List<int>();
            foreach (var s in strings)
            {
                offsets.Add((int)(input.Position - stringPosition));
                bw.WriteString(s, Encoding.ASCII);
            }
            bw.WriteAlignment(0x10);

            var endPosition = input.Position;

            // Write offsets
            input.Position = position + 0x20;
            WriteIntegers(offsets, bw);

            // Write header
            var header = new ApkStringHeader
            {
                sectionSize = (int)(endPosition - position - 0x10),
                stringCount = strings.Count,
                dataOffset = (int)(stringPosition - position - 0x10)
            };

            input.Position = position + 0x10;
            WriteStringHeader(header, bw);

            // Write section header
            input.Position = position;
            WriteSectionHeader(new ApkSectionHeader { magic = ApkSection.StringTable, sectionSize = (int)(endPosition - position - 0x10) }, bw);

            input.Position = endPosition;
        }

        public static void WriteEntrySection(Stream input, DirectoryEntry rootEntry, IList<string> strings, IList<long> dataOffsets, bool writeFileData = true)
        {
            using var bw = new BinaryWriterX(input, true);

            var position = input.Position;

            // Write dir entry
            var entry = new ApkTocEntry
            {
                flags = 1,
                stringIndex = strings.IndexOf(""),
                offset = 1,
                count = rootEntry.Directories.Count + rootEntry.Files.Count
            };

            input.Position = position + 0x20;
            WriteTocEntry(entry, bw);

            // Write entries
            var entryCount = WriteEntries(input, rootEntry, strings, dataOffsets, writeFileData);
            var endPosition = input.Position;

            // Write entry header
            input.Position = position + 0x10;
            WriteTocHeader(new ApkTocHeader { entryCount = entryCount }, bw);

            // Write section header
            input.Position = position;
            WriteSectionHeader(new ApkSectionHeader { magic = ApkSection.PackToc, sectionSize = (int)((endPosition - position - 0x10 + 0xF) & ~0xF) }, bw);
        }

        private static int WriteEntries(Stream output, DirectoryEntry entry, IList<string> strings, IList<long> dataOffsets, bool writeFileData, int index = 1)
        {
            using var bw = new BinaryWriterX(output, true);

            // Sort entries
            var entries = new List<(string, object)>();
            foreach (var dir in entry.Directories)
                entries.Add((dir.Name, dir));
            foreach (var file in entry.Files)
                entries.Add((file.FilePath.GetName(), file));

            var position = output.Position;
            var nextIndex = index + entries.Count;
            var nextPosition = output.Position + entries.Count * 0x28;

            // Write entries
            foreach (var dirFile in entries.OrderBy(x => x.Item1))
            {
                if (dirFile.Item2 is DirectoryEntry currentEntry)
                {
                    // Write dir entry
                    var tocEntry = new ApkTocEntry
                    {
                        flags = 1,
                        stringIndex = strings.IndexOf(currentEntry.Name),
                        offset = (uint)nextIndex,
                        count = currentEntry.Directories.Count + currentEntry.Files.Count
                    };

                    output.Position = position;
                    WriteTocEntry(tocEntry, bw);

                    // Write sub entries
                    output.Position = nextPosition;
                    nextIndex = WriteEntries(output, currentEntry, strings, dataOffsets, writeFileData, nextIndex);
                    nextPosition = output.Position;
                }
                else if (dirFile.Item2 is ApkArchiveFile file)
                {
                    var headerIndex = Math.Min(dataOffsets.Count - 1, file.HeaderIndex);
                    var dataOffset = dataOffsets[headerIndex];

                    // Write file data
                    long writtenSize;
                    if (writeFileData)
                    {
                        output.Position = dataOffset;
                        writtenSize = file.WriteFileData(output, true);
                    }
                    else
                    {
                        writtenSize = file.GetFileData().Result.Length;
                    }

                    // Write file entry
                    var tocEntry = new ApkTocEntry
                    {
                        flags = file.UsesCompression ? 0x200 : 0,
                        headerIndex = file.HeaderIndex,
                        stringIndex = strings.IndexOf(dirFile.Item1),
                        offset = (uint)dataOffset,
                        compSize = file.UsesCompression ? (uint)writtenSize : 0,
                        decompSize = (uint)file.FileSize
                    };

                    output.Position = position;
                    WriteTocEntry(tocEntry, bw);

                    dataOffsets[headerIndex] += (writtenSize + 0xF) & ~0xF;
                }

                position += 0x28;
            }

            output.Position = nextPosition;
            return nextIndex;
        }

        public static ApkSectionHeader ReadSectionHeader(BinaryReaderX reader)
        {
            return new ApkSectionHeader
            {
                magic = reader.ReadString(8),
                sectionSize = reader.ReadInt32(),
                zero1 = reader.ReadInt32()
            };
        }

        public static ApkStringHeader ReadStringHeader(BinaryReaderX reader)
        {
            return new ApkStringHeader
            {
                stringCount = reader.ReadInt32(),
                tableOffset = reader.ReadInt32(),
                dataOffset = reader.ReadInt32(),
                sectionSize = reader.ReadInt32()
            };
        }

        public static void WriteSectionHeader(ApkSectionHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.sectionSize);
            writer.Write(header.zero1);
        }

        public static void WritePackFilesHeader(ApkPackFilesHeader header, BinaryWriterX writer)
        {
            writer.Write(header.zero0);
            writer.Write(header.entrySize);
            writer.Write(header.sectionSize);
            writer.Write(header.zero1);
        }

        public static void WritePackHeader(ApkPackHeader header, BinaryWriterX writer)
        {
            writer.Write(header.unk1);
            writer.Write(header.stringIndex);
            writer.Write(header.dataOffset);
            writer.Write(header.unk2);
            writer.Write(header.headerIdent);
        }

        private static void WriteIntegers(IList<int> entries, BinaryWriterX writer)
        {
            foreach (int entry in entries)
                writer.Write(entry);
        }

        private static void WriteStringHeader(ApkStringHeader header, BinaryWriterX writer)
        {
            writer.Write(header.stringCount);
            writer.Write(header.tableOffset);
            writer.Write(header.dataOffset);
            writer.Write(header.sectionSize);
        }

        private static void WriteTocEntry(ApkTocEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.flags);
            writer.Write(entry.stringIndex);
            writer.Write(entry.headerIndex);
            writer.Write(entry.zero0);
            writer.Write(entry.offset);
            writer.Write(entry.count);
            writer.Write(entry.decompSize);
            writer.Write(entry.zero1);
            writer.Write(entry.compSize);
            writer.Write(entry.zero2);
        }

        private static void WriteTocHeader(ApkTocHeader header, BinaryWriterX writer)
        {
            writer.Write(header.entrySize);
            writer.Write(header.entryCount);
            writer.Write(header.entryOffset);
            writer.Write(header.zero0);
        }
    }
}
