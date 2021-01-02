using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;

namespace plugin_nintendo.Archives
{
    class MMBin
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(MMBinHeader));
        private static readonly int EntrySize = Tools.MeasureType(typeof(MMBinResourceEntry));

        private MMBinHeader _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<MMBinHeader>();

            // Read entries
            var entries = br.ReadMultiple<MMBinResourceEntry>(_header.resourceCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                var offset = entry.offset;
                var resourceName = entry.resourceName.Trim('\0');

                var metaStream = new SubStream(input, offset, entry.metaSize);
                var metaName = $"{resourceName}/{resourceName}.meta";
                result.Add(new ArchiveFileInfo(metaStream, metaName));
                offset += entry.metaSize;

                var ctpkStream = new SubStream(input, offset, entry.ctpkSize);
                var ctpkName = $"{resourceName}/{resourceName}.ctpk";
                result.Add(new ArchiveFileInfo(ctpkStream, ctpkName));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var fileOffset = entryOffset + (files.Count / 2) * EntrySize;
            var filePosition = fileOffset;

            // Write files
            output.Position = filePosition;

            var entries = new List<MMBinResourceEntry>();
            foreach (var fileGroup in files.GroupBy(x => x.FilePath.ToRelative().GetDirectory()))
            {
                var metaFile = fileGroup.First(x => x.FilePath.GetExtensionWithDot() == ".meta") as ArchiveFileInfo;
                metaFile.SaveFileData(output);
                var metaSize = metaFile.FileSize;

                var ctpkFile = fileGroup.First(x => x.FilePath.GetExtensionWithDot() == ".ctpk") as ArchiveFileInfo;
                ctpkFile.SaveFileData(output);
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
            bw.WriteMultiple(entries);

            // Write header
            output.Position = 0;

            _header.tableSize = fileOffset;
            _header.resourceCount = (short)(files.Count / 2);
            bw.WriteType(_header);
        }
    }
}
