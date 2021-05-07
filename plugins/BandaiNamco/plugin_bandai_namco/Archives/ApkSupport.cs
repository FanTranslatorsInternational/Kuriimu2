using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.Extensions;
using Komponent.IO;
using Komponent.IO.Attributes;
using Komponent.IO.Streams;
using Kompression.Implementations;
using Kontract.Extensions;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_bandai_namco.Archives
{
    class ApkSectionHeader
    {
        [FixedLength(8)]
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
        [FixedLength(0x10)]
        public byte[] headerIdent;
    }

    class ApkToc
    {
        public ApkTocHeader header;
        [VariableLength("header.entryCount")]
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
        public int offset;
        public int count;
        public int decompSize;
        public int zero1;
        public int compSize;
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
            var header = br.ReadType<ApkSectionHeader>();

            // Prepare section data
            var section = new SubStream(br.BaseStream, br.BaseStream.Position, header.sectionSize);
            br.BaseStream.Position += (header.sectionSize + 0xF) & ~0xF;

            return new ApkSection
            {
                _header = header,
                Data = section
            };
        }

        public T As<T>()
        {
            using var br = new BinaryReaderX(Data, true);
            return br.ReadType<T>();
        }
    }

    class ApkArchiveFileInfo : ArchiveFileInfo
    {
        public int HeaderIndex { get; }

        public ApkArchiveFileInfo(Stream fileData, string filePath, int headerIndex) : base(fileData, filePath)
        {
            HeaderIndex = headerIndex;
        }

        public ApkArchiveFileInfo(Stream fileData, string filePath, int headerIndex, IKompressionConfiguration configuration, long decompressedSize) : base(fileData, filePath, configuration, decompressedSize)
        {
            HeaderIndex = headerIndex;
        }

        public Stream GetFinalStream()
        {
            return base.GetFinalStream();
        }
    }

    class ApkSupport
    {
        public static IEnumerable<IArchiveFileInfo> EnumerateFiles(IList<Stream> streams, ApkTocEntry entry, UPath path, IList<ApkPackHeader> apkHeaders, IList<string> strings, IList<ApkTocEntry> entries)
        {
            var headerName = strings[apkHeaders[entry.headerIndex].stringIndex];
            var name = strings[entry.stringIndex];

            var isDirectory = (entry.flags & 0x1) > 0;
            var isCompressed = (entry.flags & 0x200) > 0;

            if (isDirectory)
            {
                foreach (var subEntry in entries.Skip(entry.offset).Take(entry.count))
                    foreach (var file in EnumerateFiles(streams, subEntry, path / name, apkHeaders, strings, entries))
                        yield return file;
            }
            else
            {
                var stream = streams[entry.headerIndex];
                if(stream==null)
                    yield break;

                if (isCompressed)
                    yield return new ApkArchiveFileInfo(new SubStream(stream, entry.offset, entry.compSize), (headerName / path.ToRelative() / name).FullName, entry.headerIndex, Compressions.ZLib, entry.decompSize);
                else
                    yield return new ApkArchiveFileInfo(new SubStream(stream, entry.offset, entry.decompSize), (headerName / path.ToRelative() / name).FullName, entry.headerIndex);
            }
        }

        public static void Save(Stream output, IList<IArchiveFileInfo> files, string name, byte[] headerIdent)
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
            bw.WriteType(new ApkSectionHeader { magic = ApkSection.EndSection });

            // Write pack file section
            output.Position = packFslsOffset;
            bw.WriteType(new ApkSectionHeader { magic = ApkSection.PackFiles, sectionSize = 0x10 });
            bw.WriteType(new ApkPackFilesHeader());

            // Write entries
            output.Position = entryOffset;
            WriteEntrySection(output, fileTree, strings, new List<long> { dataOffset });

            // Write start section
            output.Position = 0;
            bw.WriteType(new ApkSectionHeader { magic = ApkSection.StartSection });

            // Write pack header
            bw.WriteType(new ApkSectionHeader { magic = ApkSection.PackHeader, sectionSize = 0x20 });
            bw.WriteType(new ApkPackHeader { dataOffset = (int)dataOffset, headerIdent = headerIdent });
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
                bw.WriteString(s, Encoding.ASCII, false);
            }
            bw.WriteAlignment();

            var endPosition = input.Position;

            // Write offsets
            input.Position = position + 0x20;
            bw.WriteMultiple(offsets);

            // Write header
            input.Position = position + 0x10;
            bw.WriteType(new ApkStringHeader
            {
                sectionSize = (int)(endPosition - position - 0x10),
                stringCount = strings.Count,
                dataOffset = (int)(stringPosition - position - 0x10)
            });

            // Write section header
            input.Position = position;
            bw.WriteType(new ApkSectionHeader { magic = ApkSection.StringTable, sectionSize = (int)(endPosition - position - 0x10) });

            input.Position = endPosition;
        }

        public static void WriteEntrySection(Stream input, DirectoryEntry rootEntry, IList<string> strings, IList<long> dataOffsets, bool writeFileData = true)
        {
            using var bw = new BinaryWriterX(input, true);

            var position = input.Position;

            // Write dir entry
            input.Position = position + 0x20;
            bw.WriteType(new ApkTocEntry { flags = 1, stringIndex = strings.IndexOf(""), offset = 1, count = rootEntry.Directories.Count + rootEntry.Files.Count });

            // Write entries
            var entryCount = WriteEntries(input, rootEntry, strings, dataOffsets, writeFileData);
            var endPosition = input.Position;

            // Write entry header
            input.Position = position + 0x10;
            bw.WriteType(new ApkTocHeader { entryCount = entryCount });

            // Write section header
            input.Position = position;
            bw.WriteType(new ApkSectionHeader { magic = ApkSection.PackToc, sectionSize = (int)((endPosition - position - 0x10 + 0xF) & ~0xF) });
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
                    output.Position = position;
                    bw.WriteType(new ApkTocEntry { flags = 1, stringIndex = strings.IndexOf(currentEntry.Name), offset = nextIndex, count = currentEntry.Directories.Count + currentEntry.Files.Count });

                    // Write sub entries
                    output.Position = nextPosition;
                    nextIndex = WriteEntries(output, currentEntry, strings, dataOffsets, writeFileData, nextIndex);
                    nextPosition = output.Position;
                }
                else if (dirFile.Item2 is ApkArchiveFileInfo file)
                {
                    var headerIndex = Math.Min(dataOffsets.Count - 1, file.HeaderIndex);
                    var dataOffset = dataOffsets[headerIndex];

                    // Write file data
                    long writtenSize;
                    if (writeFileData)
                    {
                        output.Position = dataOffset;
                        writtenSize = file.SaveFileData(output);
                    }
                    else
                    {
                        writtenSize = file.GetFinalStream().Length;
                    }

                    // Write file entry
                    output.Position = position;
                    bw.WriteType(new ApkTocEntry { flags = file.UsesCompression ? 0x200 : 0, headerIndex = file.HeaderIndex, stringIndex = strings.IndexOf(dirFile.Item1), offset = (int)dataOffset, compSize = file.UsesCompression ? (int)writtenSize : 0, decompSize = (int)file.FileSize });

                    dataOffsets[headerIndex] += (writtenSize + 0xF) & ~0xF;
                }

                position += 0x28;
            }

            output.Position = nextPosition;
            return nextIndex;
        }
    }
}
