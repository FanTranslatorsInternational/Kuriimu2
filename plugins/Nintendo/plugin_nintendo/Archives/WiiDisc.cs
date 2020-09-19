using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using Kryptography.Nintendo.Wii;

namespace plugin_nintendo.Archives
{
    // TODO: Make partition reading its own plugin?
    class WiiDisc
    {
        public IList<ArchiveFileInfo> Load(Stream input)
        {
            var wiiDiscStream = new WiiDiscStream(input);
            using var br = new BinaryReaderX(wiiDiscStream, ByteOrder.BigEndian);

            // Read disc header
            var header = br.ReadType<WiiDiscHeader>();

            // Read partition infos
            br.BaseStream.Position = 0x40000;
            var partitionInformation = br.ReadType<WiiDiscPartitionInformation>();

            // Read partitions
            var partitions = new List<WiiDiscPartitionEntry>();
            br.BaseStream.Position = partitionInformation.partitionOffset1 << 2;
            partitions.AddRange(br.ReadMultiple<WiiDiscPartitionEntry>(partitionInformation.partitionCount1));

            // Read region settings
            br.BaseStream.Position = 0x4E000;
            var regionSettings = br.ReadType<WiiDiscRegionSettings>();

            // Read magic word
            br.BaseStream.Position = 0x4FFFC;
            var magic = br.ReadUInt32();
            if (magic != 0xC3F81A8E)
                throw new InvalidOperationException("Invalid Wii disc magic word.");

            // Read data partitions
            var result = new List<ArchiveFileInfo>();
            foreach (var partition in partitions.Where(x => x.type == 0))
            {
                br.BaseStream.Position = partition.offset << 2;
                var partitionHeader = br.ReadType<WiiDiscPartitionHeader>();

                var partitionStream = new SubStream(wiiDiscStream, (partition.offset << 2) + ((long)partitionHeader.dataOffset << 2), (long)partitionHeader.dataSize << 2);
                var partitionDataStream = new WiiDiscPartitionDataStream(partitionStream);

                using (var partitionBr = new BinaryReaderX(partitionDataStream, true, ByteOrder.BigEndian))
                {
                    // Read partition data header
                    var partitionDataHeader = partitionBr.ReadType<WiiDiscHeader>();

                    // Read file system offset
                    partitionBr.BaseStream.Position = 0x424;
                    var fileSystemOffset = partitionBr.ReadInt32() << 2;
                    var fileSystemSize = partitionBr.ReadInt32() << 2;

                    // Parse file system
                    var fileSystem = new WiiDiscU8FileSystem("DATA");
                    result.AddRange(fileSystem.Parse(partitionDataStream, fileSystemOffset, fileSystemSize, fileSystemOffset));
                }
            }

            return result;
        }
    }
}
