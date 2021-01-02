using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using plugin_capcom.Compression;

namespace plugin_capcom.Archives
{
    class AAPack
    {
        private static readonly int FileEntrySize = Tools.MeasureType(typeof(AAPackFileEntry));
        private static readonly Regex FileNameRegex = new Regex("\\d{8}\\.bin");

        public IList<IArchiveFileInfo> Load(Stream incStream, Stream datStream, string version)
        {
            using var incBr = new BinaryReaderX(incStream);

            var entryCount = (int)(incStream.Length / FileEntrySize);
            var entries = incBr.ReadMultiple<AAPackFileEntry>(entryCount);

            var nameMapping = AAPackSupport.GetMapping(version);

            var result = new IArchiveFileInfo[entryCount];
            for (var i = 0; i < entryCount; i++)
            {
                var subStream = new SubStream(datStream, entries[i].offset, entries[i].compSize);

                var compressionMethod = NintendoCompressor.PeekCompressionMethod(subStream);

                var fileName = $"{i:00000000}.bin";
                if (nameMapping.ContainsKey(entries[i].hash))
                    fileName = nameMapping[entries[i].hash];

                result[i] = new AAPackArchiveFileInfo(subStream, fileName,
                    NintendoCompressor.GetConfiguration(compressionMethod), entries[i].uncompSize, entries[i]);
            }

            return result;
        }

        public void Save(Stream incStream, Stream datStream, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(incStream);

            foreach (var file in files.Cast<AAPackArchiveFileInfo>())
            {
                file.Entry.offset = (uint)datStream.Position;
                var writtenSize = file.SaveFileData(datStream);

                file.Entry.hash = IsUnmappedFile(file.FilePath.ToRelative().FullName) ? file.Entry.hash : AAPackSupport.CreateHash(file.FilePath.ToRelative().FullName);
                file.Entry.compSize = (uint)writtenSize;
                file.Entry.uncompSize = (uint)file.FileSize;
                bw.WriteType(file.Entry);
            }
        }

        private bool IsUnmappedFile(string input)
        {
            return FileNameRegex.IsMatch(input);
        }
    }
}
