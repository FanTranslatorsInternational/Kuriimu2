using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Format.ASTC.Models
{
    internal enum BlockMode : int
    {
        ASTC4x4 = 0,
        ASTC5x4 = 1,
        ASTC5x5 = 2,
        ASTC6x5 = 3,
        ASTC6x6 = 4,
        ASTC8x5 = 5,
        ASTC8x6 = 6,
        ASTC8x8 = 7,
        ASTC10x5 = 8,
        ASTC10x6 = 9,
        ASTC10x8 = 10,
        ASTC10x10 = 11,
        ASTC12x10 = 12,
        ASTC12x12 = 13,

        ASTC3x3x3 = 14,
        ASTC4x3x3 = 15,
        ASTC4x4x3 = 16,
        ASTC4x4x4 = 17,
        ASTC5x4x4 = 18,
        ASTC5x5x4 = 19,
        ASTC5x5x5 = 20,
        ASTC6x5x5 = 21,
        ASTC6x6x5 = 22,
        ASTC6x6x6 = 23,
    }
}
