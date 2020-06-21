using System.Collections.Generic;
using System.IO;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_tri_ace.Archives
{
    // TODO: Test plugin
    // Game: Beyond The Labyrinth
    class Pack
    {
        private static int _headerSize = Tools.MeasureType(typeof(PackHeader));
        private static int _entrySize = Tools.MeasureType(typeof(PackFileEntry));

        public IReadOnlyList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<PackHeader>();

            // Read file entries
            var entries = br.ReadMultiple<PackFileEntry>(header.fileCount + 1);

            // Add files
            var result = new List<ArchiveFileInfo>();
            for (var i = 0; i < header.fileCount; i++)
            {
                var fileStream = new SubStream(input, entries[i].offset, entries[i + 1].offset - entries[i].offset);
                var extension = DetermineExtension(entries[i].fileType);

                result.Add(new ArchiveFileInfo(fileStream, i.ToString("00000000") + extension)
                {
                    PluginIds = PackSupport.RetrievePluginMapping(entries[i].fileType)
                });
            }

            return result;
        }

        public void Save(Stream output, IReadOnlyList<ArchiveFileInfo> files)
        {
        }

        private string DetermineExtension(int fileType)
        {
            switch (fileType)
            {
                case 0x2:
                    return ".pack";

                case 0x20:
                case 0x30:
                case 0x40:
                    return ".cgfx";

                case 0x400:
                    return ".mpak8";

                default:
                    return ".bin";
            }
        }
    }
}
