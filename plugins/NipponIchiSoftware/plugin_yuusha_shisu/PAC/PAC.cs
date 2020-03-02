using Komponent.IO;
using System.Collections.Generic;
using System.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_yuusha_shisu.PAC
{
    /// <summary>
    /// 
    /// </summary>
    public class Pac
    {
        private const int EntryAlignment = 0x20;
        private const int FileAlignment = 0x80;

        private FileHeader _header;
        private List<FileEntry> _entries;

        public IReadOnlyList<ArchiveFileInfo> Load(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                // Header
                _header = br.ReadType<FileHeader>();

                // Offsets
                var offsets = br.ReadMultiple<int>(_header.FileCount);
                br.SeekAlignment(EntryAlignment);

                // Entries
                _entries = br.ReadMultiple<FileEntry>(_header.FileCount);

                // Files
                var result = new List<ArchiveFileInfo>();
                for (var i = 0; i < offsets.Count; i++)
                {
                    br.BaseStream.Position = offsets[i];
                    var length = br.ReadInt32();
                    var off = br.BaseStream.Position + FileAlignment - sizeof(int);

                    // TODO: Add plugin Id to each *.msg file
                    result.Add(new ArchiveFileInfo(new SubStream(br.BaseStream, off, length),
                        _entries[i].FileName.Trim('\0')));
                }

                return result;
            }
        }

        /// <summary>
        /// Saves the metadata and files into a PAC archive.
        /// </summary>
        /// <param name="output">An output stream for a PAC archive.</param>
        /// <param name="files">The files to save.</param>
        /// <returns>True if successful.</returns>
        public bool Save(Stream output, IReadOnlyList<ArchiveFileInfo> files)
        {
            using (var bw = new BinaryWriterX(output, true))
            {
                // Header
                bw.WriteType(_header);
                var offsetPosition = bw.BaseStream.Position;

                // Skip Offsets
                bw.BaseStream.Position += _header.FileCount * sizeof(int);
                bw.WriteAlignment(EntryAlignment);

                // Entries
                bw.WriteMultiple(_entries);
                bw.WriteAlignment(FileAlignment);

                // Files
                var offsets = new List<int>();
                foreach (var afi in files)
                {
                    offsets.Add((int)bw.BaseStream.Position);
                    bw.Write((int)afi.FileSize);
                    bw.Write(FileAlignment);
                    bw.WriteAlignment(FileAlignment);
                    afi.SaveFileData(bw.BaseStream, null);
                    bw.WriteAlignment(FileAlignment);
                }

                // Offsets
                bw.BaseStream.Position = offsetPosition;
                bw.WriteMultiple(offsets);
            }

            return true;
        }
    }
}
