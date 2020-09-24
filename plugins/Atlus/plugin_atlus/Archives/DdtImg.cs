using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_atlus.Archives
{
    class DdtImg
    {
        private const int Alignment_ = 0x800;

        private static readonly int EntrySize = Tools.MeasureType(typeof(DdtEntry));

        public IList<IArchiveFileInfo> Load(Stream ddtStream, Stream imgStream)
        {
            using var br = new BinaryReaderX(ddtStream);
            return EnumerateFiles(br, imgStream, UPath.Root).ToArray();
        }

        private IEnumerable<IArchiveFileInfo> EnumerateFiles(BinaryReaderX br, Stream imgStream, UPath currentPath)
        {
            // Read current entry
            var entry = br.ReadType<DdtEntry>();

            // Read name
            var nameLength = GetStringLength(br, entry.nameOffset);
            br.BaseStream.Position = entry.nameOffset;
            var name = br.PeekString(nameLength, Encoding.GetEncoding("EUC-JP"));

            // If entry is a file
            if (entry.IsFile)
            {
                var subStream = new SubStream(imgStream, entry.entryOffset * Alignment_, entry.entrySize);

                yield return new ArchiveFileInfo(subStream, (currentPath / name).FullName);
                yield break;
            }

            // If entry is a directory
            for (var i = 0; i < -entry.entrySize; i++)
            {
                br.BaseStream.Position = entry.entryOffset + i * EntrySize;
                foreach (var file in EnumerateFiles(br, imgStream, currentPath / name))
                    yield return file;
            }
        }

        private int GetStringLength(BinaryReaderX br, long offset)
        {
            var bkPos = br.BaseStream.Position;

            var result = 0;

            br.BaseStream.Position = offset;
            while (br.ReadByte() != 0)
                result++;

            br.BaseStream.Position = bkPos;
            return result;
        }
    }
}
