using System.Buffers.Binary;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;
using Kryptography.Encryption;
using plugin_spike_chunsoft.Archives.Lookups;

namespace plugin_spike_chunsoft.Archives
{
    class NonaryMain
    {
        private static readonly byte[] Key = [0xDA, 0xCE, 0xBA, 0xFA];

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(new PositionalXorStream(input, Key), true);

            // Read header
            var header = ReadHeader(br);

            // Read directories
            var dirHeader = ReadTableHeader(br);
            _ = ReadUnsignedIntegers(br, dirHeader.entryCount);

            br.SeekAlignment();

            var dirEntries = ReadDirectoryEntries(br, dirHeader.entryCount);

            // Read file entries
            var fileHeader = ReadTableHeader(br);
            _ = ReadUnsignedIntegers(br, fileHeader.entryCount);

            br.SeekAlignment();

            // Add files
            var result = new List<IArchiveFile>();

            foreach (var dirEntry in dirEntries)
            {
                if (!NonaryLookups.Directories.TryGetValue(dirEntry.directoryHash, out var dirPath))
                    dirPath = $"/UNK/0x{dirEntry.directoryHash:X8}";

                var fileEntries = ReadEntries(br, dirEntry.fileCount);
                foreach (var fileEntry in fileEntries)
                {
                    var xorValue = BinaryPrimitives.ReadUInt32LittleEndian(fileEntry.XorPad);

                    if (!NonaryLookups.Files.TryGetValue(xorValue, out var fileName))
                        fileName = $"{dirPath}/0x{xorValue:X8}.unk";

                    var subStream = new PositionalXorStream(new SubStream(input, header.dataOffset + fileEntry.fileOffset, fileEntry.fileSize), fileEntry.XorPad);

                    result.Add(new ArchiveFile(new ArchiveFileInfo
                    {
                        FilePath = fileName,
                        FileData = subStream
                    }));
                }
            }

            return result;
        }

        private NonaryHeader ReadHeader(BinaryReaderX reader)
        {
            return new NonaryHeader
            {
                magic = reader.ReadString(4),
                hashTableOffset = reader.ReadInt32(),
                fileEntryOffset = reader.ReadInt32(),
                dataOffset = reader.ReadInt64(),
                infoSecSize = reader.ReadInt64(),
                hold0 = reader.ReadInt32()
            };
        }

        private NonaryTableHeader ReadTableHeader(BinaryReaderX reader)
        {
            return new NonaryTableHeader
            {
                tableSize = reader.ReadInt32(),
                entryCount = reader.ReadInt32(),
                hold0 = reader.ReadInt64()
            };
        }

        private uint[] ReadUnsignedIntegers(BinaryReaderX reader, int count)
        {
            var result = new uint[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadUInt32();

            return result;
        }

        private NonaryDirectoryEntry[] ReadDirectoryEntries(BinaryReaderX reader, int count)
        {
            var result = new NonaryDirectoryEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadDirectoryEntry(reader);

            return result;
        }

        private NonaryDirectoryEntry ReadDirectoryEntry(BinaryReaderX reader)
        {
            return new NonaryDirectoryEntry
            {
                directoryHash = reader.ReadUInt32(),
                fileCount = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                hold0 = reader.ReadUInt32()
            };
        }

        private NonaryEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new NonaryEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private NonaryEntry ReadEntry(BinaryReaderX reader)
        {
            return new NonaryEntry
            {
                fileOffset = reader.ReadInt64(),
                XorPad = reader.ReadBytes(4),
                fileSize = reader.ReadInt64(),
                XorId = reader.ReadUInt32(),
                directoryHashId = reader.ReadInt16(),
                const0 = reader.ReadInt16(),
                hold0 = reader.ReadUInt32()
            };
        }
    }
}
