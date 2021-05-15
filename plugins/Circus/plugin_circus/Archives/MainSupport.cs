using System;
using System.Collections.Generic;
using System.Text;
using Komponent.IO.Attributes;
#pragma warning disable 649

namespace plugin_circus.Archives
{
    class MainHeader
    {
        [FixedLength(4)]
        public string magic;

        public int unk1;
        public int fileSize;
        public int unk2;
    }
}
