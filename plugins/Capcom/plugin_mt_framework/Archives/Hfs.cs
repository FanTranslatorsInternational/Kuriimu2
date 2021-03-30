using System.Collections.Generic;
using System.IO;
using System.Threading;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_mt_framework.Archives
{
    class Hfs
    {
        private HfsHeader _header;
        private string _contentMagic;

        // Method based on MtArc.LoadBigEndian
        public IList<IArchiveFileInfo> Load(Stream input, string fileName)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read HFS header
            _header = br.ReadType<HfsHeader>();

            // Prepare stream
            var arcOffset = GetArchiveOffset(_header.type);
            var hfsStream = new HfsStream(new SubStream(input, arcOffset, input.Length - arcOffset));

            // Read HFS content
            return new List<IArchiveFileInfo> { new ArchiveFileInfo(hfsStream, Path.GetFileNameWithoutExtension(fileName) + ".unhfs" + Path.GetExtension(fileName)) };
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            // Prepare stream
            var archiveOffset = GetArchiveOffset(_header.type);
            var archiveSize = files[0].FileSize;

            var hfsLength = HfsStream.GetBaseLength(archiveSize);
            output.SetLength(archiveOffset + hfsLength);

            using var bw = new BinaryWriterX(output, ByteOrder.BigEndian);

            // Write HFS content
            var hfsStream = new HfsStream(new SubStream(output, archiveOffset, hfsLength));
            (files[0] as ArchiveFileInfo).SaveFileData(hfsStream);

            hfsStream.Flush();

            // Write header
            _header.fileSize = (int)archiveSize;

            bw.BaseStream.Position = 0;
            bw.WriteType(_header);
        }

        private int GetArchiveOffset(int type)
        {
            return type == 0 ? 0x20000 : 0x10;
        }
    }
}
