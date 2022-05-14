using Komponent.IO.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace plugin_capcom.Archives
{
    public class GtPacHeader
    {
        [FixedLength(8)]
        public int fileCount;
        public uint fileOffsetTableSize;
    }
    class GtPacSupport
    {

    }
}
