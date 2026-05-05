using Komponent.Contract.Enums;

namespace Komponent.Contract.Aspects
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field)]
    public class EndiannessAttribute : Attribute
    {
        public ByteOrder ByteOrder { get; set; } = ByteOrder.LittleEndian;
    }
}
