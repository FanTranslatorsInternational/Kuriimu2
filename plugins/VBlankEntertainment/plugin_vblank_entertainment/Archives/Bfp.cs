using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_vblank_entertainment.Archives
{
    class Bfp
    {
        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = ReadHeader(br);

            // Read entries
            input.Position = 0x20;
            var entries = ReadEntries(br, header.entryCount);

            // Read bucket entries
            var bucketEntries = ReadBucketEntries(br, 0x100);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < header.entryCount; i++)
            {
                var entry = entries[i];
                input.Position = entry.offset;
                var size = br.ReadInt32();

                var subStream = new SubStream(input, entry.offset + 0x20, size);
                var name = $"{i:00000000}.bin";

                result.Add(CreateAfi(subStream, name, entry.decompSize));
            }

            var count = header.entryCount;
            foreach (var entry in bucketEntries.Where(x => x.offset != 0))
            {
                input.Position = entry.offset;
                var size = br.ReadInt32();

                var subStream = new SubStream(input, entry.offset + 0x20, size);
                var name = $"{count++:00000000}.bin";

                result.Add(CreateAfi(subStream, name, entry.decompSize));
            }

            return result;
        }

        private IArchiveFile CreateAfi(Stream file, string name, int decompSize)
        {
            if (file.Length == decompSize)
                return new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = name,
                    FileData = file
                });

            return new ArchiveFile(new CompressedArchiveFileInfo
            {
                FilePath = name,
                FileData = file,
                Compression = Compressions.ZLib.Build(),
                DecompressedSize = decompSize
            });
        }

        private BfpHeader ReadHeader(BinaryReaderX reader)
        {
            return new BfpHeader
            {
                magic = reader.ReadString(4),
                entryCount = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                unk2 = reader.ReadInt32()
            };
        }

        private BfpFileEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new BfpFileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private BfpFileEntry ReadEntry(BinaryReaderX reader)
        {
            return new BfpFileEntry
            {
                hash = reader.ReadUInt32(),
                offset = reader.ReadInt32(),
                decompSize = reader.ReadInt32()
            };
        }

        private BfpBucketFileEntry[] ReadBucketEntries(BinaryReaderX reader, int count)
        {
            var result = new BfpBucketFileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadBucketEntry(reader);

            return result;
        }

        private BfpBucketFileEntry ReadBucketEntry(BinaryReaderX reader)
        {
            return new BfpBucketFileEntry
            {
                offset = reader.ReadInt32(),
                decompSize = reader.ReadInt32()
            };
        }
    }
}
