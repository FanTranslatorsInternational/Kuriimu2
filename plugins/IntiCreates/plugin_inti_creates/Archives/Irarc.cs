using System.Buffers.Binary;
using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_inti_creates.Archives
{
    class Irarc
    {
        public List<IArchiveFile> Load(Stream lstStream, Stream arcStream)
        {
            using var br = new BinaryReaderX(lstStream);

            // Read entries
            var entryCount = br.ReadInt32();
            var entries = ReadEntries(br, entryCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < entryCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(arcStream, entry.offset, entry.size);
                var name = $"{i:00000000}.vap";

                result.Add(CreateAfi(subStream, name, entry));
            }

            return result;
        }

        public void Save(Stream lstStream, Stream arcStream, IList<IArchiveFile> files)
        {
            using var lstBw = new BinaryWriterX(lstStream);

            // Write files
            var entries = new List<IrarcFileEntry>();
            foreach (var file in files.Cast<IrarcArchiveFile>())
            {
                var offset = arcStream.Position;
                var writtenSize = file.WriteFileData(arcStream);

                entries.Add(new IrarcFileEntry
                {
                    id = file.Entry.id,
                    flags = file.Entry.flags,
                    offset = (int)offset,
                    size = (int)writtenSize
                });
            }

            // Write entries
            lstBw.Write(entries.Count);
            WriteEntries(entries, lstBw);
        }

        private IArchiveFile CreateAfi(Stream file, string name, IrarcFileEntry entry)
        {
            if (entry.IsCompressed)
            {
                file.Position = 0xC;
                var decompressedSize = PeekInt32(file);

                file = new SubStream(file, 0x18, file.Length - 0x18);

                return new IrarcArchiveFile(new CompressedArchiveFileInfo
                {
                    FilePath = name,
                    FileData = file,
                    Compression = Compressions.IrLz.Build(),
                    DecompressedSize = decompressedSize,
                    PluginIds = [Guid.Parse("e38a0292-5e7d-457f-8795-8e0a1c44900f")]
                }, entry);
            }

            return new IrarcArchiveFile(new ArchiveFileInfo
            {
                FilePath = name,
                FileData = file,
                PluginIds = [Guid.Parse("e38a0292-5e7d-457f-8795-8e0a1c44900f")]
            }, entry);
        }

        private int PeekInt32(Stream input)
        {
            var bkPos = input.Position;

            var buffer = new byte[4];
            _ = input.Read(buffer, 0, 4);

            input.Position = bkPos;

            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        private IrarcFileEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new IrarcFileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private IrarcFileEntry ReadEntry(BinaryReaderX reader)
        {
            return new IrarcFileEntry
            {
                id = reader.ReadInt32(),
                offset = reader.ReadInt32(),
                size = reader.ReadInt32(),
                flags = reader.ReadInt32()
            };
        }

        private void WriteEntries(IList<IrarcFileEntry> entries, BinaryWriterX writer)
        {
            foreach (IrarcFileEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(IrarcFileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.id);
            writer.Write(entry.offset);
            writer.Write(entry.size);
            writer.Write(entry.flags);
        }
    }
}
