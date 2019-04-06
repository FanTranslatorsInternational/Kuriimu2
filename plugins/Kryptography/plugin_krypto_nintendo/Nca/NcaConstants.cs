using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_krypto_nintendo.Nca
{
    static class NcaConstants
    {
        public static int MediaSize => 0x200;
        public static int HeaderSize => 0xc00;
        public static int HeaderWithoutSectionsSize => 0x400;
    }
}
