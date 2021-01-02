using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_tamsoft.Archives
{
    class Skb
    {
        private byte[] _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadBytes(0x18);

            // Read entry count
            br.BaseStream.Position = 0x20;
            var entryCount = br.ReadInt32();

            // Read offsets
            var offsets = br.ReadMultiple<int>(entryCount);

            // Read sizes
            var sizes = br.ReadMultiple<int>(entryCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < entryCount; i++)
            {
                var subStream = new SubStream(input, offsets[i], sizes[i]);
                var name = $"{i:00000000}{SkbSupport.DetermineExtension(subStream)}";

                result.Add(new ArchiveFileInfo(subStream, name));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = 0x20;
            var fileOffset = (entryOffset + 4 + files.Count * 8 + 0x7F) & ~0x7F;

            // Write files
            var offsets = new List<int>();
            var sizes = new List<int>();

            output.Position = fileOffset;
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                fileOffset = (int)output.Position;
                var writtenSize = file.SaveFileData(output);

                bw.WriteAlignment(0x80);

                offsets.Add(fileOffset);
                sizes.Add((int)writtenSize);
            }

            // Write entries
            output.Position = entryOffset;

            bw.Write(files.Count);
            bw.WriteMultiple(offsets);
            bw.WriteMultiple(sizes);

            // Write header
            output.Position = 0;
            bw.Write(_header);
        }
    }
}
