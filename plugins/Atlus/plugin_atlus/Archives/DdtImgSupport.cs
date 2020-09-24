using System;
using System.Collections.Generic;
using System.Text;

namespace plugin_atlus.Archives
{
    class DdtEntry
    {
        public uint nameOffset;
        public uint entryOffset;
        public int entrySize;

        public bool IsFile => entrySize >= 0;
    }
}
