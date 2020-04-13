using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Komponent.IO;
using Komponent.IO.Attributes;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_nintendo.Archives
{
    public class CIA
    {
        private const int Alignment_ = 0x40;

        public IReadOnlyList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var ciaHeader = br.ReadType<CiaHeader>();

            // Read certificate chain
            var certChain = br.ReadType<CiaCertificateChain>();

            // Read ticket
            var ticket = br.ReadType<CiaTicket>();

            // Read TMD
            var tmd = br.ReadType<CiaTmd>();

            // Declare NCCH partitions
            var result = new List<ArchiveFileInfo>();

            var partitions = new List<SubStream>();
            var partitionOffset = br.BaseStream.Position;
            foreach (var contentChunkRecord in tmd.contentChunkRecord)
            {
                partitions.Add(new SubStream(br.BaseStream, partitionOffset, contentChunkRecord.contentSize));
                partitionOffset += contentChunkRecord.contentSize;
            }

            var index = 0;
            foreach (var partition in partitions)
            {
                partition.Position = 0x188;
                var flags = new byte[8];
                partition.Read(flags, 0, 8);
                partition.Position = 0;

                result.Add(new ArchiveFileInfo(partition, GetPartitionName(flags[5], index++)));
            }

            // Read meta data
            br.BaseStream.Position = partitionOffset;
            CiaMeta meta;
            if (ciaHeader.metaSize != 0)
                meta = br.ReadType<CiaMeta>();

            return result;
        }

        public void Save(Stream output, IReadOnlyList<ArchiveFileInfo> files)
        {

        }

        private string GetPartitionName(byte typeFlag, int index)
        {
            var ext = (typeFlag & 0x1) == 1 && ((typeFlag >> 1) & 0x1) == 1 ? ".cxi" : ".cfa";

            var fileName = "";
            if ((typeFlag & 0x1) == 1 && ((typeFlag >> 1) & 0x1) == 1)
                fileName = "GameData";
            else if ((typeFlag & 0x1) == 1 && ((typeFlag >> 2) & 0x1) == 1 && ((typeFlag >> 3) & 0x1) == 1)
                fileName = "DownloadPlay";
            else if ((typeFlag & 0x1) == 1 && ((typeFlag >> 2) & 0x1) == 1)
                fileName = "3DSUpdate";
            else if ((typeFlag & 0x1) == 1 && ((typeFlag >> 3) & 0x1) == 1)
                fileName = "Manual";
            else if ((typeFlag & 0x1) == 1 && ((typeFlag >> 4) & 0x1) == 1)
                fileName = "Trial";
            else if (typeFlag == 1)
                fileName = $"Data{index:000}";

            return fileName + ext;
        }
    }
}
