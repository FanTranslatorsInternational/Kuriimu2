using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_koei_tecmo.Archives
{
    class Idx
    {
        public IList<IArchiveFileInfo> Load(Stream idxStream, Stream binStream)
        {
            using var br = new BinaryReaderX(idxStream);
            using var binBr = new BinaryReaderX(binStream, true);

            // Read entries
            var entries = br.ReadMultiple<IdxEntry>((int)(idxStream.Length / 8));

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                var fileStream = new SubStream(binStream, entry.offset, entry.size);
                var fileName = $"{i:00000000}{IdxSupport.DetermineExtension(fileStream)}";

                result.Add(new ArchiveFileInfo(fileStream, fileName));
            }

            return result;
        }

        public void Save(Stream idxStream, Stream binStream, IList<IArchiveFileInfo> files)
        {
            // Write files
            var entries = new List<IdxEntry>();

            var dataPosition = 0;
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                // Write file data
                binStream.Position = dataPosition;
                var writtenSize = file.SaveFileData(binStream);

                // Add entry
                entries.Add(new IdxEntry { offset = dataPosition, size = (int)writtenSize });

                dataPosition += (int)writtenSize;
            }

            // Write entries
            using var bw = new BinaryWriterX(idxStream);
            bw.WriteMultiple(entries);
        }
    }
}
