using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Format.ASTC.KTX.Models
{
    internal enum GLDataType : int
    {
        BYTE = 0x1400,
        UBYTE = 0x1401,
        SHORT = 0x1402,
        USHORT = 0x1403,
        INT = 0x1404,
        UINT = 0x1405,
        FLOAT = 0x1406,
        TWOBYTES = 0x1407,
        THREEBYTES = 0x1408,
        FOURBYTES = 0x1409,
        DOUBLE = 0x140A,
        //HALF_FLOAT = 0x140B,
        FIXED = 0x140C
    }
}
