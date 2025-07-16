using Komponent.IO;
using Komponent.Streams;
using plugin_level5.Common.Archive.Models;
using plugin_level5.Common.Compression;

namespace plugin_level5.Common.Archive
{
    internal class XfspReader : IArchiveReader
    {
        private readonly Decompressor _decompressor = new();

        public ArchiveData Read(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            XfspHeader header = ReadHeader(br);

            br.BaseStream.Position = header.infoOffset << 2;
            XfspEntry[] entries = ReadEntries(br, header.fileCountAndType & 0xFFF);

            Stream compressedNameStream = new SubStream(input, header.nameTableOffset << 2, header.nameTableSize << 2);
            Level5CompressionMethod compression = _decompressor.PeekCompressionType(compressedNameStream, 0);
            Stream nameStream = _decompressor.Decompress(compressedNameStream, 0);

            using var nameReader = new BinaryReaderX(nameStream);
            IList<ArchiveNamedEntry> files = CreateFileEntries(br, header.dataOffset << 2, entries, nameReader);

            return new ArchiveData
            {
                ArchiveType = ArchiveType.Xfsp,
                ContentType = (byte)(header.fileCountAndType >> 0xC),
                StringCompression = compression,
                Files = files
            };
        }

        private XfspHeader ReadHeader(BinaryReaderX br)
        {
            return new XfspHeader
            {
                magic = br.ReadString(4),
                fileCountAndType = br.ReadUInt16(),

                infoOffset = br.ReadUInt16(),
                nameTableOffset = br.ReadUInt16(),
                dataOffset = br.ReadUInt16(),
                infoSize = br.ReadUInt16(),
                nameTableSize = br.ReadUInt16(),
                dataSize = br.ReadUInt32()
            };
        }

        private XfspEntry[] ReadEntries(BinaryReaderX br, int entryCount)
        {
            var result = new XfspEntry[entryCount];
            for (var i = 0; i < entryCount; i++)
                result[i] = ReadEntry(br);

            return result;
        }

        private XfspEntry ReadEntry(BinaryReaderX br)
        {
            return new XfspEntry
            {
                hash = br.ReadUInt16(),
                nameOffset = br.ReadUInt16(),

                fileOffsetLower = br.ReadUInt16(),
                fileSizeLower = br.ReadUInt16(),
                fileOffsetUpper = br.ReadByte(),
                fileSizeUpper = br.ReadByte(),
            };
        }

        private IList<ArchiveNamedEntry> CreateFileEntries(BinaryReaderX br, int dataOffset, XfspEntry[] entries, BinaryReaderX nameReader)
        {
            var result = new ArchiveNamedEntry[entries.Length];
            for (var i = 0; i < entries.Length; i++)
                result[i] = CreateFileEntry(br, dataOffset, entries[i], nameReader);

            return result;
        }

        private ArchiveNamedEntry CreateFileEntry(BinaryReaderX br, int dataOffset, XfspEntry entry, BinaryReaderX nameReader)
        {
            nameReader.BaseStream.Position = entry.nameOffset;
            string name = nameReader.ReadNullTerminatedString();

            int fileOffset = ((entry.fileOffsetUpper << 16) | entry.fileOffsetLower) << 2;
            int fileSize = (entry.fileSizeUpper << 16) | entry.fileSizeLower;

            Stream fileContent = new SubStream(br.BaseStream, dataOffset + fileOffset, fileSize);
            if (name == "RES.bin")
                fileContent = _decompressor.Decompress(fileContent, 0);

            return new ArchiveNamedEntry
            {
                Name = name,
                Content = fileContent
            };
        }
    }
}
