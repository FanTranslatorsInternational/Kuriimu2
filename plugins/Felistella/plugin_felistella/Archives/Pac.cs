using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_felistella.Archives
{
    class Pac
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(PacHeader));
        private static readonly int NameSize = Tools.MeasureType(typeof(PacDirectoryEntry));
        private static readonly int EntrySize = 8;

        private PacHeader _header;
        private IList<PacDirectoryEntry> _dirs;
        private IList<PacEntry> _entries;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<PacHeader>();

            // Read names
            input.Position = _header.nameOffset;
            _dirs = br.ReadMultiple<PacDirectoryEntry>(_header.nameCount);

            // Read entries
            input.Position = _header.entryOffset;
            _entries = br.ReadMultiple<PacEntry>(_header.entryCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < _header.entryCount; i++)
            {
                var entry = _entries[i];
                if (entry.offset == 0 && entry.size == 0)
                    continue;

                var dirEntry = _dirs.FirstOrDefault(x => i >= x.entryIndex && i < x.entryIndex + x.entryCount);
                var name = (dirEntry?.name.Trim() ?? "") + "/" + $"{i:00000000}.bin";

                var size = entry.size * ((entry.flags & 0x100) == 0 ? 0x20 : 1);
                var fileStream = new SubStream(input, entry.offset * 0x20, size);

                result.Add(new PacArchiveFileInfo(fileStream, name,entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var nameOffset = HeaderSize;
            var entryOffset = nameOffset + _dirs.Count * NameSize;
            var dataOffset = (entryOffset + _entries.Count * EntrySize + 0x1F) & ~0x1F;

            // Write files
            var dataPosition = dataOffset;
            foreach (var file in files.Cast<PacArchiveFileInfo>())
            {
                // Write data
                output.Position = dataPosition;
                var writtenSize = file.SaveFileData(output);
                bw.WriteAlignment(0x20);

                // Update entry
                file.Entry.offset = dataPosition / 0x20;
                file.Entry.size = (int)((file.Entry.flags & 0x100) == 0 ? writtenSize / 0x20 : writtenSize);

                dataPosition += (int)((writtenSize + 0x1F) & ~0x1F);
            }

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(_entries);

            // Write dirs
            output.Position = nameOffset;
            bw.WriteMultiple(_dirs);

            // Write header
            _header.nameOffset = _dirs.Count > 0 ? nameOffset : 0;
            _header.nameCount = _dirs.Count;
            _header.entryOffset = entryOffset;
            _header.entryCount = _entries.Count;
            _header.dataSize = (int)(output.Length - 0x10);
            _header.fileSize = (int)output.Length;
            _header.blockCount = (int)(output.Length / 0x20);

            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
