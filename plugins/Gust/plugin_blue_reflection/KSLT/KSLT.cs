using Komponent.IO;
using Kontract.Interfaces.Archive;
using System.Collections.Generic;
using System.IO;

namespace plugin_blue_reflection.KSLT
{
    public class KSLT
    {
        private FileHeader _header;

        public KSLT(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                _header = br.ReadType<FileHeader>();
                br.BaseStream.Position = 0x40;
                var padding = br.ReadMultiple<padding>(_header.FileCount);
                var offsets = br.ReadMultiple<OffsetEntry>(_header.FileCount);
            }
        }
    }
}
