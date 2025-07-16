using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_mt_framework.Archives
{
    class Hfs
    {
        private HfsHeader _header;
        private string _contentMagic;

        // Method based on MtArc.LoadBigEndian
        public List<IArchiveFile> Load(Stream input, string fileName)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read HFS header
            _header = ReadHeader(br);

            // Prepare stream
            var arcOffset = GetArchiveOffset(_header.type);
            var hfsStream = new HfsStream(new SubStream(input, arcOffset, input.Length - arcOffset), _header.fileSize);

            // Read HFS content
            return
            [
                new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = Path.GetFileNameWithoutExtension(fileName) + ".unhfs" + Path.GetExtension(fileName),
                    FileData = hfsStream
                })
            ];
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            // Prepare stream
            var archiveOffset = GetArchiveOffset(_header.type);
            var archiveSize = files[0].FileSize;

            var hfsLength = HfsStream.GetBaseLength(archiveSize);
            output.SetLength(archiveOffset + hfsLength);

            using var bw = new BinaryWriterX(output, ByteOrder.BigEndian);

            // Write HFS content
            var hfsStream = new HfsStream(new SubStream(output, archiveOffset, hfsLength), archiveSize);
            files[0].WriteFileData(hfsStream);

            hfsStream.Flush();

            // Write header
            _header.fileSize = (int)archiveSize;

            bw.BaseStream.Position = 0;
            WriteHeader(_header, bw);
        }

        private int GetArchiveOffset(int type)
        {
            return type == 0 ? 0x20000 : 0x10;
        }

        private HfsHeader ReadHeader(BinaryReaderX reader)
        {
            return new HfsHeader
            {
                magic = reader.ReadString(4),
                version = reader.ReadInt16(),
                type = reader.ReadInt16(),
                fileSize = reader.ReadInt32()
            };
        }

        private void WriteHeader(HfsHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.version);
            writer.Write(header.type);
            writer.Write(header.fileSize);
        }
    }
}
