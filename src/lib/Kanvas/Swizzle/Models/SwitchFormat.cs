using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Swizzle.Models
{
    public enum SwitchFormat : byte
    {
        R8 = 0x02,
        RGB565 = 0x07,
        RG88 = 0x09,
        RGBA8888 = 0x0B,
        DXT1 = 0x1A,
        DXT3 = 0x1B,
        DXT5 = 0x1C,
        ATI1 = 0x1D,
        ATI2 = 0x1E,
        BC6H = 0x1F,
        BC7 = 0x20,
        ASTC4x4 = 0x2D,
        ASTC5x4 = 0x2E,
        ASTC5x5 = 0x2F,
        ASTC6x5 = 0x30,
        ASTC6x6 = 0x31,
        ASTC8x5 = 0x32,
        ASTC8x6 = 0x33,
        ASTC8x8 = 0x34,
        ASTC10x5 = 0x35,
        ASTC10x6 = 0x36,
        ASTC10x8 = 0x37,
        ASTC10x10 = 0x38,
        ASTC12x10 = 0x39,
        ASTC12x12 = 0x3A,
        Empty = 0xFF
    }
}
