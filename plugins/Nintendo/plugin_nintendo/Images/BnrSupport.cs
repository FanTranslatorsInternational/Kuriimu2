using System;
using System.Collections.Generic;
using System.Text;
using Komponent.IO.Attributes;

namespace plugin_nintendo.Images
{
    [Alignment(0x20)]
    class BnrHeader
    {
        public short version;
        public ushort crc16_v1;
        public ushort crc16_v2;
        public ushort crc16_v3;
        public ushort crc16_v103;
    }
}
