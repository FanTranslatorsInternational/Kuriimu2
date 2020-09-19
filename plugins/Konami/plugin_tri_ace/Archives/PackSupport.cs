using System;
using System.IO;
using Komponent.IO.Attributes;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;

namespace plugin_tri_ace.Archives
{
    class PackHeader
    {
        [FixedLength(4)]
        public string magic = "P@CK";
        public short version = 3;
        public short fileCount;
    }

    class PackFileEntry
    {
        public int offset;
        public int fileType; // 2 = P@CK; 0x400 = mpak8
        public int unk0; // Maybe ID?
        public int zero0;
    }

    class PackArchiveFileInfo : ArchiveFileInfo
    {
        public PackFileEntry Entry { get; }

        public PackArchiveFileInfo(Stream fileData, string filePath,PackFileEntry entry) : 
            base(fileData, filePath)
        {
            Entry = entry;
        }

        public PackArchiveFileInfo(Stream fileData, string filePath, IKompressionConfiguration configuration, long decompressedSize) : 
            base(fileData, filePath, configuration, decompressedSize)
        {
        }
    }

    static class PackSupport
    {
        public static string DetermineExtension(int fileType)
        {
            switch (fileType)
            {
                case 0x2:
                    return ".pack";

                case 0x20:
                case 0x30:
                case 0x40:
                    return ".cgfx";

                case 0x400:
                    return ".mpak8";

                default:
                    return ".bin";
            }
        }

        public static Guid[] RetrievePluginMapping(int fileType)
        {
            switch (fileType)
            {
                case 0x2:
                    return new[] { Guid.Parse("8c81d937-e1a8-42e6-910a-d9911a6a93af") };

                default:
                    return null;
            }
        }
    }
}
