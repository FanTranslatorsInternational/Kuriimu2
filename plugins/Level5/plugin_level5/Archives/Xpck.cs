using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using plugin_level5.Compression;

namespace plugin_level5.Archives
{
    // TODO: Recreate name table and enable adding files
    class Xpck
    {
        private XpckHeader _header;
        private IList<XpckFileInfo> _entries;
        private Stream _compNameTable;

        public IReadOnlyList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Header
            _header = br.ReadType<XpckHeader>();

            // Entries
            br.BaseStream.Position = _header.FileInfoOffset;
            _entries = br.ReadMultiple<XpckFileInfo>(_header.FileCount);

            // File names
            _compNameTable = new SubStream(input, _header.FilenameTableOffset, _header.FilenameTableSize);
            var decNames = new MemoryStream();
            Compressor.Decompress(_compNameTable, decNames);

            // Files
            using var nameList = new BinaryReaderX(decNames);

            var files = new List<ArchiveFileInfo>();
            foreach (var entry in _entries)
            {
                nameList.BaseStream.Position = entry.nameOffset;
                var name = nameList.ReadCStringASCII();

                var fileData = new SubStream(input, _header.DataOffset + entry.FileOffset, entry.FileSize);
                files.Add(new XpckArchiveFileInfo(fileData, name, entry)
                {
                    PluginIds = XpckSupport.RetrievePluginMapping(name)
                });
            }

            return files;
        }

        public void Save(Stream output, IReadOnlyList<ArchiveFileInfo> files)
        {
            var castedFiles = files.Cast<XpckArchiveFileInfo>().ToArray();
            using var bw = new BinaryWriterX(output);

            // Files
            var fileOffset = (int)_header.DataOffset;
            foreach (var file in castedFiles.OrderBy(x => x.FileEntry.FileOffset))
            {
                output.Position = fileOffset;
                file.SaveFileData(output, null);

                var relativeOffset = fileOffset - _header.DataOffset;
                file.FileEntry.FileOffset = relativeOffset;
                file.FileEntry.FileSize = (int)file.FileSize;

                fileOffset = (ushort)output.Length;
            }

            // Entries
            bw.BaseStream.Position = 0x14;
            foreach (var file in castedFiles)
                bw.WriteType(file.FileEntry);

            // File names
            _compNameTable.Position = 0;
            _compNameTable.CopyTo(output);

            // Header
            _header.DataSize = (uint)(bw.BaseStream.Length - _header.DataOffset);
            bw.BaseStream.Position = 0;
            bw.WriteType(_header);
        }
    }
}
