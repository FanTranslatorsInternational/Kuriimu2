using System;
using System.Collections.Generic;
using System.Text;
using Komponent.IO.Attributes;
#pragma warning disable 649

namespace plugin_beeworks.Archives
{
    [Alignment(0x10)]
    class TD3Header
    {
        public int fileCount;
        public int nameBufSize = 0x40;
    }

    class TD3Entry
    {
        public int offset;
        public int size;
        [FixedLength(0x40)]
        public string fileName;
    }
}
