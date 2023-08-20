using System;
using Kontract.Models.IO;

namespace Komponent.IO.Attributes
{
    /// <inheritdoc />
    /// <summary>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field)]
    public class EndiannessAttribute : Attribute
    {
        public ByteOrder ByteOrder = ByteOrder.LittleEndian;
    }
}
