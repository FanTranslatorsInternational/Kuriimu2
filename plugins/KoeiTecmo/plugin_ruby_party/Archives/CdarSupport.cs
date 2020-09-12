using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Komponent.IO.Attributes;
using Kontract.Interfaces.Progress;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;

namespace plugin_ruby_party.Archives
{
    class CdarHeader
    {
        [FixedLength(4)]
        public string magic = "CDAR";
        public int unk1;
        public int entryCount;
        public int unk2;
    }

    class CdarFileEntry
    {
        public int offset;
        public int decompSize;
        public int size;
    }

    class CdarArchiveFileInfo : ArchiveFileInfo
    {
        public CdarArchiveFileInfo(Stream fileData, string filePath) :
            base(fileData, filePath)
        {
        }

        public CdarArchiveFileInfo(Stream fileData, string filePath, IKompressionConfiguration configuration, long decompressedSize) :
            base(fileData, filePath, configuration, decompressedSize)
        {
        }

        public override long SaveFileData(Stream output, bool compress, IProgressContext progress = null)
        {
            var writtenSize = base.SaveFileData(output, compress, progress);

            var random = new Random();
            while (output.Position % 0x10 > 0)
                output.WriteByte((byte)random.Next());

            return writtenSize;
        }
    }
}
