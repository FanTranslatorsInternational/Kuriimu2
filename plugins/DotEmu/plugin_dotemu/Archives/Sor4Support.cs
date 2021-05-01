using System.IO;
using System.Text;
using Komponent.IO;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;

namespace plugin_dotemu.Archives
{
    class Sor4Entry
    {
        public string path;

        public int offset;
        public int flags;
        public int compSize;

        public static Sor4Entry Read(BinaryReaderX br)
        {
            // Read string
            int value;
            var shift = 0;
            var length = 0;
            do
            {
                value = br.ReadByte();
                length |= (value & 0x7F) << shift;
                shift += 7;
            } while ((value & 0x80) > 0);

            return new Sor4Entry
            {
                path = Encoding.Unicode.GetString(br.ReadBytes(length)),
                offset = br.ReadInt32(),
                flags = br.ReadInt32(),
                compSize = br.ReadInt32()
            };
        }

        public void Write(BinaryWriterX bw)
        {
            // Write string
            var length = Encoding.Unicode.GetByteCount(path);
            do
            {
                var value = length & 0x7F;
                length >>= 7;

                if (length > 0)
                    value |= 0x80;
                bw.Write((byte)value);
            } while (length > 0);

            bw.WriteString(path, Encoding.Unicode, false, false);
            bw.Write(offset);
            bw.Write(flags);
            bw.Write(compSize);
        }
    }

    class Sor4ArchiveFileInfo : ArchiveFileInfo
    {
        public Sor4Entry Entry { get; }

        public Sor4ArchiveFileInfo(Stream fileData, string filePath, Sor4Entry entry, IKompressionConfiguration configuration, long decompressedSize) : base(fileData, filePath, configuration, decompressedSize)
        {
            Entry = entry;
        }
    }
}
