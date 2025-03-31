using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    public class NCSD
    {
        private const int MediaSize_ = 0x200;
        private const int FirstPartitionOffset_ = 0x4000;

        private NcsdHeader _header;

        public List<IArchiveFile> Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = typeReader.Read<NcsdHeader>(br);

            // Parse NCCH partitions
            var result = new List<IArchiveFile>();
            for (var i = 0; i < 8; i++)
            {
                var partitionEntry = _header.partitionEntries[i];
                if (partitionEntry.length == 0)
                    continue;

                var name = GetPartitionName(i);
                var fileStream = new SubStream(input, (long)partitionEntry.offset * MediaSize_, (long)partitionEntry.length * MediaSize_);
                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = name,
                    FileData = fileStream,
                    PluginIds = [Guid.Parse("7d0177a6-1cab-44b3-bf22-39f5548d6cac")]
                }));
            }

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            // Update partition entries
            long partitionOffset = FirstPartitionOffset_;
            foreach (var file in files)
            {
                var partitionIndex = GetPartitionIndex(file.FilePath.GetName());
                var partitionEntry = _header.partitionEntries[partitionIndex];

                partitionEntry.offset = (int)(partitionOffset / MediaSize_);
                partitionEntry.length = (int)(file.FileSize / MediaSize_);

                output.Position = partitionOffset;
                file.WriteFileData(output);

                partitionOffset = output.Position;
            }

            // Store first NCCH header
            var firstNcchHeader = new byte[0x100];
            foreach (var partitionEntry in _header.partitionEntries)
            {
                if (partitionEntry.length != 0)
                {
                    var ncchStream = new SubStream(output, partitionEntry.offset * MediaSize_, partitionEntry.length * MediaSize_);
                    ncchStream.Read(firstNcchHeader, 0, 0x100);
                    break;
                }
            }

            _header.cardHeader.cardInfoHeader.firstNcchHeader = firstNcchHeader;

            output.Position = 0;

            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(output);

            // Update NCSD size
            _header.ncsdSize = (int)(output.Length / MediaSize_);

            // Write NCSD header
            typeWriter.Write(_header, bw);

            // Pad until first partition
            bw.WritePadding(FirstPartitionOffset_ - 0x1200, 0xFF);
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

        private int GetPartitionIndex(string name)
        {
            switch (name)
            {
                case "GameData.cxi":
                    return 0;

                case "Manual.cfa":
                    return 1;

                case "DownloadPlay.cfa":
                    return 2;

                case "New3DSUpdateData.cfa":
                    return 6;

                case "UpdateData.cfa":
                    return 7;

                default:
                    throw new InvalidOperationException($"Partition name {name} is not associated.");
            }
        }
    }
}
