using System;
using System.Collections.Generic;
using System.IO;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_nintendo.Archives
{
    public class NCSD
    {
        private const int MediaSize_ = 0x200;

        public IList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<NcsdHeader>();

            // Parse NCCH partitions
            var result = new List<ArchiveFileInfo>();
            for (var i = 0; i < 8; i++)
            {
                var partitionEntry = header.partitionEntries[i];
                if (partitionEntry.length == 0)
                    continue;

                var name = GetPartitionName(i);
                var fileStream = new SubStream(input, partitionEntry.offset * MediaSize_, partitionEntry.length * MediaSize_);
                result.Add(new ArchiveFileInfo(fileStream, name)
                {
                    // Add NCCH plugin
                    PluginIds = new[] { Guid.Parse("7d0177a6-1cab-44b3-bf22-39f5548d6cac") }
                });
            }

            return result;
        }

        public void Save(Stream output, IList<ArchiveFileInfo> files)
        {
        }

        private string GetPartitionName(int partitionIndex)
        {
            switch (partitionIndex)
            {
                case 0:
                    return "GameData.cxi";

                case 1:
                    return "Manual.cfa";

                case 2:
                    return "DownloadPlay.cfa";

                case 6:
                    return "New3DSUpdateData.cfa";

                case 7:
                    return "UpdateData.cfa";

                default:
                    throw new InvalidOperationException($"Partition index {partitionIndex} is not associated.");
            }
        }
    }
}
