using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komponent.IO.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field)]
    public class EndiannessAttribute : Attribute
    {
        public ByteOrder ByteOrder = ByteOrder.LittleEndian;
    }
}
