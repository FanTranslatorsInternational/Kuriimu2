using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;

namespace plugin_sting_entertainment.Archives
{
    class Pck
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(PckHeader));
        private static readonly int EntrySize = Tools.MeasureType(typeof(PckEntry));

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read name header
            var nameHeader = br.ReadType<PckHeader>();

            // Read file count
            input.Position = nameHeader.size + HeaderSize;
            var fileCount = br.ReadInt32();

            // Read file names
            input.Position = HeaderSize;
            var nameOffsets = br.ReadMultiple<int>(fileCount);

            // Read entries
            input.Position = nameHeader.size + HeaderSize + 4;
            var entries = br.ReadMultiple<PckEntry>(fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < fileCount; i++)
            {
                var entry = entries[i];

                var fileStream = new SubStream(input, entry.offset, entry.size);
                input.Position = nameOffsets[i] + HeaderSize;
                var fileName = br.ReadCStringASCII();

                result.Add(new ArchiveFileInfo(fileStream, fileName));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var nameOffsetsOffset = HeaderSize;
            var stringOffset = nameOffsetsOffset + files.Count * 4;
            var packOffset = (stringOffset + files.Sum(x => x.FilePath.ToRelative().FullName.Length + 1) + 3) & ~3;
            var entryOffset = packOffset + HeaderSize + 4;
            var dataOffset = (entryOffset + files.Count * EntrySize + 0x7FF) & ~0x7FF;

            // Write files
            var names = new List<string>();
            var entries = new List<PckEntry>();

            var dataPosition = dataOffset;
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                // Write file data
                output.Position = dataPosition;
                var writtenSize = file.SaveFileData(output);
                bw.WriteAlignment(0x800);

                // Add entry
                entries.Add(new PckEntry { offset = dataPosition, size = (int)writtenSize });

                // Add name
                names.Add(file.FilePath.ToRelative().FullName);

                dataPosition = (int)((dataPosition + writtenSize + 0x7FF) & ~0x7FF);
            }
            bw.Write(0);

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write pack header
            output.Position = packOffset;
            bw.WriteType(new PckHeader { magic = "Pack    ", size = HeaderSize + 4 + files.Count * EntrySize });
            bw.Write(entries.Count);

            // Write strings
            var nameOffsets = new List<int>();

            output.Position = stringOffset;
            foreach (var name in names)
            {
                nameOffsets.Add((int)output.Position - HeaderSize);
                bw.WriteString(name, Encoding.ASCII, false);
            }

            // Write name offsets
            output.Position = nameOffsetsOffset;
            bw.WriteMultiple(nameOffsets);

            // Write name header
            output.Position = 0;
            bw.WriteType(new PckHeader { magic = "Filename", size = packOffset });
        }
    }
}
