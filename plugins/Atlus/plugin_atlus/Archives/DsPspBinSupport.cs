using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Attributes;
#pragma warning disable 649

namespace plugin_atlus.Archives
{
    class DsPspBinHeader
    {        
        public int FileCount;
    }

    class DsPspBinEntry
    {        
        public int Size;
    }
}
