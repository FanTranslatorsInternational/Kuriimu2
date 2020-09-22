using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_nintendo.Archives
{
    public class PC
    {
        private static int _headerSize = Tools.MeasureType(typeof(PcHeader));

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<PcHeader>();

            // Read offsets
            var offsets = br.ReadMultiple<uint>(header.entryCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < offsets.Count; i++)
            {
                var endOffset = i + 1 < offsets.Count ? offsets[i + 1] : input.Length;
                var fileStream = new SubStream(input, offsets[i], endOffset - offsets[i]);

                result.Add(new ArchiveFileInfo(fileStream, $"{i:00000000}.bin"));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            var dataPosition = (_headerSize + (files.Count + 1) * 4 + 0x7F) & ~0x7F;

            using var bw = new BinaryWriterX(output);

            // Write files
            bw.BaseStream.Position = dataPosition;

            var offsets = new List<uint>();
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                offsets.Add((uint)bw.BaseStream.Position);

                file.SaveFileData(bw.BaseStream, null);
                bw.WriteAlignment(0x80);
            }

            // Write offsets
            bw.BaseStream.Position = _headerSize;
            bw.WriteMultiple(offsets);
            bw.Write(bw.BaseStream.Length);

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(new PcHeader
            {
                entryCount = (short)files.Count
            });
        }
    }
}
