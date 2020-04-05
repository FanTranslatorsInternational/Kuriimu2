using System.Collections.Generic;
using System.IO;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_level5.Archives
{
    // TODO: Test plugin
    // Game: Professor Layton 3 on DS
    class Lpc2
    {
        private readonly int _headerSize = Tools.MeasureType(typeof(Lpc2Header));
        private readonly int _fileEntrySize = Tools.MeasureType(typeof(Lpc2FileEntry));

        public IReadOnlyList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<Lpc2Header>();

            // Read file entries
            br.BaseStream.Position = header.fileEntryOffset;
            var entries = br.ReadMultiple<Lpc2FileEntry>(header.fileCount);

            // Add files
            var result = new List<ArchiveFileInfo>();
            foreach (var entry in entries)
            {
                br.BaseStream.Position = header.nameOffset + entry.nameOffset;
                var name = br.ReadCStringASCII();

                var fileStream = new SubStream(input, header.dataOffset + entry.fileOffset, entry.fileSize);
                result.Add(new ArchiveFileInfo(fileStream, name)
                {
                    PluginIds = Lpc2Support.RetrievePluginMapping(name)
                });
            }

            return result;
        }

        public void Save(Stream output, IReadOnlyList<ArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            var fileEntryStartOffset = _headerSize;
            var nameStartOffset = _headerSize + files.Count * _fileEntrySize;

            // Write names
            var fileOffset = 0;
            var nameOffset = 0;
            var fileEntries = new List<Lpc2FileEntry>();
            foreach (var file in files)
            {
                fileEntries.Add(new Lpc2FileEntry
                {
                    fileOffset = fileOffset,
                    fileSize = (int)file.FileSize,
                    nameOffset = nameOffset
                });

                bw.BaseStream.Position = nameStartOffset + nameOffset;
                bw.WriteString(file.FilePath.FullName, Encoding.ASCII, false);
                nameOffset = (int)bw.BaseStream.Position - nameStartOffset;

                fileOffset += (int)file.FileSize;
            }

            // Write file data
            var dataOffset = (int)bw.BaseStream.Position;
            foreach (var file in files)
                file.SaveFileData(bw.BaseStream, null);

            // Write file entries
            bw.BaseStream.Position = fileEntryStartOffset;
            foreach (var fileEntry in fileEntries)
                bw.WriteType(fileEntry);

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(new Lpc2Header
            {
                fileEntryOffset = fileEntryStartOffset,
                nameOffset = nameStartOffset,
                dataOffset = dataOffset,

                fileCount = files.Count,

                headerSize = _headerSize,
                fileSize = (int)bw.BaseStream.Length
            });
        }
    }
}
