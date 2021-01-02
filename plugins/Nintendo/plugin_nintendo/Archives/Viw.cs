using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using plugin_nintendo.Compression;

namespace plugin_nintendo.Archives
{
    class Viw
    {
        private static readonly int InfHeaderSize = Tools.MeasureType(typeof(ViwInfHeader));
        private static readonly int InfEntrySize = Tools.MeasureType(typeof(ViwInfEntry));

        private IList<ViwInfMetaEntry> _metas;
        private IList<ViwEntry> _nameEntries;

        public IList<IArchiveFileInfo> Load(Stream viwStream, Stream infStream, Stream dataStream)
        {
            using var infBr = new BinaryReaderX(infStream);
            using var viwBr = new BinaryReaderX(viwStream);

            // Read inf header
            var infHeader = infBr.ReadType<ViwInfHeader>();

            // Read entries
            infStream.Position = infHeader.entryOffset;
            var entries = infBr.ReadMultiple<ViwInfEntry>(infHeader.fileCount);

            // Read meta entries
            infStream.Position = infHeader.metaOffset;
            _metas = infBr.ReadMultiple<ViwInfMetaEntry>(infHeader.metaCount);

            // Read name entries
            _nameEntries = viwBr.ReadMultiple<ViwEntry>(infHeader.metaCount <= 0 ? infHeader.fileCount : infHeader.metaCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < infHeader.fileCount; i++)
            {
                var entry = entries[i];
                var nameEntry = i < _nameEntries.Count ? _nameEntries[i] : null;

                var subStream = new SubStream(dataStream, entry.offset, entry.compSize);
                var fileName = (infHeader.fileCount != _nameEntries.Count ? (_nameEntries[0].id + i).ToString("X4") : nameEntry.name.Trim(' ', '\0')) + ViwSupport.DetermineExtension(subStream);

                result.Add(CreateAfi(subStream, fileName));
            }

            return result;
        }

        public void Save(Stream viwStream, Stream infStream, Stream dataStream, IList<IArchiveFileInfo> files)
        {
            using var infBw = new BinaryWriterX(infStream);
            using var viwBw = new BinaryWriterX(viwStream);

            // Calculate offset
            var entryOffset = InfHeaderSize;
            var metaOffset = entryOffset + files.Count * InfEntrySize;

            // Write files
            var entries = new List<ViwInfEntry>();

            var filePosition = 0;
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                dataStream.Position = filePosition;
                var writtenSize = file.SaveFileData(dataStream);

                entries.Add(new ViwInfEntry
                {
                    offset = filePosition,
                    compSize = (int)writtenSize
                });

                filePosition += (int)((writtenSize + 3) & ~3);
            }

            // Write metas
            infStream.Position = metaOffset;
            infBw.WriteMultiple(_metas);

            // Write entries
            infStream.Position = entryOffset;
            infBw.WriteMultiple(entries);

            // Write inf header
            infStream.Position = 0;
            infBw.WriteType(new ViwInfHeader
            {
                fileCount = files.Count,
                metaCount = _metas.Count,
                entryOffset = entryOffset,
                metaOffset = metaOffset
            });

            // Write name entries
            viwBw.WriteMultiple(_nameEntries);
        }

        private IArchiveFileInfo CreateAfi(Stream file, string name)
        {
            file.Position = 0;

            var method = NintendoCompressor.PeekCompressionMethod(file);
            var size = NintendoCompressor.PeekDecompressedSize(file);

            return new ArchiveFileInfo(file, name, NintendoCompressor.GetConfiguration(method), size);
        }
    }
}
