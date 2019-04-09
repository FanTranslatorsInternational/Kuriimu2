using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_krypto_nintendo.Nca.Models
{
    public enum NcaSectionCrypto
    {
        NoCrypto = 1,
        Xts,
        Ctr,
        Bktr,
        TitleKey = 256
    }
}
