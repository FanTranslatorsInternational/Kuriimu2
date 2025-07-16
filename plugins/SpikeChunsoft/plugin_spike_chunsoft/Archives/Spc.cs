using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_spike_chunsoft.Archives
{
    class Spc
    {
        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = ReadHeader(br);

            // Read Root entry
            input.Position = 0x20;
            var rootEntry = ReadEntry(br);
            br.SeekAlignment();

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < rootEntry.decompSize; i++)
            {
                var entry = ReadEntry(br);
                br.SeekAlignment();

                var fileStream = new SubStream(input, input.Position, entry.compSize);
                var fileName = entry.name;

                input.Position += entry.compSize;
                br.SeekAlignment();

                switch (entry.flag)
                {
                    case 1:
                        result.Add(new SpcArchiveFile(new ArchiveFileInfo
                        {
                            FilePath = fileName,
                            FileData = fileStream
                        }, entry));
                        break;

                    case 2:
                        result.Add(new SpcArchiveFile(new CompressedArchiveFileInfo
                        {
                            FilePath = fileName,
                            FileData = fileStream,
                            Compression = Compressions.Danganronpa3.Build(),
                            DecompressedSize = entry.decompSize
                        }, entry));
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown entry flag {entry.flag}.");
                }
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var rootOffset = 0x20;
            var dataOffset = 0x50;

            // Write entries and files
            var dataPosition = (long)dataOffset;
            foreach (var file in files.Cast<SpcArchiveFile>())
            {
                var fileName = file.FilePath.ToRelative().FullName;
                var entryLength = (0x20 + fileName.Length + 0xF) & ~0xF;

                // Write file data
                output.Position = dataPosition + entryLength;
                var writtenSize = file.WriteFileData(output, true);

                bw.WriteAlignment(0x10);
                var nextDataPosition = output.Position;

                // Write entry
                var entry = new SpcEntry
                {
                    flag = (short)(file.UsesCompression ? 2 : 1),
                    unk1 = file.Entry.unk1,
                    compSize = (int)writtenSize,
                    decompSize = (int)file.FileSize,
                    nameLength = fileName.Length,
                    name = fileName
                };

                output.Position = dataPosition;
                WriteEntry(entry, bw);

                dataPosition = nextDataPosition;
            }

            // Write root entry
            var root = new SpcEntry
            {
                decompSize = files.Count,
                nameLength = 4,
                name = "Root"
            };

            output.Position = rootOffset;
            WriteEntry(root, bw);

            // Write header
            output.Position = 0;
            WriteHeader(new SpcHeader(), bw);
        }

        private SpcHeader ReadHeader(BinaryReaderX reader)
        {
            return new SpcHeader
            {
                magic = reader.ReadString(4),
                zero0 = reader.ReadInt32(),
                unk1 = reader.ReadInt64()
            };
        }

        private SpcEntry ReadEntry(BinaryReaderX reader)
        {
            var entry = new SpcEntry
            {
                flag = reader.ReadInt16(),
                unk1 = reader.ReadInt16(),
                compSize = reader.ReadInt32(),
                decompSize = reader.ReadInt32(),
                nameLength = reader.ReadInt32(),
                zero0 = reader.ReadBytes(0x10)
            };

            entry.name = reader.ReadString(entry.nameLength);

            return entry;
        }

        private void WriteHeader(SpcHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.zero0);
            writer.Write(header.unk1);
        }

        private void WriteEntry(SpcEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.flag);
            writer.Write(entry.unk1);
            writer.Write(entry.compSize);
            writer.Write(entry.decompSize);
            writer.Write(entry.nameLength);
            writer.Write(entry.zero0);

            writer.WriteString(entry.name, writeNullTerminator: false);
        }
    }
}
