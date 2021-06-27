using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;

namespace plugin_koei_tecmo.Archives
{
    class Crae
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(CraeHeader));
        private static readonly int EntrySize = Tools.MeasureType(typeof(CraeEntry));

        private CraeHeader _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<CraeHeader>();

            // Read entries
            input.Position = _header.entryOffset;
            var entries = br.ReadMultiple<CraeEntry>(_header.fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                var fileStream = new SubStream(input, entry.offset, entry.size);
                var fileName = entry.name.Trim('\0');

                result.Add(new ArchiveFileInfo(fileStream, fileName));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var dataOffset = entryOffset + EntrySize * files.Count;

            // Write files
            var entries = new List<CraeEntry>();

            var dataPosition = dataOffset;
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                // Write file data
                output.Position = dataPosition;
                file.SaveFileData(output);

                // Add entry
                entries.Add(new CraeEntry
                {
                    offset = dataPosition,
                    size = (int)file.FileSize,
                    name = file.FilePath.GetName()
                });

                dataPosition += (int)file.FileSize;
            }

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write header
            _header.dataOffset = dataOffset;
            _header.entryOffset = entryOffset;
            _header.fileCount = files.Count;
            _header.dataSize = (int)(output.Length - dataOffset);

            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
