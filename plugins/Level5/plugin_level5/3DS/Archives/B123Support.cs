﻿using System;
using System.IO;
using Komponent.IO;
using Komponent.IO.Attributes;
using Kontract.Interfaces.Progress;
using Kontract.Kompression.Interfaces.Configuration;
using Kontract.Models.Plugins.State.Archive;

#pragma warning disable 649

namespace plugin_level5._3DS.Archives
{
    class B123Header
    {
        [FixedLength(4)]
        public string magic = "B123";
        public int directoryEntriesOffset;
        public int directoryHashOffset;
        public int fileEntriesOffset;
        public int nameOffset;
        public int dataOffset;
        public short directoryEntriesCount;
        public short directoryHashCount;
        public int fileEntriesCount;
        public int tableChunkSize;
        public int zero1;

        //Hashes?
        public uint unk2;
        public uint unk3;
        public uint unk4;
        public uint unk5;

        public int directoryCount;
        public int fileCount;
        public uint unk7;
        public int zero2;
    }

    class B123FileEntry
    {
        // TODO: Hashes of files to lower?
        public uint crc32;  // only filename.ToLower()
        public uint nameOffsetInFolder;
        public uint fileOffset;
        public uint fileSize;
    }

    class B123DirectoryEntry
    {
        // TODO: Hashes of files to lower?
        public uint crc32;  // directoryName.ToLower()
        public short fileCount;
        public short directoryCount;
        public int fileNameStartOffset;
        public int firstFileIndex;
        public int firstDirectoryIndex;
        public int directoryNameStartOffset;
    }

    class B123ArchiveFileInfo : ArchiveFileInfo
    {
        public B123FileEntry Entry { get; }

        public B123ArchiveFileInfo(Stream fileData, string filePath, B123FileEntry entry) :
            base(fileData, filePath)
        {
            Entry = entry;
        }

        public B123ArchiveFileInfo(Stream fileData, string filePath,
            IKompressionConfiguration configuration, long decompressedSize,
            B123FileEntry entry) :
            base(fileData, filePath, configuration, decompressedSize)
        {
            Entry = entry;
        }

        public override long SaveFileData(Stream output, bool compress, IProgressContext progress = null)
        {
            var writtenSize = base.SaveFileData(output, compress, progress);

            output.Position = output.Length;
            while (output.Position % 4 != 0)
                output.WriteByte(0);

            return writtenSize;
        }
    }

    static class B123Support
    {
        public static Guid[] RetrievePluginMapping(Stream fileStream, string fileName)
        {
            var extension = Path.GetExtension(fileName);
            using var br = new BinaryReaderX(fileStream, true);

            var magic = br.ReadString(4);

            switch (extension)
            {
                case ".xi":
                    return new[] { Guid.Parse("898c9151-71bd-4638-8f90-6d34f0a8600c") };

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

                // TODO: add t2b cfg.bin
                //case ".bin":
                //    if (!fileName.EndsWith(".cfg.bin"))
                //        return null;

                //    fileStream.Position = fileStream.Length - 0xF;
                //    if (br.ReadString(3) == "t2b")
                //        return null;

                //    return null;

                default:
                    return null;
            }
        }
    }
}
