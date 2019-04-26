using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Format.ASTC.Models
{
    internal enum Method : int
    {
        Compression = 0,
        Decompression = 1,
        DoBoth = 2,
        Compare = 4
    }
}
