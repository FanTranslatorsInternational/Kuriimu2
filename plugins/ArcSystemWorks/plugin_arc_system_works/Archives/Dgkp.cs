using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;

namespace plugin_arc_system_works.Archives
{
    class Dgkp
    {
        private static readonly int FileEntrySize = Tools.MeasureType(typeof(DgkpFileEntry));

        private DgkpHeader _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<DgkpHeader>();

            // Read entries
            br.BaseStream.Position = _header.entryOffset;
            var entries = br.ReadMultiple<DgkpFileEntry>(_header.fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                // There are 3 files in the game Chase: Cold Case Investigations which are 8 bytes short
                // The files are:
                // - naui/common_ui_000 - 복사본.pac
                // - naui/common_ui_000.pac
                // - naui/SearchConfirm.pac
                // This code works around this issue
                var size = entry.size;
                if (entry == entries.Last())
                    size = (int)Math.Min(input.Length - entry.offset, entry.size);

                var subStream = new SubStream(input, entry.offset, size);
                result.Add(new DgkpArchiveFileInfo(subStream, entry.name.TrimEnd('\0'), entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = _header.entryOffset;
            var fileOffset = entryOffset + files.Count * FileEntrySize;

            // Write files
            var entries = new List<DgkpFileEntry>();

            output.Position = fileOffset;
            foreach (var file in files.Cast<DgkpArchiveFileInfo>())
            {
                fileOffset = (int)output.Position;
                var writtenSize = file.SaveFileData(output);

                entries.Add(new DgkpFileEntry
                {
                    offset = fileOffset,
                    size = (int)writtenSize,
                    magic = file.Entry.magic,
                    name = file.FilePath.ToRelative().FullName.PadRight(0x80, '\0')
                });
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
