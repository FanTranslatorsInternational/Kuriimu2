using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Format.ASTC.Models
{
    internal enum DecodeMode : int
    {
        LDR_SRGB = 0,
        LDR = 1,
        //HDR = 2       //HDR will be disabled for now, since it sets KTX to bitness 16, which triggers HalfFloat usage
        //Until HalfFloat can be read, it will be left disabled
    }
}
