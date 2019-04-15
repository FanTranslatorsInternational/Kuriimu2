using Komponent.IO;
using Kontract.Interfaces.Archive;
using System.Collections.Generic;
using System.IO;

namespace plugin_yuusha_shisu.PAC
{
    public class PAC
    {
        private const int _entryAlignment = 0x20;
        private const int _fileAlignment = 0x80;

        private FileHeader _header;
        private List<FileEntry> _entries;

        /// <summary>
        /// The files contained within this PAC archive.
        /// </summary>
        public List<ArchiveFileInfo> Files { get; } = new List<ArchiveFileInfo>();

        /// <summary>
        /// Loads the metadata and files from a PAC archive.
        /// </summary>
        /// <param name="input">An input stream for a PAC archive.</param>
        public PAC(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                // Header
                _header = br.ReadType<FileHeader>();
                
                // Offsets
                var offsets = br.ReadMultiple<int>(_header.FileCount);
                br.SeekAlignment(_entryAlignment);
                
                // Entries
                _entries = br.ReadMultiple<FileEntry>(_header.FileCount);

                // Files
                for (int i = 0; i < offsets.Count; i++)
                {
                    br.BaseStream.Position = offsets[i];
                    var length = br.ReadInt32();
                    var off = br.BaseStream.Position + _fileAlignment - sizeof(int);

                    Files.Add(new ArchiveFileInfo
                    {
                        FileName = _entries[i].FileName.Trim('\0'),
                        FileData = new SubStream(br.BaseStream, off, length),
                        State = ArchiveFileState.Archived
                    });
                }
            }
        }

        /// <summary>
        /// Saves the metadata and files into a PAC archive.
        /// </summary>
        /// <param name="output">An output stream for a PAC archive.</param>
        /// <returns>True if successful.</returns>
        public bool Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output, true))
            {
                // Header
                bw.WriteType(_header);
                var offsetPosition = bw.BaseStream.Position;

                // Skip Offsets
                bw.BaseStream.Position += _header.FileCount * sizeof(int);
                bw.WriteAlignment(_entryAlignment);

                // Entries
                bw.WriteMultiple(_entries);
                bw.WriteAlignment(_fileAlignment);

                // Files
                var offsets = new List<int>();
                foreach(var afi in Files)
                {
                    offsets.Add((int)bw.BaseStream.Position);
                    bw.Write((int)afi.FileSize);
                    bw.Write(_fileAlignment);
                    bw.WriteAlignment(_fileAlignment);
                    afi.FileData.CopyTo(bw.BaseStream);
                    bw.WriteAlignment(_fileAlignment);
                }

                // Offsets
                bw.BaseStream.Position = offsetPosition;
                bw.WriteMultiple(offsets);
            }
            return true;
        }
    }
}
