using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompression.LempelZiv.Models
{
    class LzmaProperties
    {
        public int LC { get; }
        public int PB { get; }
        public int LP { get; }

        public LzmaProperties(int lc, int pb, int lp)
        {
            LC = lc;
            PB = pb;
            LP = lp;
        }
    }
}
