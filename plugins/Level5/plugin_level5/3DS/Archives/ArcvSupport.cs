﻿using System.IO;
using Komponent.IO.Attributes;
using Kontract.Interfaces.Progress;
using Kontract.Models.Plugins.State.Archive;

#pragma warning disable 649

namespace plugin_level5._3DS.Archives
{
    class ArcvHeader
    {
        [FixedLength(4)]
        public string magic = "ARCV";
        public int fileCount;
        public int fileSize;
    }

    class ArcvFileInfo
    {
        public int offset;
        public int size;
        public uint hash;
    }

    class ArcvArchiveFileInfo : ArchiveFileInfo
    {
        public ArcvFileInfo Entry { get; }

        public ArcvArchiveFileInfo(Stream fileData, string filePath, ArcvFileInfo entry) :
            base(fileData, filePath)
        {
            Entry = entry;
        }

        public override long SaveFileData(Stream output, bool compress, IProgressContext progress = null)
        {
            var writtenSize = base.SaveFileData(output, compress, progress);

            output.Position = output.Length;
            while (output.Position % 0x7F != 0)
                output.WriteByte(0xAC);

            return writtenSize;
        }
    }
}
