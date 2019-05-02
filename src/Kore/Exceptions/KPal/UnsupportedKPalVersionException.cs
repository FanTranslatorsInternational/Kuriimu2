using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kore.Exceptions.KPal
{
    class UnsupportedKPalVersionException : Exception
    {
        public UnsupportedKPalVersionException(int version) : base($"Unsupported Kuriimu Palette version: {version}.")
        {

        }
    }
}
