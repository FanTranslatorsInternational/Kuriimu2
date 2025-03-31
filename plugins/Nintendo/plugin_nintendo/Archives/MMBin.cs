using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    class MmBin
    {
        private const int HeaderSize_ = 0xC;
        private const int EntrySize_ = 0x40;

        private MMBinHeader _header;

        public List<IArchiveFile> Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = typeReader.Read<MMBinHeader>(br);

            // Read entries
            var entries = typeReader.ReadMany<MMBinResourceEntry>(br, _header.resourceCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                var offset = entry.offset;
                var resourceName = entry.resourceName.Trim('\0');

                var metaStream = new SubStream(input, offset, entry.metaSize);
                var metaName = $"{resourceName}/{resourceName}.meta";
                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = metaName,
                    FileData = metaStream
                }));
                offset += entry.metaSize;

                var ctpkStream = new SubStream(input, offset, entry.ctpkSize);
                var ctpkName = $"{resourceName}/{resourceName}.ctpk";
                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = ctpkName,
                    FileData = ctpkStream
                }));
            }

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize_;
            var fileOffset = entryOffset + (files.Count / 2) * EntrySize_;
            var filePosition = fileOffset;

            // Write files
            output.Position = filePosition;

            var entries = new List<MMBinResourceEntry>();
            foreach (var fileGroup in files.GroupBy(x => x.FilePath.ToRelative().GetDirectory()))
            {
                var metaFile = fileGroup.First(x => x.FilePath.GetExtensionWithDot() == ".meta");
                metaFile.WriteFileData(output);
                var metaSize = metaFile.FileSize;

                var ctpkFile = fileGroup.First(x => x.FilePath.GetExtensionWithDot() == ".ctpk");
                ctpkFile.WriteFileData(output);
                var ctpkSize = ctpkFile.FileSize;

                var entry = new MMBinResourceEntry
                {
                    resourceName = fileGroup.Key.FullName.PadRight(0x24, '\0'),
                    offset = filePosition,
                    metaSize = (int)metaSize,
                    ctpkSize = (int)ctpkSize
                };
                entries.Add(entry);

                filePosition += (int)(metaSize + ctpkSize);
            }

            // Write entries
            output.Position = entryOffset;
            typeWriter.WriteMany(entries, bw);

            // Write header
            output.Position = 0;

            _header.tableSize = fileOffset;
            _header.resourceCount = (short)(files.Count / 2);
            typeWriter.Write(_header, bw);
        }
    }
}
