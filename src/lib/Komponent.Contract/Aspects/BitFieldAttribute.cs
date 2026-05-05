namespace Komponent.Contract.Aspects
{
    [AttributeUsage(AttributeTargets.Field)]
    public class BitFieldAttribute(int bitLength) : Attribute
    {
        public int BitLength { get; } = bitLength;
    }
}
