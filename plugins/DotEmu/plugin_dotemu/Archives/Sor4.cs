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
        private Platform _platform;

        public IList<IArchiveFileInfo> Load(Stream texStream, Stream texListStream, Platform platform)
        {
            _platform = platform;

            using var texBr = new BinaryReaderX(texStream, true);
            using var texListBr = new BinaryReaderX(texListStream, Encoding.Unicode);

            // Read entries
            var entries = new List<Sor4Entry>();
            while (texListStream.Position < texListStream.Length)
            {
                // TODO: Requires more research as to split texture files
                try
                {
                    entries.Add(texListBr.ReadType<Sor4Entry>());
                }
                catch
                {
                    break;
                }
            }

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                Stream fileStream = null;
                var decompSize = -1;

                switch (platform)
                {
                    case Platform.Pc:
                        fileStream = new SubStream(texStream, entry.offset, entry.compSize);
                        break;

                    case Platform.Switch:
                        texStream.Position = entry.offset;
                        decompSize = texBr.ReadInt32();

                        fileStream = new SubStream(texStream, entry.offset + 4, entry.compSize - 4);
                        break;
                }

                result.Add(new Sor4ArchiveFileInfo(fileStream, entry.path, entry, Compressions.Deflate, decompSize));
            }

            return result;
        }

        public void Save(Stream texStream, Stream texListStream, IList<IArchiveFileInfo> files)
        {
            using var texBw = new BinaryWriterX(texStream);
            using var texListBw = new BinaryWriterX(texListStream, Encoding.Unicode);

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
                file.Entry.compSize = (int)writtenSize + _platform == Platform.Pc ? 0 : 4;
                file.Entry.offset = dataPosition;
                entries.Add(file.Entry);

                switch (_platform)
                {
                    case Platform.Pc:
                        dataPosition = (int)(dataPosition + writtenSize);
                        break;

                    case Platform.Switch:
                        dataPosition = (int)(dataPosition + 4 + writtenSize + 0xF) & ~0xF;
                        break;
                }
            }

            // Write entries
            texListBw.WriteMultiple(entries);
        }
    }
}
