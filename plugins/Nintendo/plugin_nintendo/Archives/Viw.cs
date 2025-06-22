using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;
using plugin_nintendo.Common.Compression;

namespace plugin_nintendo.Archives
{
    class Viw
    {
        private const int InfHeaderSize_ = 0x10;
        private const int InfEntrySize_ = 0x8;

        private IList<ViwInfMetaEntry> _metas;
        private IList<ViwEntry> _nameEntries;

        public List<IArchiveFile> Load(Stream viwStream, Stream infStream, Stream dataStream)
        {
            using var infBr = new BinaryReaderX(infStream);
            using var viwBr = new BinaryReaderX(viwStream);

            // Read inf header
            var infHeader = ReadInfHeader(infBr);

            // Read entries
            infStream.Position = infHeader.entryOffset;
            var entries = ReadInfEntries(infBr, infHeader.fileCount);

            // Read meta entries
            infStream.Position = infHeader.metaOffset;
            _metas = ReadInfMetaEntries(infBr, infHeader.metaCount);

            // Read name entries
            _nameEntries = ReadEntries(viwBr, infHeader.metaCount <= 0 ? infHeader.fileCount : infHeader.metaCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < infHeader.fileCount; i++)
            {
                var entry = entries[i];
                ViwEntry? nameEntry = i < _nameEntries.Count ? _nameEntries[i] : null;

                var subStream = new SubStream(dataStream, entry.offset, entry.compSize);
                var fileName = (infHeader.fileCount != _nameEntries.Count ? (_nameEntries[0].id + i).ToString("X4") : nameEntry!.Value.name.Trim(' ', '\0')) + ViwSupport.DetermineExtension(subStream);

                result.Add(CreateAfi(subStream, fileName));
            }

            return result;
        }

        public void Save(Stream viwStream, Stream infStream, Stream dataStream, List<IArchiveFile> files)
        {
            using var infBw = new BinaryWriterX(infStream);
            using var viwBw = new BinaryWriterX(viwStream);

            // Calculate offset
            var entryOffset = InfHeaderSize_;
            var metaOffset = entryOffset + files.Count * InfEntrySize_;

            // Write files
            var entries = new List<ViwInfEntry>();

            var filePosition = 0;
            foreach (var file in files)
            {
                dataStream.Position = filePosition;
                var writtenSize = file.WriteFileData(dataStream);

                entries.Add(new ViwInfEntry
                {
                    offset = filePosition,
                    compSize = (int)writtenSize
                });

                filePosition += (int)((writtenSize + 3) & ~3);
            }

            // Write metas
            infStream.Position = metaOffset;
            WriteInfMetaEntries(_metas, infBw);

            // Write entries
            infStream.Position = entryOffset;
            WriteInfEntries(entries, infBw);

            // Write inf header
            var header = new ViwInfHeader
            {
                fileCount = files.Count,
                metaCount = _metas.Count,
                entryOffset = entryOffset,
                metaOffset = metaOffset
            };

            infStream.Position = 0;
            WriteInfHeader(header, infBw);

            // Write name entries
            WriteEntries(_nameEntries, viwBw);
        }

        private IArchiveFile CreateAfi(Stream file, string name)
        {
            file.Position = 0;

            var method = NintendoCompressor.PeekCompressionMethod(file);
            var size = NintendoCompressor.PeekDecompressedSize(file);

            return new ArchiveFile(new CompressedArchiveFileInfo
            {
                FilePath = name,
                FileData = file,
                Compression = NintendoCompressor.GetCompression(method),
                DecompressedSize = size
            });
        }

        private ViwInfHeader ReadInfHeader(BinaryReaderX reader)
        {
            return new ViwInfHeader
            {
                fileCount = reader.ReadInt32(),
                metaCount = reader.ReadInt32(),
                entryOffset = reader.ReadInt32(),
                metaOffset = reader.ReadInt32()
            };
        }

        private ViwInfEntry[] ReadInfEntries(BinaryReaderX reader, int count)
        {
            var result = new ViwInfEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadInfEntry(reader);

            return result;
        }

        private ViwInfEntry ReadInfEntry(BinaryReaderX reader)
        {
            return new ViwInfEntry
            {
                offset = reader.ReadInt32(),
                compSize = reader.ReadInt32()
            };
        }

        private ViwInfMetaEntry[] ReadInfMetaEntries(BinaryReaderX reader, int count)
        {
            var result = new ViwInfMetaEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadInfMetaEntry(reader);

            return result;
        }

        private ViwInfMetaEntry ReadInfMetaEntry(BinaryReaderX reader)
        {
            return new ViwInfMetaEntry
            {
                unk1 = reader.ReadInt16(),
                unk2 = reader.ReadInt16(),
                unk3 = reader.ReadInt32()
            };
        }

        private ViwEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new ViwEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private ViwEntry ReadEntry(BinaryReaderX reader)
        {
            return new ViwEntry
            {
                id = reader.ReadInt32(),
                name = reader.ReadString(0x14)
            };
        }

        private void WriteInfHeader(ViwInfHeader header, BinaryWriterX writer)
        {
            writer.Write(header.fileCount);
            writer.Write(header.metaCount);
            writer.Write(header.entryOffset);
            writer.Write(header.metaOffset);
        }

        private void WriteInfEntries(IList<ViwInfEntry> entries, BinaryWriterX writer)
        {
            foreach (ViwInfEntry entry in entries)
                WriteInfEntry(entry, writer);

        }

        private void WriteInfEntry(ViwInfEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.compSize);
        }

        private void WriteInfMetaEntries(IList<ViwInfMetaEntry> entries, BinaryWriterX writer)
        {
            foreach (ViwInfMetaEntry entry in entries)
                WriteInfMetaEntry(entry, writer);
        }

        private void WriteInfMetaEntry(ViwInfMetaEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.unk1);
            writer.Write(entry.unk2);
            writer.Write(entry.unk3);
        }

        private void WriteEntries(IList<ViwEntry> entries, BinaryWriterX writer)
        {
            foreach (ViwEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(ViwEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.id);
            writer.WriteString(entry.name, writeNullTerminator: false);
        }
    }
}
