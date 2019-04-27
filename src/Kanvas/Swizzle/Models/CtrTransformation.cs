using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Swizzle.Models
{
    public enum CtrTransformation : byte
    {
        None = 0,
        YFlip = 2,
        Rotate90 = 4,
        Transpose = 8
    }
}
