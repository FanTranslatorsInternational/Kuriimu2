using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;
using plugin_nintendo.Common.Compression;

namespace plugin_nintendo.Archives
{
    class Viw
    {
        private const int InfHeaderSize = 0x10;
        private const int InfEntrySize = 0x8;

        private IList<ViwInfMetaEntry> _metas;
        private IList<ViwEntry> _nameEntries;

        public List<IArchiveFile> Load(Stream viwStream, Stream infStream, Stream dataStream)
        {
            var typeReader = new BinaryTypeReader();
            using var infBr = new BinaryReaderX(infStream);
            using var viwBr = new BinaryReaderX(viwStream);

            // Read inf header
            var infHeader = typeReader.Read<ViwInfHeader>(infBr);

            // Read entries
            infStream.Position = infHeader.entryOffset;
            var entries = typeReader.ReadMany<ViwInfEntry>(infBr, infHeader.fileCount);

            // Read meta entries
            infStream.Position = infHeader.metaOffset;
            _metas = typeReader.ReadMany<ViwInfMetaEntry>(infBr, infHeader.metaCount);

            // Read name entries
            _nameEntries = typeReader.ReadMany<ViwEntry>(viwBr, infHeader.metaCount <= 0 ? infHeader.fileCount : infHeader.metaCount);

            // Add files
            var result = new List<IArchiveFile>();
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

        public void Save(Stream viwStream, Stream infStream, Stream dataStream, List<IArchiveFile> files)
        {
            var typeWriter = new BinaryTypeWriter();
            using var infBw = new BinaryWriterX(infStream);
            using var viwBw = new BinaryWriterX(viwStream);

            // Calculate offset
            var entryOffset = InfHeaderSize;
            var metaOffset = entryOffset + files.Count * InfEntrySize;

            // Write files
            var entries = new List<ViwInfEntry>();

            var filePosition = 0;
            foreach (var file in files)
            {
                dataStream.Position = filePosition;
                var writtenSize = file.WriteFileData(dataStream);

                entries.Add(new ViwInfEntry
                {
                    offset = filePosition,
                    compSize = (int)writtenSize
                });

                filePosition += (int)((writtenSize + 3) & ~3);
            }

            // Write metas
            infStream.Position = metaOffset;
            typeWriter.WriteMany(_metas, infBw);

            // Write entries
            infStream.Position = entryOffset;
            typeWriter.WriteMany(entries, infBw);

            // Write inf header
            infStream.Position = 0;
            typeWriter.Write(new ViwInfHeader
            {
                fileCount = files.Count,
                metaCount = _metas.Count,
                entryOffset = entryOffset,
                metaOffset = metaOffset
            }, infBw);

            // Write name entries
            typeWriter.WriteMany(_nameEntries, viwBw);
        }

        private IArchiveFile CreateAfi(Stream file, string name)
        {
            file.Position = 0;

            var method = NintendoCompressor.PeekCompressionMethod(file);
            var size = NintendoCompressor.PeekDecompressedSize(file);

            return new ArchiveFile(new CompressedArchiveFileInfo
            {
                FilePath = name,
                FileData = file,
                Compression = NintendoCompressor.GetCompression(method),
                DecompressedSize = size
            });
        }
    }
}
