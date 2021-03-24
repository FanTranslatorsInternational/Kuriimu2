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
        //private const int FooterSize_ = 0x10;

        private HfsHeader _header;
        //private byte[] _footer;

        private MtArc _arc;

        // Method based on MtArc.LoadBigEndian
        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read HFS header
            _header = br.ReadType<HfsHeader>();

            // Prepare stream
            var arcOffset = GetArchiveOffset(_header.type);
            var hfsStream = new HfsStream(new SubStream(input, arcOffset, input.Length - arcOffset));

            // Read MT ARC
            _arc = new MtArc();
            var files = _arc.Load(hfsStream, MtArcPlatform.BigEndian);

            return files;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            // Prepare stream
            var archiveOffset = GetArchiveOffset(_header.type);
            var archiveSize = _arc.GetArchiveSize(files);

            var hfsLength = HfsStream.GetBaseLength(archiveSize);
            output.SetLength(archiveOffset + hfsLength);

            using var bw = new BinaryWriterX(output, ByteOrder.BigEndian);

            // Write arc
            var hfsStream = new HfsStream(new SubStream(output, archiveOffset, hfsLength));
            _arc.Save(hfsStream, files);

            hfsStream.Flush();

            // Write header
            _header.fileSize = archiveSize;

            bw.BaseStream.Position = 0;
            bw.WriteType(_header);
        }

        private int GetArchiveOffset(int type)
        {
            return type == 0 ? 0x20000 : 0x10;
        }
    }
}
