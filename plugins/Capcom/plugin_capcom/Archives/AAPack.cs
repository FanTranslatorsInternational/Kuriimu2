using System.Collections.Generic;
using System.IO;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using plugin_capcom.Compression;

namespace plugin_capcom.Archives
{
    class AAPack
    {
        private static readonly int FileEntrySize = Tools.MeasureType(typeof(AAPackFileEntry));

        public IList<ArchiveFileInfo> Load(Stream incStream, Stream datStream, string version)
        {
            using var incBr = new BinaryReaderX(incStream);

            var entryCount = (int)(incStream.Length / FileEntrySize);
            var entries = incBr.ReadMultiple<AAPackFileEntry>(entryCount);

            var nameMapping = AAPackSupport.GetMapping(version);

            var result = new ArchiveFileInfo[entryCount];
            for (var i = 0; i < entryCount; i++)
            {
                var subStream = new SubStream(datStream, entries[i].offset, entries[i].compSize);

                var compressionMethod = NintendoCompressor.PeekCompressionMethod(subStream);

                var fileName = $"{i:00000000}{AAPackSupport.DetermineExtension(subStream)}";
                if (nameMapping.ContainsKey(entries[i].hash))
                    fileName = nameMapping[entries[i].hash];

                result[i] = new ArchiveFileInfo(subStream, fileName,
                    NintendoCompressor.GetConfiguration(compressionMethod), entries[i].uncompSize);
            }

            return result;
        }
    }
}
