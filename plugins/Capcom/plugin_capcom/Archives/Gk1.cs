using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_capcom.Archives
{
    class Gk1
    {
        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            br.BaseStream.Position += 8;

            // Read first offset
            var firstOffset = br.ReadInt32();
            br.BaseStream.Position -= 4;

            // Read all offsets
            var offsets = br.ReadMultiple<int>((firstOffset - 8) / 4);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < offsets.Count; i++)
            {
                br.BaseStream.Position = offsets[i];
                var fileSize = br.ReadInt32();

                var subStream = new SubStream(input, offsets[i] + 4, fileSize);
                var fileName = $"{i:00000000}.bin";

                result.Add(new ArchiveFileInfo(subStream, fileName));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = 8;
            var fileOffset = entryOffset + files.Count * 4;

            // Write files
            var offsets = new List<int>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                offsets.Add(filePosition);
                output.Position = filePosition;

                // Write size
                bw.Write((int)file.FileSize);

                // Write file data
                file.SaveFileData(output);

                filePosition += (int)(4 + file.FileSize);
            }

            // Write offsets
            output.Position = entryOffset;
            bw.WriteMultiple(offsets);

            // Write header data
            output.Position = 0;
            bw.Write(fileOffset - 4);
        }
    }
}
