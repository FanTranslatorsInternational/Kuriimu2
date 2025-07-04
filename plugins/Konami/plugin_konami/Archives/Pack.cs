using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

// NOTE: Each entry has information about decompressed and compressed size and offset per file.
// NOTE: Each entry contains a flag marking a file to be compressed or not
// NOTE: The pack first stores all uncompressed and then all compressed files
// NOTE: For uncompressed files, compressed size and offset are either 0 or equal to decompressed size and offset (equal for TEX files, otherwise 0)
// NOTE: SERI files reference this packs string table, which makes them directly dependent to it

// TODO: Maybe create an extra state and form to better incorporate this pack format and its quirks?

namespace plugin_konami.Archives
{
    class Pack
    {
        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = ReadHeader(br);

            // Read entries
            var entries = ReadEntries(br, header.fileCount);

            // Read string offsets
            input.Position = header.stringOffsetsOffset;
            var stringOffsets = ReadOffsets(br, header.fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < header.fileCount; i++)
            {
                var entry = entries[i];
                var stringOffset = stringOffsets[i];

                var offset = entry.IsCompressed ? entry.compOffset : entry.decompOffset;
                var size = entry.IsCompressed ? entry.compSize : entry.decompSize;
                var subStream = new SubStream(input, offset, size);

                input.Position = header.stringOffset + stringOffset;
                var fileName = br.ReadNullTerminatedString();

                // It seems that for TEX files, the names are stored, but not referenced at all
                // Instead for TEX, the TEXI names are referenced and will be shortened to .tex
                if (entry.magic == "TEX ")
                    fileName = fileName[..^1];

                result.Add(CreateAfi(subStream, fileName, entry));
            }

            return result;
        }

        private IArchiveFile CreateAfi(Stream file, string fileName, PackEntry entry)
        {
            if (entry.IsCompressed)
                return new ArchiveFile(new CompressedArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = file,
                    Compression = Compressions.ZLib.Build(),
                    DecompressedSize = entry.decompSize
                });

            return new ArchiveFile(new ArchiveFileInfo
            {
                FilePath = fileName,
                FileData = file
            });
        }

        private PackHeader ReadHeader(BinaryReaderX reader)
        {
            return new PackHeader
            {
                magic = reader.ReadString(4),
                unk1 = reader.ReadInt16(),
                fileCount = reader.ReadInt16(),
                stringOffsetsOffset = reader.ReadInt32(),
                stringOffset = reader.ReadInt32(),
                decompressedDataEnd = reader.ReadInt32(),
                decompSize = reader.ReadInt32(),
                compSize = reader.ReadInt32(),
                zero1 = reader.ReadInt32()
            };
        }

        private PackEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new PackEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private PackEntry ReadEntry(BinaryReaderX reader)
        {
            return new PackEntry
            {
                magic = reader.ReadString(4),
                zero1 = reader.ReadInt32(),
                decompSize = reader.ReadInt32(),
                decompOffset = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                flags = reader.ReadInt32(),
                compSize = reader.ReadInt32(),
                compOffset = reader.ReadInt32()
            };
        }

        private int[] ReadOffsets(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }
    }
}
