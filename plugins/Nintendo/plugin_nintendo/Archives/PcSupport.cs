using System;
using System.Collections.Generic;
using System.Text;
using Komponent.IO.Attributes;

namespace plugin_nintendo.Archives
{
    class PcHeader
    {
        [FixedLength(2)]
        public string magic = "PC";
        public short entryCount;
    }
}
