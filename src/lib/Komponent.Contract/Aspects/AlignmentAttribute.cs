namespace Komponent.Contract.Aspects
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class AlignmentAttribute(int align) : Attribute
    {
        public int Alignment { get; } = align;
    }
}
