using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_cavia.Archives
{
    class Dg2Dpk
    {
        private const int Alignment_ = 0x800;
        private static readonly int EntrySize = Tools.MeasureType(typeof(DpkEntry));

        private DpkHeader _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<DpkHeader>();

            // Read entries
            input.Position = _header.entryOffset;
            var entries = br.ReadMultiple<DpkEntry>(_header.fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < _header.fileCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, entry.fileOffset, entry.fileSize);
                var name = $"{i:00000000}.bin";

                result.Add(new DpkArchiveFileInfo(subStream, name, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = _header.entryOffset;
            var fileOffset = (entryOffset + files.Count * EntrySize + Alignment_ - 1) & ~(Alignment_ - 1);

            _header.fileOffset = fileOffset;

            // Write files
            var entries = new List<DpkEntry>();

            output.Position = fileOffset;
            foreach (var file in files.Cast<DpkArchiveFileInfo>())
            {
                var writtenSize = file.SaveFileData(output);
                bw.WriteAlignment(Alignment_);

                file.Entry.fileOffset = fileOffset;
                file.Entry.fileSize = (int)writtenSize;
                file.Entry.padFileSize = (int)((writtenSize + Alignment_ - 1) & ~(Alignment_ - 1));

                entries.Add(file.Entry);

                fileOffset += file.Entry.padFileSize;
            }

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write header
            _header.fileCount = files.Count;

            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
