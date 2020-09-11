using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_inti_creates.Archives
{
    class Irarc
    {
        public IList<ArchiveFileInfo> Load(Stream lstStream, Stream arcStream)
        {
            using var br = new BinaryReaderX(lstStream);

            // Read entries
            var entryCount = br.ReadInt32();
            var entries = br.ReadMultiple<IrarcFileEntry>(entryCount);

            // Add files
            var result = new List<ArchiveFileInfo>();
            for (var i = 0; i < entryCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(arcStream, entry.offset, entry.size);
                var name = $"{i:00000000}.bin";

                result.Add(CreateAfi(subStream, name, entry));
            }

            return result;
        }

        public void Save(Stream lstStream, Stream arcStream, IList<ArchiveFileInfo> files)
        {
            using var lstBw = new BinaryWriterX(lstStream);

            // Write files
            var entries = new List<IrarcFileEntry>();
            foreach (var file in files.Cast<IrarcArchiveFileInfo>())
            {
                var offset = arcStream.Position;
                var writtenSize = file.SaveFileData(arcStream);

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
            lstBw.WriteMultiple(entries);
        }

        private ArchiveFileInfo CreateAfi(Stream file, string name, IrarcFileEntry entry)
        {
            if (entry.IsCompressed)
            {
                file.Position = 0xC;
                var decompressedSize = PeekInt32(file);

                file = new SubStream(file, 0x18, file.Length - 0x18);

                return new IrarcArchiveFileInfo(file, name, entry, Kompression.Implementations.Compressions.IrLz, decompressedSize);
            }

            return new IrarcArchiveFileInfo(file, name, entry);
        }

        private int PeekInt32(Stream input)
        {
            var bkPos = input.Position;

            var buffer = new byte[4];
            input.Read(buffer, 0, 4);

            input.Position = bkPos;

            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }
    }
}
