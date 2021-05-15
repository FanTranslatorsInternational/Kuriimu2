using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using Kryptography;

namespace plugin_square_enix.Archives
{
    class Dq3Dat
    {
        private static readonly byte[] Key = { 0x4E, 0x69, 0x29, 0x75 };

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            // Wrap decryption
            input = new XorStream(input, Key);

            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read names
            var count = br.ReadInt16();

            var names = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var length = br.ReadInt16();
                names.Add(Encoding.UTF8.GetString(br.ReadBytes(length)));
            }

            // Read sizes
            count = br.ReadInt16();

            var sizes = br.ReadMultiple<int>(count);

            // Add files
            var result = new List<IArchiveFileInfo>();

            var offset = input.Position;
            for (var i = 0; i < count; i++)
            {
                var name = names[i];
                var size = sizes[i];

                result.Add(new ArchiveFileInfo(new SubStream(input, offset, size), name));

                offset += size;
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            // Wrap encryption
            output = new XorStream(output, Key);

            using var bw = new BinaryWriterX(output, ByteOrder.BigEndian);

            // Calculate offsets
            var sizeOffset = 2 + files.Sum(x => Encoding.UTF8.GetByteCount(x.FilePath.ToRelative().FullName)) + files.Count * 2;
            var dataOffset = sizeOffset + 2 + files.Count * 4;

            // Write files
            output.Position = dataOffset;
            foreach (var file in files.Cast<ArchiveFileInfo>())
                file.SaveFileData(output);

            // Write sizes
            output.Position = sizeOffset;
            bw.Write((short)files.Count);
            bw.WriteMultiple(files.Select(x => (int)x.FileSize));

            // Write names
            output.Position = 0;
            bw.Write((short)files.Count);
            foreach (var file in files)
            {
                var name = file.FilePath.ToRelative().FullName;

                bw.Write((short)Encoding.UTF8.GetByteCount(name));
                bw.WriteString(name, Encoding.UTF8, false, false);
            }
        }
    }
}
