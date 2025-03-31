using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.Plugin.File.Archive;
using Kryptography.Encryption.Nintendo.Wii;

namespace plugin_nintendo.Archives
{
    // TODO: Make partition reading its own plugin?
    class WiiDisc
    {
        public List<IArchiveFile> Load(Stream input)
        {
            var wiiDiscStream = new WiiDiscStream(input);

            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(wiiDiscStream, ByteOrder.BigEndian);

            // Read disc header
            var header = typeReader.Read<WiiDiscHeader>(br);

            // Read partition infos
            br.BaseStream.Position = 0x40000;
            var partitionInformation = typeReader.Read<WiiDiscPartitionInformation>(br);

            // Read partitions
            var partitions = new List<WiiDiscPartitionEntry>();
            br.BaseStream.Position = partitionInformation.partitionOffset1 << 2;
            partitions.AddRange(typeReader.ReadMany<WiiDiscPartitionEntry>(br, partitionInformation.partitionCount1));

            // Read region settings
            br.BaseStream.Position = 0x4E000;
            var regionSettings = typeReader.Read<WiiDiscRegionSettings>(br);

            // Read magic word
            br.BaseStream.Position = 0x4FFFC;
            var magic = br.ReadUInt32();
            if (magic != 0xC3F81A8E)
                throw new InvalidOperationException("Invalid Wii disc magic word.");

            // Read data partitions
            var result = new List<IArchiveFile>();
            foreach (var partition in partitions.Where(x => x.type == 0))
            {
                br.BaseStream.Position = partition.offset << 2;
                var partitionHeader = typeReader.Read<WiiDiscPartitionHeader>(br);

                var partitionStream = new SubStream(wiiDiscStream, (partition.offset << 2) + ((long)partitionHeader.dataOffset << 2), (long)partitionHeader.dataSize << 2);
                var partitionDataStream = new WiiDiscPartitionDataStream(partitionStream);

                using (var partitionBr = new BinaryReaderX(partitionDataStream, true, ByteOrder.BigEndian))
                {
                    // Read partition data header
                    var partitionDataHeader = typeReader.Read<WiiDiscHeader>(partitionBr);

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
