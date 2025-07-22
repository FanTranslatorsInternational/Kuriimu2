using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using static ICSharpCode.SharpZipLib.Zip.ExtendedUnixData;

namespace plugin_arc_system_works.Archives
{
    class FPAC
    {
        private static readonly int HeaderSize = 0x20;
        private static readonly int EntrySize = 0xC;

        private FPACTableStructure _tableStruct;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header table structure
            _tableStruct = ReadStruct(br);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in _tableStruct.entries)
            {
                var subStream = new SubStream(input, _tableStruct.header.dataOffset + entry.offset, entry.size);
                result.Add(new FpacArchiveFile(new ArchiveFileInfo
                {
                    FilePath = entry.fileName is null ? $"{entry.fileId:X8}.bin" : entry.fileName.Trim('\0'),
                    FileData = subStream
                }, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entrySize = EntrySize;
            if ((_tableStruct.header.flags & FpacFlags.HasHash) != 0)
                entrySize += 4;

            var fileOffset = HeaderSize + files.Count * ((_tableStruct.header.nameBufferSize + entrySize + 0xF) & ~0xF);

            // Write files
            var entries = new List<FPACEntry>();

            var filePosition = fileOffset;
            foreach (FpacArchiveFile file in files.Cast<FpacArchiveFile>())
            {
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output, true);

                file.Entry.offset = filePosition - fileOffset;
                file.Entry.size = (int)writtenSize;

                entries.Add(file.Entry);

                filePosition += (int)writtenSize;
            }

            // Write structure
            _tableStruct.entries = entries.ToArray();
            _tableStruct.header.dataOffset = fileOffset;
            _tableStruct.header.fileCount = files.Count;
            _tableStruct.header.fileSize = (int)output.Length;

            output.Position = 0;
            WriteStruct(_tableStruct, bw);
        }

        private FPACTableStructure ReadStruct(BinaryReaderX reader)
        {
            FPACHeader header = ReadHeader(reader);
            reader.SeekAlignment();

            return new FPACTableStructure
            {
                header = header,
                entries = ReadEntries(reader, header.fileCount, header.nameBufferSize, header.flags)
            };
        }

        private FPACHeader ReadHeader(BinaryReaderX reader)
        {
            return new FPACHeader
            {
                magic = reader.ReadString(4),
                dataOffset = reader.ReadInt32(),
                fileSize = reader.ReadInt32(),
                fileCount = reader.ReadInt32(),
                flags = (FpacFlags)reader.ReadUInt32(),
                nameBufferSize = reader.ReadInt32()
            };
        }

        private FPACEntry[] ReadEntries(BinaryReaderX reader, int count, int bufferSize, FpacFlags flags)
        {
            var result = new FPACEntry[count];

            for (var i = 0; i < count; i++)
            {
                result[i] = ReadEntry(reader, bufferSize, flags);
                reader.SeekAlignment();
            }

            return result;
        }

        private FPACEntry ReadEntry(BinaryReaderX reader, int bufferSize, FpacFlags flags)
        {
            string? fileName = null;
            if ((flags & FpacFlags.HasNoName) == 0)
                fileName = reader.ReadString(bufferSize);

            var entry = new FPACEntry
            {
                fileName = fileName,
                fileId = reader.ReadInt32(),
                offset = reader.ReadInt32(),
                size = reader.ReadInt32()
            };

            if ((flags & FpacFlags.HasHash) != 0)
                entry.hash = reader.ReadUInt32();

            return entry;
        }

        private void WriteStruct(FPACTableStructure table, BinaryWriterX writer)
        {
            WriteHeader(table.header, writer);
            writer.WriteAlignment(0x10);

            WriteEntries(table.entries, writer, table.header.flags);
        }

        private void WriteHeader(FPACHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.dataOffset);
            writer.Write(header.fileSize);
            writer.Write(header.fileCount);
            writer.Write((uint)header.flags);
            writer.Write(header.nameBufferSize);
        }

        private void WriteEntries(FPACEntry[] entries, BinaryWriterX writer, FpacFlags flags)
        {
            foreach (FPACEntry entry in entries)
            {
                WriteEntry(entry, writer, flags);
                writer.WriteAlignment(0x10);
            }
        }

        private void WriteEntry(FPACEntry entry, BinaryWriterX writer, FpacFlags flags)
        {
            if ((flags & FpacFlags.HasNoName) == 0)
                writer.WriteString(entry.fileName!, writeNullTerminator: false);

            writer.Write(entry.fileId);
            writer.Write(entry.offset);
            writer.Write(entry.size);

            if ((flags & FpacFlags.HasHash) != 0)
                writer.Write(entry.hash);
        }
    }
}
