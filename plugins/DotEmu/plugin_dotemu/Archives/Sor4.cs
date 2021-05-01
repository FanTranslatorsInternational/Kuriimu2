using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kompression.Implementations;
using Kontract.Models.Archive;

namespace plugin_dotemu.Archives
{
    class Sor4
    {
        public IList<IArchiveFileInfo> Load(Stream texStream, Stream texListStream)
        {
            using var texBr = new BinaryReaderX(texStream, true);
            using var texListBr = new BinaryReaderX(texListStream, Encoding.Unicode);

            // Read entries
            var entries = new List<Sor4Entry>();
            while (texListStream.Position < texListStream.Length)
                entries.Add(Sor4Entry.Read(texListBr));

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                texStream.Position = entry.offset;
                var decompSize = texBr.ReadInt32();

                var fileStream = new SubStream(texStream, entry.offset + 4, entry.compSize - 4);
                result.Add(new Sor4ArchiveFileInfo(fileStream, entry.path, entry, Compressions.Deflate, decompSize));
            }

            return result;
        }

        public void Save(Stream texStream, Stream texListStream, IList<IArchiveFileInfo> files)
        {
            using var texBw = new BinaryWriterX(texStream);
            using var texListBw = new BinaryWriterX(texListStream);

            // Write files
            var dataPosition = 0;

            var entries = new List<Sor4Entry>();
            foreach (var file in files.Cast<Sor4ArchiveFileInfo>())
            {
                // Write data
                texStream.Position = dataPosition;
                texBw.Write((int)file.FileSize);
                var writtenSize = file.SaveFileData(texStream);

                // Update entry
                file.Entry.compSize = (int)writtenSize + 4;
                file.Entry.offset = dataPosition;
                entries.Add(file.Entry);

                dataPosition = (int)(dataPosition + 4 + writtenSize + 0xF) & ~0xF;
            }

            // Write entries
            foreach (var entry in entries)
                entry.Write(texListBw);
        }
    }
}
