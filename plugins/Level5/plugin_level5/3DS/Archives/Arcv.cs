using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_level5._3DS.Archives
{
    // Game: Inazuma 3 Ogre Team
    class Arcv
    {
        private readonly int _headerSize = Tools.MeasureType(typeof(ArcvHeader));
        private readonly int _entrySize = Tools.MeasureType(typeof(ArcvFileInfo));

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<ArcvHeader>();

            // Read entries
            var entries = br.ReadMultiple<ArcvFileInfo>(header.fileCount);

            var files = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                var fileStream = new SubStream(input, entry.offset, entry.size);
                files.Add(new ArcvArchiveFileInfo(fileStream, $"{files.Count:00000000}.bin", entry));
            }

            return files;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            var castedFiles = files.Cast<ArcvArchiveFileInfo>().ToArray();
            using var bw = new BinaryWriterX(output);

            bw.BaseStream.Position = (_headerSize + files.Count * _entrySize + 0x7F) & ~0x7F;

            // Write files
            foreach (var file in castedFiles)
            {
                file.Entry.offset = (int)bw.BaseStream.Position;
                file.Entry.size = (int)file.FileSize;

                file.SaveFileData(bw.BaseStream, null);
            }

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(new ArcvHeader
            {
                fileSize = (int)output.Length,
                fileCount = files.Count
            });

            // Write file entries
            foreach (var file in castedFiles)
                bw.WriteType(file.Entry);

            // Pad with 0xAF to first file
            bw.WriteAlignment(0x80, 0xAC);
        }
    }
}
