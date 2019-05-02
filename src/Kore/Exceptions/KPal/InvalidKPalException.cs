using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kore.Exceptions.KPal
{
    class InvalidKPalException:Exception
    {
        public InvalidKPalException():base("Invalid Kuriimu Palette.")
        {
            
        }
    }
}
