using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;

namespace plugin_beeworks.Archives
{
    class TD3
    {
        private static readonly int HeaderSize = 0x10;
        private static readonly int EntrySize = Tools.MeasureType(typeof(TD3Entry));

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<TD3Header>();

            // Read entries
            var entries = br.ReadMultiple<TD3Entry>(header.fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                var subStream = new SubStream(input, entry.offset, entry.size);
                var fileName = entry.fileName.Trim('\0');

                result.Add(new ArchiveFileInfo(subStream, fileName));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var fileOffset = (entryOffset + files.Count * EntrySize + 0xF) & ~0xF;

            // Write files
            var entries = new List<TD3Entry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);

                entries.Add(new TD3Entry
                {
                    offset = filePosition,
                    size = (int)writtenSize,
                    fileName = file.FilePath.ToRelative().FullName.PadRight(0x40, '\0')
                });

                filePosition += ((int)writtenSize + 0xF) & ~0xF;
            }

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write header
            output.Position = 0;
            bw.WriteType(new TD3Header
            {
                fileCount = files.Count
            });
        }
    }
}
