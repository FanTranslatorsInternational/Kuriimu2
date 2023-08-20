using System;
using Kontract.Models.IO;

namespace Komponent.IO.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class BitFieldInfoAttribute : Attribute
    {
        public int BlockSize = 4;
        public BitOrder BitOrder = BitOrder.Default;
    }
}
