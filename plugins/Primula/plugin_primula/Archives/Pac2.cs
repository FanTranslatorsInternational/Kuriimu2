using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_primula.Archives
{
    class Pac2
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(Pac2Header));
        private static readonly int EntrySize = Tools.MeasureType(typeof(Pac2Entry));

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<Pac2Header>();

            // Read entries
            var fileNames = new List<string>();
            for (int i = 0; i < header.fileCount; i++)
            {
                fileNames.Add(Encoding.ASCII.GetString(br.ReadBytes(0x20)).Trim('\0'));                
            }

            var entries = br.ReadMultiple<Pac2Entry>(header.fileCount);
            var dataOrigin = input.Position;

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (int i = 0; i < header.fileCount; i++)
            {
                var name = fileNames[i];
                var fileStream = new SubStream(input, entries[i].Position + dataOrigin, entries[i].Size);

                result.Add(new ArchiveFileInfo(fileStream, name));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var fileNameOffset = HeaderSize;
            var pointerOffset = HeaderSize + (0x20 * files.Count);
            var dataOffset = HeaderSize + (0x20 * files.Count) + (files.Count * 8);

            // Write header
            var header = new Pac2Header { fileCount = files.Count };
            bw.WriteType(header);

            // Write filenames
            foreach (var file in files)
            {
                var fileName = Encoding.ASCII.GetBytes(file.FilePath.ToString().TrimStart('/'));                
                bw.Write(fileName);
                bw.WritePadding(0x20 - fileName.Length);
            }

            // Write files
            var entries = new List<Pac2Entry>();

            output.Position = dataOffset;
            var basePos = 0;
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                var writtenSize = file.SaveFileData(output);

                entries.Add(new Pac2Entry
                {
                    Position = basePos,
                    Size = (int)writtenSize
                });

                basePos += (int)writtenSize;
            }

            // Write pointers
            output.Position = pointerOffset;
            bw.WriteMultiple<Pac2Entry>(entries);            
        }
    }
}
