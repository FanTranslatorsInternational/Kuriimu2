using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    class MmBin
    {
        private const int HeaderSize_ = 0xC;
        private const int EntrySize_ = 0x40;

        private MMBinHeader _header;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadHeader(br);

            // Read entries
            var entries = ReadEntries(br, _header.resourceCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                var offset = entry.offset;
                var resourceName = entry.resourceName.Trim('\0');

                var metaStream = new SubStream(input, offset, entry.metaSize);
                var metaName = $"{resourceName}/{resourceName}.meta";
                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = metaName,
                    FileData = metaStream
                }));
                offset += entry.metaSize;

                var ctpkStream = new SubStream(input, offset, entry.ctpkSize);
                var ctpkName = $"{resourceName}/{resourceName}.ctpk";
                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = ctpkName,
                    FileData = ctpkStream
                }));
            }

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize_;
            var fileOffset = entryOffset + (files.Count / 2) * EntrySize_;
            var filePosition = fileOffset;

            // Write files
            output.Position = filePosition;

            var entries = new List<MMBinResourceEntry>();
            foreach (var fileGroup in files.GroupBy(x => x.FilePath.ToRelative().GetDirectory()))
            {
                var metaFile = fileGroup.First(x => x.FilePath.GetExtensionWithDot() == ".meta");
                metaFile.WriteFileData(output);
                var metaSize = metaFile.FileSize;

                var ctpkFile = fileGroup.First(x => x.FilePath.GetExtensionWithDot() == ".ctpk");
                ctpkFile.WriteFileData(output);
                var ctpkSize = ctpkFile.FileSize;

                var entry = new MMBinResourceEntry
                {
                    resourceName = fileGroup.Key.FullName.PadRight(0x24, '\0'),
                    offset = filePosition,
                    metaSize = (int)metaSize,
                    ctpkSize = (int)ctpkSize,
                    padding = new byte[0xC]
                };
                entries.Add(entry);

                filePosition += (int)(metaSize + ctpkSize);
            }

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write header
            output.Position = 0;

            _header.tableSize = fileOffset;
            _header.resourceCount = (short)(files.Count / 2);
            WriteHeader(_header, bw);
        }

        private MMBinHeader ReadHeader(BinaryReaderX reader)
        {
            return new MMBinHeader
            {
                tableSize = reader.ReadInt32(),
                resourceCount = reader.ReadInt16(),
                unk1 = reader.ReadInt16(),
                unk2 = reader.ReadInt32()
            };
        }

        private MMBinResourceEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new MMBinResourceEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private MMBinResourceEntry ReadEntry(BinaryReaderX reader)
        {
            return new MMBinResourceEntry
            {
                resourceName = reader.ReadString(0x24),
                offset = reader.ReadInt32(),
                metaSize = reader.ReadInt32(),
                ctpkSize = reader.ReadInt32(),
                padding = reader.ReadBytes(0xC)
            };
        }

        private void WriteHeader(MMBinHeader header, BinaryWriterX writer)
        {
            writer.Write(header.tableSize);
            writer.Write(header.resourceCount);
            writer.Write(header.unk1);
            writer.Write(header.unk2);
        }

        private void WriteEntries(IList<MMBinResourceEntry> entries, BinaryWriterX writer)
        {
            foreach (MMBinResourceEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(MMBinResourceEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.resourceName);
            writer.Write(entry.offset);
            writer.Write(entry.metaSize);
            writer.Write(entry.ctpkSize);
            writer.Write(entry.padding);
        }
    }
}
