using Komponent.IO;
using Kontract.Interfaces.Archive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_yuusha_shisu.PAC
{
    public class PAC
    {
        public List<ArchiveFileInfo> Files { get; } = new List<ArchiveFileInfo>();
        private FileHeader _header;
        private List<FileEntry> _entries;

        public PAC(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                _header = br.ReadType<FileHeader>();
                var offsets = br.ReadMultiple<int>(_header.FileCount);
                br.SeekAlignment(0x20);
                _entries = br.ReadMultiple<FileEntry>(_header.FileCount);
                //br.SeekAlignment(0x80);
                for (int i = 0; i < offsets.Count; i++)
                {
                    var length = (i < offsets.Count-1 ? offsets[i + 1] : (int)br.BaseStream.Length) - offsets[i];
                    Files.Add(new ArchiveFileInfo
                    {
                        FileName = _entries[i].FileName.Trim('\0'),
                        FileData = new SubStream(br.BaseStream, offsets[i], length),
                        State = ArchiveFileState.Archived
                    });
                }
            }
        }

        public bool Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output, true))
            {
                bw.WriteType(_header);
                var offsetPosition = bw.BaseStream.Position;
                bw.BaseStream.Position += _header.FileCount * sizeof(int);
                bw.WriteAlignment(0x20);
                bw.WriteMultiple(_entries);
                bw.WriteAlignment(0x80);
                var offsets = new List<int>();

                foreach(var afi in Files)
                {
                    offsets.Add((int)bw.BaseStream.Position);
                    afi.FileData.CopyTo(bw.BaseStream);
                }
                bw.BaseStream.Position = offsetPosition;
                bw.WriteMultiple(offsets);
            }
            return true;
        }
    }
}
