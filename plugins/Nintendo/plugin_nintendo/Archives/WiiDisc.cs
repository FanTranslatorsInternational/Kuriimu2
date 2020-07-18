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
        public IReadOnlyList<ArchiveFileInfo> Load(Stream input)
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
                    var fileSystem = new U8FileSystem("DATA");
                    result.AddRange(fileSystem.Parse(partitionDataStream, fileSystemOffset, fileSystemSize, fileSystemOffset));
                }
            }

            return result;
        }

        private IReadOnlyList<ArchiveFileInfo> ParseFileSystem(Stream input, long fileSystemOffset, long fileSystemSize)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read entries
            br.BaseStream.Position = fileSystemOffset;
            var rootEntry = br.ReadType<U8Entry>();

            br.BaseStream.Position = fileSystemOffset;
            var entries = br.ReadMultiple<U8Entry>(rootEntry.size);

            // Read names
            var nameStream = new SubStream(input, br.BaseStream.Position, fileSystemSize + fileSystemOffset - br.BaseStream.Position);

            // Add files
            using var nameBr = new BinaryReaderX(nameStream);

            var result = new List<ArchiveFileInfo>();
            var lastDirectoryEntry = entries[0];
            foreach (var entry in entries.Skip(1))
            {
                // A file does not know of its parent directory
                // The tree is structured so that the last directory entry read must hold the current file

                // Remember the last directory entry
                if (entry.IsDirectory)
                {
                    lastDirectoryEntry = entry;
                    continue;
                }

                // Find whole path recursively from lastDirectoryEntry
                var currentDirectoryEntry = lastDirectoryEntry;
                var currentPath = UPath.Empty;
                while (currentDirectoryEntry != entries[0])
                {
                    nameBr.BaseStream.Position = currentDirectoryEntry.NameOffset;
                    currentPath = nameBr.ReadCStringASCII() / currentPath;

                    currentDirectoryEntry = entries[currentDirectoryEntry.offset];
                }

                // Get file name
                nameBr.BaseStream.Position = entry.NameOffset;
                var fileName = currentPath / nameBr.ReadCStringASCII();

                var fileStream = new SubStream(input, fileSystemOffset + entry.offset, entry.size);
                result.Add(new ArchiveFileInfo(fileStream, fileName.FullName));
            }

            return result;
        }
    }
}
