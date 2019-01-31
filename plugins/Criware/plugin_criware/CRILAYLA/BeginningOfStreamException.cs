using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_criware.CRILAYLA
{
    public class BeginningOfStreamException : Exception
    {
        public BeginningOfStreamException() : base("Reached the beginning of the reversed stream")
        {

        }
    }
}
