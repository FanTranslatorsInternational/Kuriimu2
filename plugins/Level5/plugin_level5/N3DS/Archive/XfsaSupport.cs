using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;
using plugin_level5.Common.Compression;


#pragma warning disable 649

namespace plugin_level5.N3DS.Archive
{
    interface IXfsa
    {
        List<ArchiveFile> Load(Stream input);
        void Save(Stream output, List<ArchiveFile> files);
    }

    // TODO: Research unk1
    struct XfsaHeader
    {
        public string magic; // XFSA

        public int directoryEntriesOffset;
        public int directoryHashOffset;
        public int fileEntriesOffset;
        public int nameOffset;
        public int dataOffset;

        public short directoryEntriesCount;
        public short directoryHashCount;
        public int fileEntriesCount;
        public int unk1;
    }

    class Xfsa1FileEntry
    {
        public uint crc32;  // filename.ToLower()
        public uint tmp1;   // offset combined with an unknown value, offset is last 24 bits with 4bit left-shift
        public uint tmp2;   // size combined with an unknown value, size is last 20 bits

        public long FileOffset
        {
            get => (tmp1 & 0x03FFFFFF) << 4;
            set => tmp1 = (uint)((tmp1 & ~0x03FFFFFF) | ((value >> 4) & 0x03FFFFFF));
        }

        public long FileSize
        {
            get => tmp2 & 0x007FFFFF;
            set => tmp2 = (uint)((tmp2 & ~0x007FFFFF) | (value & 0x007FFFFF));
        }

        public long NameOffset
        {
            get => (tmp1 >> 26 << 9) | (tmp2 >> 23);
            set
            {
                tmp1 = (uint)((tmp1 & 0x03FFFFFF) | (value >> 9 << 26));
                tmp2 = (uint)((tmp2 & 0x007FFFFF) | (value << 23));
            }
        }
    }

    class Xfsa2FileEntry
    {
        public uint crc32;  // filename.ToLower()
        public uint tmp1;   // offset combined with an unknown value, offset is last 24 bits with 4bit left-shift
        public uint tmp2;   // size combined with an unknown value, size is last 20 bits

        public long FileOffset
        {
            get => (tmp1 & 0x03FFFFFF) << 4;
            set => tmp1 = (uint)((tmp1 & ~0x03FFFFFF) | ((value >> 4) & 0x03FFFFFF));
        }

        public long FileSize
        {
            get => tmp2 & 0x003FFFFF;
            set => tmp2 = (uint)((tmp2 & ~0x003FFFFF) | (value & 0x003FFFFF));
        }

        public long NameOffset
        {
            get => (tmp1 >> 26 << 10) | (tmp2 >> 22);
            set
            {
                tmp1 = (uint)((tmp1 & 0x03FFFFFF) | (value >> 10 << 26));
                tmp2 = (uint)((tmp2 & 0x003FFFFF) | (value << 22));
            }
        }
    }

    struct Xfsa1DirectoryEntry
    {
        public uint crc32;  // directoryName.ToLower()
        public uint tmp1;
        public ushort firstFileIndex;
        public ushort firstDirectoryIndex;
        public uint tmp2;

        public long FileNameStartOffset
        {
            get => tmp1 >> 14;
            set => tmp1 = (uint)((tmp1 & 0x3FFF) | (value << 14));
        }

        public long DirectoryNameOffset
        {
            get => tmp2 >> 14;
            set => tmp2 = (uint)((tmp2 & 0x3FFF) | (value << 14));
        }

        public int FileCount
        {
            get => (int)(tmp1 & 0x3FFF);
            set => tmp1 = (uint)((tmp1 & ~0x3FFFu) | (value & 0x3FFFu));
        }

        public int DirectoryCount
        {
            get => (int)(tmp2 & 0x3FFF);
            set => tmp2 = (uint)((tmp2 & ~0x3FFFu) | (value & 0x3FFFu));
        }
    }

    struct Xfsa2DirectoryEntry
    {
        public uint crc32;
        public int fileCount;
        public int fileNameStartOffset;
        public uint tmp1;
        public int directoryCount;
        public int directoryNameOffset;

        public int FirstFileIndex
        {
            get => (int)(tmp1 & 0x7FFF);
            set => tmp1 = (uint)((tmp1 & ~0x7FFFu) | (value & 0x7FFFu));
        }

        public int FirstDirectoryIndex
        {
            get => (int)(tmp1 >> 15);
            set => tmp1 = (tmp1 & 0x7FFFu) | ((uint)value << 15);
        }
    }

    class XfsaArchiveFile<TEntry> : ArchiveFile
    {
        public TEntry Entry { get; }

        public XfsaArchiveFile(ArchiveFileInfo fileInfo, TEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }

    static class XfsaSupport
    {
        public static Guid[] RetrievePluginMapping(Stream fileStream, string fileName)
        {
            var extension = Path.GetExtension(fileName);
            using var br = new BinaryReaderX(fileStream, true);

            var magic = br.ReadString(4);

            switch (extension)
            {
                case ".xi":
                    return new[] { Guid.Parse("79159dba-3689-448f-8343-167d58a54b2c") };

                case ".xf":
                    return new[] { Guid.Parse("b1b397c4-9a02-4828-b568-39cad733fa3a") };

                case ".xr":
                case ".xc":
                case ".xa":
                case ".xk":
                    if (magic == "XPCK")
                        return new[] { Guid.Parse("de276e88-fb2b-48a6-a55f-d6c14ec60d4f") };

                    return null;

                case ".arc":
                    return new[] { Guid.Parse("db8c2deb-f11d-43c8-bb9e-e271408fd896") };
                    
                default:
                    return null;
            }
        }

        public static BinaryReaderX GetDecompressedTableEntries(Stream input, int offset, int length)
        {
            var streamComp = new SubStream(input, offset, length);
            var stream = new MemoryStream();
            Level5Compressor.Decompress(streamComp, stream);

            stream.Position = 0;
            return new BinaryReaderX(stream);
        }

        public static void WriteCompressedStream(Stream output, Stream decompressedData)
        {
            var optimalCompressedStream = new MemoryStream();
            Compress(decompressedData, optimalCompressedStream, Level5CompressionMethod.NoCompression);

            // Do not test RLE for optimality, since this is probably never the case for tables
            for (var i = 1; i < 4; i++)
            {
                var compressedStream = new MemoryStream();
                Compress(decompressedData, compressedStream, (Level5CompressionMethod)i);

                if (compressedStream.Length < optimalCompressedStream.Length)
                    optimalCompressedStream = compressedStream;
            }

            optimalCompressedStream.CopyTo(output);
        }

        public static void Compress(Stream input, Stream output, Level5CompressionMethod compressionMethod)
        {
            input.Position = 0;
            output.Position = 0;

            Level5Compressor.Compress(input, output, compressionMethod);

            output.Position = 0;
            input.Position = 0;
        }
    }
}
