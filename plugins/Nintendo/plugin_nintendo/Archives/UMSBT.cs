using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_nintendo.Archives
{
    class UMSBT
    {
        private static readonly int EntrySize = Tools.MeasureType(typeof(UMSBTEntry));

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read first offset
            var firstOffset = br.ReadInt32();

            // Read entries
            input.Position = 0;

            var entries = new List<UMSBTEntry>();
            while (input.Position < firstOffset)
            {
                var entry = br.ReadType<UMSBTEntry>();
                if (entry.size <= 0)
                    break;

                entries.Add(entry);
            }

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, entry.offset, entry.size);
                var fileName = $"{i:00000000}.msbt";

                result.Add(new ArchiveFileInfo(subStream, fileName));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var fileOffset = Math.Max(0x30, (files.Count * EntrySize + 0xF) & ~0xF);

            // Write files
            var entries = new List<UMSBTEntry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);

                entries.Add(new UMSBTEntry
                {
                    offset = filePosition,
                    size = (int)writtenSize
                });

                filePosition += (int)writtenSize;
            }

            // Write entries
            output.Position = 0;
            bw.WriteMultiple(entries);
        }
    }
}
