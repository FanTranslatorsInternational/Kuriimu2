using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_square_enix.Archives
{
    class Dpk
    {
        private const int BlockSize = 0x80;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = ReadHeader(br);

            // Read entries
            input.Position = BlockSize;
            var entries = ReadEntries(br, header.fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                var subStream = new SubStream(input, entry.offset, entry.compSize);
                var fileName = entry.name.Trim('\0');

                result.Add(CreateAfi(subStream, fileName, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = BlockSize;
            var fileOffset = entryOffset + files.Count * BlockSize;

            // Write files
            // HINT: Files are ordered by name
            var entries = new List<DpkEntry>();

            var filePosition = fileOffset;
            foreach (var file in files.OrderBy(x => x.FilePath.ToRelative().FullName))
            {
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output);

                var name = CapAndPadName(file.FilePath.ToRelative().FullName);
                entries.Add(new DpkEntry
                {
                    name = name,
                    nameSum = (short)NameSum(name),
                    offset = filePosition,
                    compSize = (int)writtenSize,
                    decompSize = (int)file.FileSize
                });

                filePosition += (int)((writtenSize + (BlockSize - 1)) & ~(BlockSize - 1));
            }

            if (output.Position % BlockSize != 0)
                bw.WriteAlignment(BlockSize);

            // Write entries
            // HINT: Entries are ordered by nameSum
            output.Position = entryOffset;
            WriteEntries(entries.OrderBy(x => x.nameSum).ToArray(), bw);

            // Write header
            var header = new DpkHeader
            {
                fileCount = files.Count,
                fileSize = (int)output.Length
            };

            output.Position = 0;
            WriteHeader(header, bw);
        }

        private IArchiveFile CreateAfi(Stream file, string name, DpkEntry entry)
        {
            if (entry.decompSize != entry.compSize)
                return new ArchiveFile(new CompressedArchiveFileInfo
                {
                    FilePath = name,
                    FileData = file,
                    Compression = Compressions.Wp16.Build(),
                    DecompressedSize = entry.decompSize,
                    PluginIds = Path.GetExtension(name) == ".PCK" ? [Guid.Parse("16951227-46b9-436c-9a02-1016ee6ffda3")] : null
                });

            return new ArchiveFile(new ArchiveFileInfo
            {
                FilePath = name,
                FileData = file,
                PluginIds = Path.GetExtension(name) == ".PCK" ? [Guid.Parse("16951227-46b9-436c-9a02-1016ee6ffda3")] : null
            });
        }

        private string CapAndPadName(string input)
        {
            return input.PadRight(0x16, '\0')[..0x16];
        }

        private int NameSum(string input)
        {
            return input.Sum(x => (byte)x);
        }

        private DpkHeader ReadHeader(BinaryReaderX reader)
        {
            return new DpkHeader
            {
                fileCount = reader.ReadInt32(),
                fileSize = reader.ReadInt32()
            };
        }

        private DpkEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new DpkEntry[count];

            for (var i = 0; i < count; i++)
            {
                result[i] = ReadEntry(reader);
                reader.SeekAlignment(0x80);
            }

            return result;
        }

        private DpkEntry ReadEntry(BinaryReaderX reader)
        {
            return new DpkEntry
            {
                name = reader.ReadString(0x16),
                nameSum = reader.ReadInt16(),
                offset = reader.ReadInt32(),
                compSize = reader.ReadInt32(),
                decompSize = reader.ReadInt32()
            };
        }

        private void WriteHeader(DpkHeader header, BinaryWriterX writer)
        {
            writer.Write(header.fileCount);
            writer.Write(header.fileSize);
        }

        private void WriteEntries(DpkEntry[] entries, BinaryWriterX writer)
        {
            foreach (DpkEntry entry in entries)
            {
                WriteEntry(entry, writer);
                writer.WriteAlignment(0x80);
            }
        }

        private void WriteEntry(DpkEntry entry, BinaryWriterX writer)
        {
            writer.WriteString(entry.name, writeNullTerminator: false);
            writer.Write(entry.nameSum);
            writer.Write(entry.offset);
            writer.Write(entry.compSize);
            writer.Write(entry.decompSize);
        }
    }
}
