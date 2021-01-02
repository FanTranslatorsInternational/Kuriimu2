using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_capcom.Archives
{
    /*
     * List of games/updates that use this format on Mobile:
     * DGS1 v1.00.00, v1.00.01
     * DGS1 v1.00.02 has content differences
     * DGS2
     * Dual Destinies
     * Spirit of Justice
     */

    // HINT: This format is used for all 3D Ace Attorney games on mobile platforms
    // HINT: Those games have 2 OBB's, one for videos and one for assets
    // HINT: The video OBB is a normal zip, while the asset OBB is of this format
    class Obb
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(ObbHeader));
        private static readonly int EntrySize = Tools.MeasureType(typeof(ObbEntry));

        private ObbHeader _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<ObbHeader>();

            // Read entries
            var entries = br.ReadMultiple<ObbEntry>(_header.fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                var subStream = new SubStream(input, entry.offset, entry.size);
                var fileName = $"{entry.pathHash:X8}{ObbSupport.DetermineExtension(subStream)}";

                result.Add(new ObbArchiveFileInfo(subStream, fileName, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var fileOffset = entryOffset + files.Count * EntrySize;

            // Write files
            var entries = new List<ObbEntry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<ObbArchiveFileInfo>())
            {
                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);

                file.Entry.offset = filePosition;
                file.Entry.size = (int)writtenSize;
                entries.Add(file.Entry);

                filePosition += (int)writtenSize;
            }

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write header
            output.Position = 0;

            _header.fileCount = files.Count;
            bw.WriteType(_header);
        }
    }
}
