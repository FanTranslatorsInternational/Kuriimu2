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
            //using var bw = new BinaryWriterX(output, ByteOrder.BigEndian);

            //// Write header
            //var archiveSize = _arc.GetArchiveSize(files);
            //_header.fileSize = archiveSize;

            //bw.WriteType(_header);

            //// Write footer
            //output.Position = archiveSize + GetArchiveOffset(_header.type);
            //bw.WriteAlignment();
            //bw.Write(_footer);

            //// Write arc
            //var arcStream = new SubStream(output, GetArchiveOffset(_header.type), archiveSize);
            //_arc.Save(arcStream, files);
        }

        private int GetArchiveOffset(int type)
        {
            return type == 0 ? 0x20000 : 0x10;
        }
    }
}
