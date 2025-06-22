using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_arc_system_works.Archives
{
    class FPAC
    {
        private static readonly int HeaderSize = 0x20;
        private static readonly int EntrySizeWithoutName = 0xC;

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
                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = entry.fileName.Trim('\0'),
                    FileData = subStream
                }));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            var maxNameLength = files.Max(x => x.FilePath.ToRelative().GetName().Length + 1);
            maxNameLength = maxNameLength % 4 == 0 ? maxNameLength + 4 : (maxNameLength + 3) & ~3;

            // Calculate offsets
            var fileOffset = HeaderSize + files.Count * ((maxNameLength + EntrySizeWithoutName + 0xF) & ~0xF);

            // Write files
            var entries = new List<FPACEntry>();

            var filePosition = fileOffset;
            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];

                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output);

                entries.Add(new FPACEntry
                {
                    fileName = file.FilePath.GetName().PadRight(maxNameLength, '\0'),
                    fileId = i,
                    offset = filePosition - fileOffset,
                    size = (int)writtenSize
                });

                filePosition += (int)writtenSize;
            }

            // Write structure
            _tableStruct.entries = entries.ToArray();
            _tableStruct.header.dataOffset = fileOffset;
            _tableStruct.header.fileCount = files.Count;
            _tableStruct.header.fileSize = (int)output.Length;
            _tableStruct.header.nameBufferSize = maxNameLength;

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
                entries = ReadEntries(reader, header.fileCount, header.nameBufferSize)
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
                unk1 = reader.ReadInt32(),
                nameBufferSize = reader.ReadInt32()
            };
        }

        private FPACEntry[] ReadEntries(BinaryReaderX reader, int count, int bufferSize)
        {
            var result = new FPACEntry[count];

            for (var i = 0; i < count; i++)
            {
                result[i] = ReadEntry(reader, bufferSize);
                reader.SeekAlignment();
            }

            return result;
        }

        private FPACEntry ReadEntry(BinaryReaderX reader, int bufferSize)
        {
            return new FPACEntry
            {
                fileName = reader.ReadString(bufferSize),
                fileId = reader.ReadInt32(),
                offset = reader.ReadInt32(),
                size = reader.ReadInt32()
            };
        }

        private void WriteStruct(FPACTableStructure table, BinaryWriterX writer)
        {
            WriteHeader(table.header, writer);
            writer.WriteAlignment(0x10);

            WriteEntries(table.entries, writer);
        }

        private void WriteHeader(FPACHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.fileSize);
            writer.Write(header.fileCount);
            writer.Write(header.unk1);
            writer.Write(header.nameBufferSize);
        }

        private void WriteEntries(FPACEntry[] entries, BinaryWriterX writer)
        {
            foreach (FPACEntry entry in entries)
            {
                WriteEntry(entry, writer);
                writer.WriteAlignment(0x10);
            }
        }

        private void WriteEntry(FPACEntry entry, BinaryWriterX writer)
        {
            writer.WriteString(entry.fileName, writeNullTerminator: false);
            writer.Write(entry.fileId);
            writer.Write(entry.offset);
            writer.Write(entry.size);
        }
    }
}
