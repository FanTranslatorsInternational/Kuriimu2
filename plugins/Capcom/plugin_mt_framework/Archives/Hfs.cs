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

            // Read HFS content
            input.Position = arcOffset;
            _contentMagic = br.ReadString(4);

            switch (_contentMagic)
            {
                case "\0CRA":
                    _arc = new MtArc();
                    return _arc.Load(hfsStream, MtArcPlatform.BigEndian);

                default:
                    return new List<IArchiveFileInfo> { new ArchiveFileInfo(hfsStream, "content.bin") };
            }
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            // Prepare stream
            var archiveOffset = GetArchiveOffset(_header.type);
            var archiveSize = GetArchiveSize(files);

            var hfsLength = HfsStream.GetBaseLength(archiveSize);
            output.SetLength(archiveOffset + hfsLength);

            using var bw = new BinaryWriterX(output, ByteOrder.BigEndian);

            // Write HFS content
            var hfsStream = new HfsStream(new SubStream(output, archiveOffset, hfsLength));
            switch (_contentMagic)
            {
                case "\0CRA":
                    _arc.Save(hfsStream, files);
                    break;

                default:
                    (files[0] as ArchiveFileInfo).SaveFileData(hfsStream);
                    break;
            }

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

        private long GetArchiveSize(IList<IArchiveFileInfo> files)
        {
            switch (_contentMagic)
            {
                case "\0CRA":
                    return _arc.GetArchiveSize(files);

                default:
                    return files[0].FileSize;
            }
        }
    }
}
