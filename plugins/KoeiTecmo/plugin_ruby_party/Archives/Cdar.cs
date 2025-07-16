using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_ruby_party.Archives
{
    class Cdar
    {
        private static readonly int HeaderSize = 0x10;
        private static readonly int FileEntrySize = 0xC;

        private CdarHeader _header;
        private IList<uint> _hashes;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadHeader(br);

            // Read hashes
            _hashes = ReadHashes(br, _header.entryCount);

            // Read entries
            var entries = ReadEntries(br, _header.entryCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < _header.entryCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, entry.offset, entry.size);
                var name = $"{i:00000000}.bin";

                result.Add(new ArchiveFile(new CompressedArchiveFileInfo
                {
                    FilePath = name,
                    FileData = subStream,
                    Compression = Compressions.ZLib.Build(),
                    DecompressedSize = entry.decompSize
                }));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var hashOffset = HeaderSize;
            var entryOffset = (hashOffset + _hashes.Count * 4 + 0xF) & ~0xF;
            var fileOffset = (entryOffset + files.Count * FileEntrySize + 0xF) & ~0xF;

            // Write files
            output.Position = fileOffset;

            var random = new Random();

            var entries = new List<CdarFileEntry>();
            foreach (var file in files)
            {
                fileOffset = (int)output.Position;
                var writtenSize = file.WriteFileData(output);

                if (files[^1] != file)
                {
                    while (output.Position % 0x10 > 0)
                        output.WriteByte((byte)random.Next());
                }

                entries.Add(new CdarFileEntry
                {
                    offset = fileOffset,
                    size = (int)writtenSize,
                    decompSize = (int)file.FileSize
                });
            }

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write hashes
            output.Position = hashOffset;
            WriteHashes(_hashes, bw);

            // Write header
            output.Position = 0;

            _header.entryCount = files.Count;
            WriteHeader(_header, bw);
        }

        private CdarHeader ReadHeader(BinaryReaderX reader)
        {
            return new CdarHeader
            {
                magic = reader.ReadString(4),
                unk1 = reader.ReadInt32(),
                entryCount = reader.ReadInt32(),
                unk2 = reader.ReadInt32(),
            };
        }

        private uint[] ReadHashes(BinaryReaderX reader, int count)
        {
            var result = new uint[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadUInt32();

            return result;
        }

        private CdarFileEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new CdarFileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private CdarFileEntry ReadEntry(BinaryReaderX reader)
        {
            return new CdarFileEntry
            {
                offset = reader.ReadInt32(),
                decompSize = reader.ReadInt32(),
                size = reader.ReadInt32()
            };
        }

        private void WriteHeader(CdarHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.unk1);
            writer.Write(header.entryCount);
            writer.Write(header.unk2);
        }

        private void WriteHashes(IList<uint> hashes, BinaryWriterX writer)
        {
            foreach (uint hash in hashes)
                writer.Write(hash);
        }

        private void WriteEntries(IList<CdarFileEntry> entries, BinaryWriterX writer)
        {
            foreach (CdarFileEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(CdarFileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.decompSize);
            writer.Write(entry.size);
        }
    }
}
