using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;

namespace plugin_arc_system_works.Archives
{
    class FPAC
    {
        private static readonly int HeaderSize = 0x20;
        private static readonly int EntrySizeWithoutName = 0xC;

        private FPACTableStructure _tableStruct;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header table structure
            _tableStruct = br.ReadType<FPACTableStructure>();

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in _tableStruct.entries)
            {
                var subStream = new SubStream(input, _tableStruct.header.dataOffset + entry.offset, entry.size);
                result.Add(new ArchiveFileInfo(subStream, entry.fileName.Trim('\0')));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            var maxNameLength = files.Max(x => x.FilePath.ToRelative().GetName().Length + 1);
            maxNameLength = maxNameLength % 4 == 0 ? maxNameLength + 4 : (maxNameLength + 3) & ~3;

            // Calculate offsets
            var fileOffset = HeaderSize + files.Count * ((maxNameLength + EntrySizeWithoutName + 0xF) & ~0xF);

            // Write files
            var entries = new List<FPACEntry>();

            var filePosition = fileOffset;
            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i] as ArchiveFileInfo;

                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);

                entries.Add(new FPACEntry
                {
                    fileName = file.FilePath.GetName().PadRight(maxNameLength, '\0'),
                    fileId = i,
                    offset = filePosition - fileOffset,
                    size = (int)writtenSize
                });

                filePosition += (int)writtenSize;
            }

            // Write structure
            _tableStruct.entries = entries.ToArray();
            _tableStruct.header.dataOffset = fileOffset;
            _tableStruct.header.fileCount = files.Count;
            _tableStruct.header.fileSize = (int)output.Length;
            _tableStruct.header.nameBufferSize = maxNameLength;

            output.Position = 0;
            bw.WriteType(_tableStruct);
        }
    }
}
